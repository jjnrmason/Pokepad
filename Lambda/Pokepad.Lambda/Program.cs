using Amazon.Athena;
using Amazon.Glue;
using Anthropic;
using Pokepad.Lambda;
using Pokepad.Lambda.Models;
using Pokepad.Lambda.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddAWSService<IAmazonGlue>();
builder.Services.AddAWSService<IAmazonAthena>();
builder.Services.AddSingleton<AnthropicClient>();
builder.Services.AddSingleton<GlueSchemaService>();
builder.Services.AddSingleton<AthenaService>();
builder.Services.AddSingleton<ClaudeService>();
builder.Services.AddSingleton<SqlValidator>();

var app = builder.Build();

app.MapPost("/search", async (
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
