using System.Text.Json;
using Amazon.Athena;
using Amazon.Athena.Model;
using Amazon.DynamoDBv2;
using Amazon.Glue;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Anthropic;
using Pokepad.Lambda;
using Pokepad.Lambda.Models;
using Pokepad.Lambda.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddAWSService<IAmazonGlue>();
builder.Services.AddAWSService<IAmazonAthena>();
builder.Services.AddAWSService<IAmazonDynamoDB>();

if (!builder.Environment.IsDevelopment())
{
    var ssmParamName = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY_PARAM")
                       ?? throw new InvalidOperationException("ANTHROPIC_API_KEY_PARAM is required");

    using var ssm = new AmazonSimpleSystemsManagementClient();
    var ssmResponse = await ssm.GetParameterAsync(new GetParameterRequest
    {
        Name = ssmParamName,
        WithDecryption = true
    });

    Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", ssmResponse.Parameter.Value);
}

builder.Services.AddSingleton<AnthropicClient>();
builder.Services.AddSingleton<GlueSchemaService>();
builder.Services.AddSingleton<AthenaService>();
builder.Services.AddSingleton<ClaudeService>();
builder.Services.AddSingleton<SqlValidator>();
builder.Services.AddSingleton<QueryTrackingService>();

var app = builder.Build();

var v1 = app.MapGroup("/v1");

v1.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

v1.MapPost("/search", async (
    SearchRequest request,
    GlueSchemaService glue,
    ClaudeService claude,
    AthenaService athena,
    SqlValidator validator) =>
{
    var schema = await glue.GetSchemaAsync();
    var sql = await claude.GenerateSqlAsync(request.Question, schema);

    validator.Validate(sql);

    var results = await athena.ExecuteAsync(sql);
    return Results.Ok(new SearchResponse(sql, results.Columns, results.Rows));
});

v1.MapPost("/query/start", async (
    HttpContext ctx,
    SearchRequest request,
    GlueSchemaService glue,
    ClaudeService claude,
    AthenaService athena,
    SqlValidator validator,
    QueryTrackingService tracking) =>
{
    var userId = GetUserId(ctx);

    var schema = await glue.GetSchemaAsync();
    var sql = await claude.GenerateSqlAsync(request.Question, schema);
    validator.Validate(sql);

    var executionId = await athena.StartAsync(sql);
    await tracking.TrackAsync(executionId, userId);

    return Results.Accepted($"/v1/query/{executionId}/status", new { executionId });
});

v1.MapGet("/query/{id}/status", async (
    HttpContext ctx,
    string id,
    AthenaService athena,
    QueryTrackingService tracking) =>
{
    var userId = GetUserId(ctx);

    var owner = await tracking.GetOwnerAsync(id);
    if (owner is null) return Results.NotFound();
    if (owner != userId) return Results.Forbid();

    var execution = await athena.GetExecutionAsync(id);
    return Results.Ok(new { executionId = id, status = execution.Status.State.Value });
});

v1.MapGet("/query/{id}/results", async (
    HttpContext ctx,
    string id,
    AthenaService athena,
    QueryTrackingService tracking) =>
{
    var userId = GetUserId(ctx);

    var owner = await tracking.GetOwnerAsync(id);
    if (owner is null) return Results.NotFound();
    if (owner != userId) return Results.Forbid();

    var execution = await athena.GetExecutionAsync(id);
    if (execution.Status.State != QueryExecutionState.SUCCEEDED)
        return Results.Conflict(new { executionId = id, status = execution.Status.State.Value });

    var results = await athena.FetchResultsAsync(id);
    return Results.Ok(new SearchResponse(execution.Query, results.Columns, results.Rows));
});

app.Run();

// API Gateway validates the JWT before forwarding to Lambda, so decoding without
// re-validating the signature here is safe and avoids an outbound JWKS fetch.
// The alternative — AddJwtBearer — would on first cold-start request:
//   1. Extract the kid from the JWT header
//   2. GET https://cognito-idp.{region}.amazonaws.com/{poolId}/.well-known/jwks.json
//   3. Verify the signature against the matching public key
//   4. Populate HttpContext.User.Claims
// The JWKS is cached after that, but cold starts are already the expensive path.
static string GetUserId(HttpContext ctx)
{
    var auth = ctx.Request.Headers.Authorization.ToString();
    if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return Results.Unauthorized().ToString()!; // unreachable past the authorizer

    var parts = auth[7..].Split('.');
    if (parts.Length < 2) throw new InvalidOperationException("Malformed JWT");

    var payload = parts[1].Replace('-', '+').Replace('_', '/');
    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

    using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
    return doc.RootElement.TryGetProperty("sub", out var sub)
        ? sub.GetString() ?? throw new InvalidOperationException("Empty sub claim")
        : throw new InvalidOperationException("No sub claim in JWT");
}
