using Microsoft.Extensions.DependencyInjection;
using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pokepad.Gold.Api.PerformanceTests.Scenarios;

/// <summary>
/// Pacer load scenarios for the deployed Pokepad API. Each scenario is a measured pipeline run by
/// every virtual user under a load profile; the <c>storefront</c> and <c>query</c> groups let
/// related scenarios be run together. Loads are deliberately modest because each search and
/// query-start triggers a real OpenAI call and Athena scan, so concurrency drives spend. Override
/// the shape from the command line with <c>--profile</c>, <c>--users</c>, and <c>--duration</c>.
/// </summary>
public static class PokepadScenarios
{
    /// <summary>Cheap liveness probe — the only endpoint comfortable under real concurrency.</summary>
    public static Scenario Health() => Scenario.Create("health")
        .InGroup("storefront")
        .AddStep("health", ctx => Send(ctx, api => api.GetAsync("v1/health", ctx.CancellationToken)))
        .WithLoad(LoadProfiles.Load(users: 10, duration: TimeSpan.FromSeconds(30)));

    /// <summary>Synchronous product search — kept to a trickle since every call hits OpenAI and Athena.</summary>
    public static Scenario Search() => Scenario.Create("search")
        .InGroup("storefront")
        .WithWarmup(TimeSpan.FromSeconds(5))
        .AddStep("search", ctx => Send(ctx, api =>
            api.PostAsync("v1/search", new { question = ApiClient.ProductQuestion }, ctx.CancellationToken)))
        .WithLoad(LoadProfiles.Load(users: 3, duration: TimeSpan.FromSeconds(30)));

    /// <summary>Vector search over the embedding index.</summary>
    public static Scenario SemanticSearch() => Scenario.Create("semantic-search")
        .InGroup("storefront")
        .WithWarmup(TimeSpan.FromSeconds(5))
        .AddStep("semantic-search", ctx => Send(ctx, api => api.PostAsync(
            "v1/semantic-search",
            new { question = ApiClient.SemanticQuestion, topK = 10, synthesise = false },
            ctx.CancellationToken)))
        .WithLoad(LoadProfiles.Load(users: 5, duration: TimeSpan.FromSeconds(30)));

    /// <summary>
    /// The full async query journey: start a query, poll its status to completion, then fetch the
    /// results. The execution id flows down the pipeline via <see cref="IStepContext.Previous"/>.
    /// </summary>
    public static Scenario AsyncQueryFlow() => Scenario.Create("async-query-flow")
        .InGroup("query")
        .AddStep("start", async ctx =>
        {
            var api = Client(ctx);
            using var response = await api.PostAsync(
                "v1/query/start", new { question = ApiClient.ProductQuestion }, ctx.CancellationToken);
            if (!response.IsSuccessStatusCode)
                return StepResult.Fail(status: Status(response));

            var executionId = await ApiClient.ReadExecutionIdAsync(response);
            return StepResult.Ok(payload: executionId, status: Status(response));
        })
        .AddStep("poll-status", async ctx =>
        {
            var api = Client(ctx);
            var executionId = ctx.PreviousAs<string>()!;
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(2);

            while (DateTime.UtcNow < deadline)
            {
                using var response = await api.GetAsync($"v1/query/{executionId}/status", ctx.CancellationToken);
                if (!response.IsSuccessStatusCode)
                    return StepResult.Fail(status: Status(response));

                var status = await ApiClient.ReadStatusAsync(response);
                if (status == "SUCCEEDED")
                    return StepResult.Ok(payload: executionId, status: status);
                if (status is "FAILED" or "CANCELLED")
                    return StepResult.Fail(status: status);

                await Task.Delay(TimeSpan.FromSeconds(1), ctx.CancellationToken);
            }

            return StepResult.Fail(status: "timeout");
        })
        .AddStep("results", ctx =>
        {
            var executionId = ctx.PreviousAs<string>()!;
            return Send(ctx, api => api.GetAsync($"v1/query/{executionId}/results", ctx.CancellationToken));
        })
        .WithLoad(LoadProfiles.Load(users: 3, duration: TimeSpan.FromMinutes(1)));

    /// <summary>
    /// Hammers <c>v1/query/start</c> to exercise the rate limiter. Accepted (202) requests are
    /// reported as successes; throttled (429) requests show up grouped under that status, so the
    /// report makes the limit visible.
    /// </summary>
    public static Scenario RateLimit() => Scenario.Create("rate-limit")
        .InGroup("query")
        .AddStep("query-start", ctx => Send(ctx, api =>
            api.PostAsync("v1/query/start", new { question = ApiClient.ProductQuestion }, ctx.CancellationToken)))
        .WithLoad(LoadProfiles.Ramp(
            peak: 15,
            rampUp: TimeSpan.FromSeconds(10),
            hold: TimeSpan.FromSeconds(20),
            rampDown: TimeSpan.FromSeconds(10)));

    /// <summary>
    /// A single search against a (hopefully) cold Lambda — run after a quiet period, with no
    /// warm-up, so the first invocation latency is captured.
    /// </summary>
    public static Scenario ColdStart() => Scenario.Create("cold-start")
        .AddStep("search", ctx => Send(ctx, api =>
            api.PostAsync("v1/search", new { question = ApiClient.ProductQuestion }, ctx.CancellationToken)))
        .WithLoad(LoadProfiles.Load(users: 1, duration: TimeSpan.FromSeconds(1)));

    private static ApiClient Client(IStepContext ctx) => ctx.Services.GetRequiredService<ApiClient>();

    private static string Status(HttpResponseMessage response) => ((int)response.StatusCode).ToString();

    /// <summary>Issues a request, mapping the HTTP status onto a <see cref="StepResult"/> and recording the response size.</summary>
    private static async ValueTask<StepResult> Send(IStepContext ctx, Func<ApiClient, Task<HttpResponseMessage>> send)
    {
        using var response = await send(Client(ctx));
        var status = Status(response);
        if (!response.IsSuccessStatusCode)
            return StepResult.Fail(status: status);

        var body = await response.Content.ReadAsByteArrayAsync(ctx.CancellationToken);
        return StepResult.Ok(bytesReceived: body.Length, status: status);
    }
}
