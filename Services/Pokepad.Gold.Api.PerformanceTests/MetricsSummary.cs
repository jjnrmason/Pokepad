using System.Diagnostics.Metrics;

namespace Pokepad.Gold.Api.PerformanceTests;

public sealed class MetricsSummary : IDisposable
{
    private readonly MeterListener _listener;
    private readonly Dictionary<string, long> _requestCounts = new();
    private readonly Dictionary<string, List<double>> _durations = new();
    private readonly Lock _lock = new();

    public MetricsSummary()
    {
        this._listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == PokepadApiMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        this._listener.SetMeasurementEventCallback<long>(this.OnCounterMeasurement);
        this._listener.SetMeasurementEventCallback<double>(this.OnHistogramMeasurement);
        this._listener.Start();
    }

    public void Print(TextWriter writer)
    {
        lock (this._lock)
        {
            writer.WriteLine();
            writer.WriteLine("Requests by endpoint and status:");
            foreach (var (key, count) in this._requestCounts.OrderBy(pair => pair.Key))
            {
                writer.WriteLine($"  {key}: {count}");
            }

            writer.WriteLine();
            writer.WriteLine("Latency by endpoint (ms):");
            foreach (var (endpoint, values) in this._durations.OrderBy(pair => pair.Key))
            {
                var sorted = values.Order().ToList();
                writer.WriteLine(
                    $"  {endpoint}: count={sorted.Count} p50={Percentile(sorted, 0.50):F0} p95={Percentile(sorted, 0.95):F0} max={sorted[^1]:F0}");
            }
        }
    }

    public void Dispose() => this._listener.Dispose();

    private static double Percentile(IReadOnlyList<double> sorted, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }

    private static string? TagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string name)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == name)
            {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private void OnCounterMeasurement(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        if (instrument.Name != "pokepad.api.client.requests")
        {
            return;
        }

        var key = $"{TagValue(tags, "endpoint")} [{TagValue(tags, "http.response.status_code")}]";
        lock (this._lock)
        {
            this._requestCounts[key] = this._requestCounts.GetValueOrDefault(key) + measurement;
        }
    }

    private void OnHistogramMeasurement(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        if (instrument.Name != "pokepad.api.client.request.duration")
        {
            return;
        }

        var endpoint = TagValue(tags, "endpoint") ?? "unknown";
        lock (this._lock)
        {
            if (!this._durations.TryGetValue(endpoint, out var values))
            {
                values = [];
                this._durations[endpoint] = values;
            }

            values.Add(measurement);
        }
    }
}
