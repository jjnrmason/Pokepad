using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests;

[Category("Integration")]
public abstract class ApiTestBase
{
    protected const string ValidQuestion = "How many customers are there?";

    protected HttpClient PrimaryUserClient { get; private set; } = null!;
    protected HttpClient SecondaryUserClient { get; private set; } = null!;
    protected HttpClient AnonymousClient { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.PrimaryUserClient = CreateClient(IntegrationTestEnvironment.PrimaryUserToken);
        this.SecondaryUserClient = CreateClient(IntegrationTestEnvironment.SecondaryUserToken);
        this.AnonymousClient = CreateClient(null);
    }

    [OneTimeTearDown]
    public virtual void OneTimeTearDown()
    {
        this.PrimaryUserClient.Dispose();
        this.SecondaryUserClient.Dispose();
        this.AnonymousClient.Dispose();
    }

    protected static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    protected async Task<string> StartQueryAsync(HttpClient client, string question)
    {
        var response = await client.PostAsync("v1/query/start", JsonBody(new { question }));
        response.EnsureSuccessStatusCode();

        using var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("executionId").GetString()
            ?? throw new InvalidOperationException("query/start did not return an executionId.");
    }

    protected async Task<string> WaitForTerminalStatusAsync(HttpClient client, string executionId)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(3);

        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"v1/query/{executionId}/status");
            response.EnsureSuccessStatusCode();

            using var body = await ReadJsonAsync(response);
            var status = body.RootElement.GetProperty("status").GetString();
            if (status is "SUCCEEDED" or "FAILED" or "CANCELLED")
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException($"Query {executionId} did not reach a terminal state within 3 minutes.");
    }

    private static HttpClient CreateClient(string? token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(IntegrationTestEnvironment.BaseUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };

        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }
}
