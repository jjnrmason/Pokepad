using System.Net;

namespace Pokepad.Gold.Api.PerformanceTests.Scenarios;

public static class RateLimitProbe
{
    private static readonly (int Workers, TimeSpan Duration)[] Stages =
    [
        (15, TimeSpan.FromSeconds(30)),
        (15, TimeSpan.FromSeconds(30)),
        (5, TimeSpan.FromSeconds(30))
    ];

    public static async Task RunAsync()
    {
        using var metrics = new MetricsSummary();
        using var client = new ApiClient();

        var accepted = 0;
        var rateLimited = 0;
        var other = 0;

        foreach (var (workers, duration) in Stages)
        {
            Console.WriteLine($"Stage: {workers} concurrent workers for {duration.TotalSeconds:F0} s");
            var stageEnd = DateTime.UtcNow + duration;

            var tasks = Enumerable.Range(0, workers).Select(async _ =>
            {
                while (DateTime.UtcNow < stageEnd)
                {
                    using var response = await client.PostAsync("v1/query/start", new { question = ApiClient.ProductQuestion }, "query_start");
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Accepted:
                            Interlocked.Increment(ref accepted);
                            break;
                        case HttpStatusCode.TooManyRequests:
                            Interlocked.Increment(ref rateLimited);
                            break;
                        default:
                            Interlocked.Increment(ref other);
                            break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });

            await Task.WhenAll(tasks);
        }

        Console.WriteLine();
        Console.WriteLine($"Accepted (202): {accepted}");
        Console.WriteLine($"Rate limited (429): {rateLimited}");
        Console.WriteLine($"Other responses: {other}");

        metrics.Print(Console.Out);

        if (rateLimited == 0)
        {
            Console.WriteLine();
            Console.WriteLine("Warning: no 429 responses observed — the probe never hit the rate limit.");
        }
    }
}
