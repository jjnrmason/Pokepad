# Pokepad.Gold.Api.PerformanceTests

BenchmarkDotNet benchmarks and load scenarios for measuring Pokepad API latency and rate-limit behaviour against a deployed environment. Every HTTP call is instrumented with [System.Diagnostics.Metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation) — a `Counter` for requests, a `Counter` for 429 responses, and a `Histogram` for round-trip duration — so runs can also be observed live with `dotnet-counters`.

## Configuration

Set the target environment either as environment variables or in a gitignored `appsettings.Development.json`:

| Env var | appsettings key | Description |
|---|---|---|
| `POKEPAD_API_BASE_URL` | `ApiBaseUrl` | Base URL of the deployed API |
| `POKEPAD_PRIMARY_USER_TOKEN` | `UserToken` | Cognito JWT for a dedicated performance-test user |

## Run

```sh
# latency benchmarks (per endpoint + full async flow)
dotnet run -c Release -- --filter '*'

# rate-limit probe: ramp to 15 concurrent workers and count 202 vs 429
dotnet run -c Release -- rate-limit

# cold start: single search request — wait ~10 minutes after the last traffic first
dotnet run -c Release -- cold-start
```

Benchmarks use the `Monitoring` run strategy with a handful of iterations per endpoint, since each search/query-start iteration triggers a real OpenAI call and Athena scan. All questions target product data to keep Athena scan costs predictable.

## Observing counters live

```sh
dotnet-counters monitor --process-id <pid> --counters Pokepad.Gold.Api.PerformanceTests
```

The rate-limit and cold-start scenarios also print a summary of the collected counters and latency percentiles when they finish.
