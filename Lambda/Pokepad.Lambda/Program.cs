using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;
using Amazon.Athena;
using Amazon.DynamoDBv2;
using Amazon.Glue;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using OpenAI;
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
    var ssmParamName = Environment.GetEnvironmentVariable("API_KEY_PARAM")
                       ?? throw new InvalidOperationException("API_KEY_PARAM is required");

    using var ssm = new AmazonSimpleSystemsManagementClient();
    var ssmResponse = await ssm.GetParameterAsync(new GetParameterRequest
    {
        Name = ssmParamName,
        WithDecryption = true
    });

    builder.Services.AddSingleton(_ => new OpenAIClient(ssmResponse.Parameter.Value));
} else 
{
    builder.Services.AddSingleton(_ => new OpenAIClient(
        Environment.GetEnvironmentVariable("API_KEY") ?? throw new InvalidOperationException("API_KEY is required")
    ));
}

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new()
        {
            Title = "Pokepad Search API",
            Version = "v1",
            Description = """
                Natural language search over e-commerce data. Ask a question in plain English;
                OpenAI generates the SQL, Athena executes it, and results are returned as structured JSON.

                ## Authentication

                All endpoints except `GET /v1/health` require a Cognito JWT passed as `Authorization: Bearer <token>`.
                Obtain a token via the Cognito `InitiateAuth` API using `USER_PASSWORD_AUTH` flow.

                ## Data schema

                Queries run against the `ecommerce_gold` Glue database. Four tables are available:

                ### customers
                | Column | Type | Description |
                |--------|------|-------------|
                | CustomerId | string | Unique customer identifier (UUID) |
                | FirstName | string | First name |
                | LastName | string | Last name |
                | Email | string | Email address — unique per customer |
                | Phone | string | Contact phone number |
                | Address | string | Street address |
                | City | string | City |
                | Country | string | Country |
                | CreatedAt | timestamp | Account creation timestamp |

                ### products
                | Column | Type | Description |
                |--------|------|-------------|
                | ProductId | string | Unique product identifier (UUID) |
                | Name | string | Product display name |
                | Category | string | Electronics, Clothing, Home & Garden, Sports, Books, Toys, Beauty, Automotive |
                | Description | string | Product description |
                | Price | double | Unit price in USD |
                | StockQuantity | int | Available stock quantity |

                ### orders
                | Column | Type | Description |
                |--------|------|-------------|
                | OrderId | string | Unique order identifier (UUID) |
                | CustomerId | string | Foreign key → customers.CustomerId |
                | OrderDate | timestamp | Timestamp when the order was placed |
                | Status | string | Pending, Processing, Shipped, Delivered, Cancelled |
                | TotalAmount | double | Total order value in USD |
                | ShippingAddress | string | Full shipping address for this order |

                ### order_items
                | Column | Type | Description |
                |--------|------|-------------|
                | OrderItemId | string | Unique order item identifier (UUID) |
                | OrderId | string | Foreign key → orders.OrderId |
                | ProductId | string | Foreign key → products.ProductId |
                | Quantity | int | Number of units ordered |
                | UnitPrice | double | Unit price at time of order in USD |
                | Subtotal | double | Line total: Quantity × UnitPrice in USD |
                """
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddSingleton<GlueSchemaService>();
builder.Services.AddSingleton<AthenaService>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<SqlValidator>();
builder.Services.AddSingleton<QueryTrackingService>();
builder.Services.AddAuthentication("ApiGateway")
    .AddScheme<AuthenticationSchemeOptions, ApiGatewayAuthHandler>("ApiGateway", _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

var v1 = app.MapGroup("/v1");

v1.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithSummary("Health check")
    .WithDescription("Returns 200 OK when the service is running. No authentication required.")
    .WithTags("Health");

v1.MapPost("/search", async (
    SearchRequest request,
    GlueSchemaService glue,
    OpenAiService openAi,
    AthenaService athena,
    SqlValidator validator) =>
{
    var schema = await glue.GetSchemaAsync();
    var sql = await openAi.GenerateSqlAsync(request.Question, schema);

    validator.Validate(sql);

    var results = await athena.ExecuteAsync(sql);
    return Results.Ok(new SearchResponse(sql, results.Columns, results.Rows));
})
    .WithName("Search")
    .WithSummary("Synchronous natural language search")
    .WithDescription("Translates a natural-language question into SQL via OpenAI, executes it against Athena, and returns the results. Blocks until the query completes (up to the 30 s Lambda timeout).")
    .WithTags("Search")
    .Produces<SearchResponse>()
    .RequireAuthorization();

v1.MapPost("/query/start", async (
    HttpContext ctx,
    SearchRequest request,
    GlueSchemaService glue,
    OpenAiService openAi,
    AthenaService athena,
    SqlValidator validator,
    QueryTrackingService tracking) =>
{
    var userId = GetUserId(ctx);

    var schema = await glue.GetSchemaAsync();
    var sql = await openAi.GenerateSqlAsync(request.Question, schema);
    validator.Validate(sql);

    var executionId = await athena.StartAsync(sql);
    await tracking.TrackAsync(executionId, userId);

    return Results.Accepted($"/v1/query/{executionId}/status", new { executionId });
})
    .WithName("StartQuery")
    .WithSummary("Start an asynchronous query")
    .WithDescription("Starts an Athena query and returns immediately with an executionId. Poll /v1/query/{id}/status until SUCCEEDED, then fetch results from /v1/query/{id}/results.")
    .WithTags("Async Query")
    .RequireAuthorization();

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
})
    .WithName("GetQueryStatus")
    .WithSummary("Poll query status")
    .WithDescription("Returns the current Athena execution state (QUEUED, RUNNING, SUCCEEDED, FAILED, CANCELLED). Returns 404 if unknown/expired, 403 if not the query owner.")
    .WithTags("Async Query")
    .RequireAuthorization();

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
})
    .WithName("GetQueryResults")
    .WithSummary("Fetch results for a completed query")
    .WithDescription("Returns query results once status is SUCCEEDED. Returns 409 Conflict with the current status if the query is still running.")
    .WithTags("Async Query")
    .Produces<SearchResponse>()
    .RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();

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
