# Pokepad.Gold.Api.PerformanceTests

[Pacer](https://github.com/jjnrmason/Pacer) load scenarios for measuring Pokepad API latency, throughput, and rate-limit behaviour against a deployed environment. Each scenario is a pipeline of measured steps that every virtual user runs as a journey under a load profile; Pacer measures latency and throughput itself and emits console, CSV, and self-contained HTML reports (and live metrics via [System.Diagnostics.Metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation)).

## Configuration

Set the target environment either as environment variables or in a gitignored `appsettings.Development.json`:

| Env var | appsettings key | Description |
|---|---|---|
| `POKEPAD_API_BASE_URL` | `ApiBaseUrl` | Base URL of the deployed API |
| `POKEPAD_PRIMARY_USER_TOKEN` | `UserToken` | Cognito JWT for a dedicated performance-test user |

## Scenarios

| Scenario | Group | What it does | Default load |
|---|---|---|---|
| `health` | `storefront` | Liveness probe | 10 users / 30s |
| `search` | `storefront` | Synchronous product search (OpenAI + Athena) | 3 users / 30s |
| `semantic-search` | `storefront` | Vector search over the embedding index | 5 users / 30s |
| `async-query-flow` | `query` | Start a query, poll status to completion, fetch results | 3 users / 1m |
| `rate-limit` | `query` | Hammers `v1/query/start` to exercise the limiter; 429s show grouped by status | Ramp to 15 |
| `cold-start` | — | Single search with no warm-up to capture cold-Lambda latency | 1 user / 1s |

Loads are kept modest because each search and query-start triggers a real OpenAI call and Athena scan, so concurrency drives spend. All questions target product data to keep scan costs predictable.

## Run

```sh
# list the registered scenarios
dotnet run -c Release -- list

# a single scenario, writing reports to ./reports
dotnet run -c Release -- run --scenario search --out ./reports

# a whole group, overriding the profile/users/duration from the command line
dotnet run -c Release -- run --group storefront --profile load --users 5 --duration 1

# rate-limit probe (ramps to 15 concurrent virtual users)
dotnet run -c Release -- run --scenario rate-limit

# cold start — wait ~10 minutes after the last traffic first
dotnet run -c Release -- run --scenario cold-start

# every scenario
dotnet run -c Release -- run --all --out ./reports
```

### Overrides

`run` accepts overrides that take precedence over what each scenario defines in code:

| Flag | Meaning |
|---|---|
| `--scenario` / `--group` / `--all` | Which scenario(s) to run |
| `--profile` | `load`, `soak`, `spike`, `stress`, or `ramp` |
| `--users` | Peak virtual users |
| `--duration` | Test duration, in minutes |
| `--warmup` | Warm-up duration, in minutes |
| `--out` | Output directory for CSV/HTML reports |
