using Amazon.Athena;
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

app.Run();
