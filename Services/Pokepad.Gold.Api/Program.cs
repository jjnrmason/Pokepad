using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;
using System.Text.Json;
using Amazon.Athena;
using Amazon.DynamoDBv2;
using Amazon.Glue;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Npgsql;
using OpenAI;
using Pokepad.Gold.Api.Endpoints.V1;
using Pokepad.Gold.Api.Exceptions;
using Pokepad.Gold.Api.Middleware;
using Pokepad.Gold.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddAWSService<IAmazonGlue>();
builder.Services.AddAWSService<IAmazonAthena>();
builder.Services.AddAWSService<IAmazonDynamoDB>();

if (!builder.Environment.IsDevelopment())
{
    var ssmParamName = Environment.GetEnvironmentVariable("AI_API_KEY_PARAM")
                       ?? Environment.GetEnvironmentVariable("API_KEY_PARAM")
                       ?? throw new InvalidOperationException("AI_API_KEY_PARAM is required");
    var vectorDbConnectionStringParam = Environment.GetEnvironmentVariable("VECTOR_DB_CONNECTION_STRING_PARAM")
                                        ?? throw new InvalidOperationException("VECTOR_DB_CONNECTION_STRING_PARAM is required");
    var vectorDbSecretArn = Environment.GetEnvironmentVariable("VECTOR_DB_SECRET_ARN")
                            ?? throw new InvalidOperationException("VECTOR_DB_SECRET_ARN is required");

    using var ssm = new AmazonSimpleSystemsManagementClient();
    using var secretsManager = new AmazonSecretsManagerClient();
    var ssmResponse = await ssm.GetParameterAsync(new GetParameterRequest
    {
        Name = ssmParamName,
        WithDecryption = true
    });
    var vectorDbResponse = await ssm.GetParameterAsync(new GetParameterRequest
    {
        Name = vectorDbConnectionStringParam,
        WithDecryption = true
    });
    var vectorDbSecretResponse = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
    {
        SecretId = vectorDbSecretArn
    });
    using var vectorDbSecret = JsonDocument.Parse(vectorDbSecretResponse.SecretString);
    var vectorDbPassword = vectorDbSecret.RootElement.GetProperty("password").GetString()
                           ?? throw new InvalidOperationException("VECTOR_DB_SECRET_ARN did not contain a password");
    var vectorDbConnectionString = $"{vectorDbResponse.Parameter.Value};Password={vectorDbPassword}";

    builder.Services.AddSingleton(_ => new OpenAIClient(ssmResponse.Parameter.Value));
    builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(vectorDbConnectionString));
} else 
{
    builder.Services.AddSingleton(_ => new OpenAIClient(
        Environment.GetEnvironmentVariable("API_KEY") ?? throw new InvalidOperationException("API_KEY is required")
    ));
    builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(
        Environment.GetEnvironmentVariable("VECTOR_DB_CONNECTION_STRING")
        ?? throw new InvalidOperationException("VECTOR_DB_CONNECTION_STRING is required")
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
builder.Services.AddSingleton<IChatService, OpenAiChatService>();
builder.Services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddSingleton<IModerationService, OpenAiModerationService>();
builder.Services.AddSingleton<ISemanticSearchRepository, PgVectorSemanticSearchRepository>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<SemanticSearchService>();
builder.Services.AddSingleton<SqlValidator>();
builder.Services.AddSingleton<QueryTrackingService>();
builder.Services.AddAuthentication("ApiGateway")
    .AddScheme<AuthenticationSchemeOptions, ApiGatewayAuthHandler>("ApiGateway", _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (ex is InputValidationException)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    else
    {
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new { error = "Something went wrong. Try again." });
    }
}));

app.MapOpenApi();
app.MapScalarApiReference();

var v1 = app.MapGroup("/v1");

v1.MapHealthEndpoints();
v1.MapSearchEndpoints();
v1.MapSemanticSearchEndpoints();
v1.MapQueryEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
