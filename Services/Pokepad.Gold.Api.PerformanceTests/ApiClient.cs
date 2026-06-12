using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Pokepad.Gold.Api.PerformanceTests;

public sealed class ApiClient : IDisposable
{
    public const string ProductQuestion = "Show me products in the electronics category";
    public const string SemanticQuestion = "Show me waterproof hiking boots under 100";

    private readonly HttpClient _client;

    public ApiClient()
    {
        this._client = new HttpClient
        {
            BaseAddress = new Uri(PerformanceTestEnvironment.BaseUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
        this._client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PerformanceTestEnvironment.UserToken);
    }

    public Task<HttpResponseMessage> GetAsync(string path, string endpoint) =>
        this.SendAsync(new HttpRequestMessage(HttpMethod.Get, path), endpoint);

    public Task<HttpResponseMessage> PostAsync(string path, object body, string endpoint) =>
        this.SendAsync(new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        }, endpoint);

    public async Task<string> StartQueryAsync()
    {
        using var response = await this.PostAsync("v1/query/start", new { question = ProductQuestion }, "query_start");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("executionId").GetString()
            ?? throw new InvalidOperationException("query/start did not return an executionId.");
    }

    public async Task WaitForQueryAsync(string executionId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var response = await this.GetAsync($"v1/query/{executionId}/status", "query_status");
            response.EnsureSuccessStatusCode();

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var status = body.RootElement.GetProperty("status").GetString();
            if (status == "SUCCEEDED")
            {
                return;
            }

            if (status is "FAILED" or "CANCELLED")
            {
                throw new InvalidOperationException($"Query {executionId} finished with status {status}.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"Query {executionId} did not complete within {timeout.TotalSeconds:F0} s.");
    }

    public void Dispose() => this._client.Dispose();

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string endpoint)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var response = await this._client.SendAsync(request);
        PokepadApiMetrics.RecordRequest(endpoint, (int)response.StatusCode, Stopwatch.GetElapsedTime(startedAt));

        return response;
    }
}
