using System.Diagnostics;

namespace Pokepad.Gold.Api.PerformanceTests.Scenarios;

public static class ColdStartProbe
{
    public static async Task RunAsync()
    {
        using var metrics = new MetricsSummary();
        using var client = new ApiClient();

        Console.WriteLine("Sending a single search request against a (hopefully) cold Lambda...");
        var startedAt = Stopwatch.GetTimestamp();
        using var response = await client.PostAsync("v1/search", new { question = ApiClient.ProductQuestion }, "search_cold_start");
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        Console.WriteLine($"Status: {(int)response.StatusCode} in {elapsed.TotalMilliseconds:F0} ms");
        metrics.Print(Console.Out);
    }
}
