using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Pokepad.Gold.Api.PerformanceTests.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 10, invocationCount: 1)]
public class EndpointBenchmarks
{
    private ApiClient _client = null!;
    private string _executionId = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        this._client = new ApiClient();
        this._executionId = await this._client.StartQueryAsync();
        await this._client.WaitForQueryAsync(this._executionId, TimeSpan.FromMinutes(2));
    }

    [GlobalCleanup]
    public void GlobalCleanup() => this._client.Dispose();

    [Benchmark]
    public async Task Health()
    {
        using var response = await this._client.GetAsync("v1/health", "health");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task QueryStart()
    {
        using var response = await this._client.PostAsync("v1/query/start", new { question = ApiClient.ProductQuestion }, "query_start");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task QueryStatus()
    {
        using var response = await this._client.GetAsync($"v1/query/{this._executionId}/status", "query_status");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task QueryResults()
    {
        using var response = await this._client.GetAsync($"v1/query/{this._executionId}/results", "query_results");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task Search()
    {
        using var response = await this._client.PostAsync("v1/search", new { question = ApiClient.ProductQuestion }, "search");
        response.EnsureSuccessStatusCode();
    }

    [Benchmark]
    public async Task SemanticSearch()
    {
        using var response = await this._client.PostAsync("v1/semantic-search", new { question = ApiClient.SemanticQuestion, topK = 10, synthesise = false }, "semantic_search");
        response.EnsureSuccessStatusCode();
    }
}
