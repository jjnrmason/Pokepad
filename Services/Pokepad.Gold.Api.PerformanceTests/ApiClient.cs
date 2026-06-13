using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Pokepad.Gold.Api.PerformanceTests;

/// <summary>
/// A thin HTTP wrapper around the deployed Pokepad API. Pacer measures latency, throughput, and
/// status grouping itself, so this client just issues authenticated requests and surfaces the raw
/// <see cref="HttpResponseMessage"/> for steps to map onto a <see cref="Pacer.Steps.StepResult"/>.
/// A single instance is shared across virtual users — <see cref="HttpClient"/> is safe for
/// concurrent requests.
/// </summary>
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

    public Task<HttpResponseMessage> GetAsync(string path, CancellationToken cancellationToken) =>
        this._client.SendAsync(new HttpRequestMessage(HttpMethod.Get, path), cancellationToken);

    public Task<HttpResponseMessage> PostAsync(string path, object body, CancellationToken cancellationToken) =>
        this._client.SendAsync(new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        }, cancellationToken);

    /// <summary>Reads the <c>status</c> field from a <c>v1/query/{id}/status</c> response body.</summary>
    public static async Task<string?> ReadStatusAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("status").GetString();
    }

    /// <summary>Reads the <c>executionId</c> field from a <c>v1/query/start</c> response body.</summary>
    public static async Task<string> ReadExecutionIdAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("executionId").GetString()
            ?? throw new InvalidOperationException("query/start did not return an executionId.");
    }

    public void Dispose() => this._client.Dispose();
}
