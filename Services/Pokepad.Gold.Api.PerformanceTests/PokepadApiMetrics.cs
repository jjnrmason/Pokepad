using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Pokepad.Gold.Api.PerformanceTests;

public static class PokepadApiMetrics
{
    public const string MeterName = "Pokepad.Gold.Api.PerformanceTests";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        name: "pokepad.api.client.requests",
        unit: "{request}",
        description: "Number of HTTP requests sent to the Pokepad API.");

    private static readonly Counter<long> RateLimitedRequests = Meter.CreateCounter<long>(
        name: "pokepad.api.client.rate_limited_requests",
        unit: "{request}",
        description: "Number of requests rejected with 429 Too Many Requests.");

    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        name: "pokepad.api.client.request.duration",
        unit: "ms",
        description: "Round-trip duration of HTTP requests to the Pokepad API.");

    public static void RecordRequest(string endpoint, int statusCode, TimeSpan duration)
    {
        var tags = new TagList
        {
            { "endpoint", endpoint },
            { "http.response.status_code", statusCode }
        };

        Requests.Add(1, tags);
        RequestDuration.Record(duration.TotalMilliseconds, tags);

        if (statusCode == 429)
        {
            RateLimitedRequests.Add(1, new TagList { { "endpoint", endpoint } });
        }
    }
}
