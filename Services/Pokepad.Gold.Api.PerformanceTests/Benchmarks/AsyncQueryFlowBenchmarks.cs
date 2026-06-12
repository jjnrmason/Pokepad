using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Pokepad.Gold.Api.PerformanceTests.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 0, iterationCount: 5, invocationCount: 1)]
public class AsyncQueryFlowBenchmarks
{
    private ApiClient _client = null!;

    [GlobalSetup]
    public void GlobalSetup() => this._client = new ApiClient();

    [GlobalCleanup]
    public void GlobalCleanup() => this._client.Dispose();

    [Benchmark]
    public async Task StartPollAndFetchResults()
    {
        var executionId = await this._client.StartQueryAsync();
        await this._client.WaitForQueryAsync(executionId, TimeSpan.FromMinutes(2));

        using var response = await this._client.GetAsync($"v1/query/{executionId}/results", "query_results");
        response.EnsureSuccessStatusCode();
    }
}
