using System.Text.Json;
using Amazon.Athena;
using Pokepad.Gold.Api.Middleware;
using Pokepad.Gold.Api.Models;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Endpoints.V1;

public static class QueryEndpoints
{
    public static void MapQueryEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/query/start", async (
            HttpContext ctx,
            SearchRequest request,
            GlueSchemaService glue,
            OpenAiService openAi,
            AthenaService athena,
            SqlValidator validator,
            QueryTrackingService tracking) =>
        {
            var userId = GetUserId(ctx);

            var schema = await glue.GetSchemaAsync();
            var sql = await openAi.GenerateSqlAsync(request.Question, schema);
            validator.Validate(sql);

            var executionId = await athena.StartAsync(sql);
            await tracking.TrackAsync(executionId, userId);

            return Results.Accepted($"/v1/query/{executionId}/status", new { executionId });
        })
            .WithName("StartQuery")
            .WithSummary("Start an asynchronous query")
            .WithDescription("Starts an Athena query and returns immediately with an executionId. Poll /v1/query/{id}/status until SUCCEEDED, then fetch results from /v1/query/{id}/results.")
            .WithTags("Async Query")
            .RequireAuthorization();

        routes.MapGet("/query/{id}/status", async (
            HttpContext ctx,
            string id,
            AthenaService athena,
            QueryTrackingService tracking) =>
        {
            var userId = GetUserId(ctx);

            var owner = await tracking.GetOwnerAsync(id);
            if (owner is null) return Results.NotFound();
            if (owner != userId) return Results.Forbid();

            var execution = await athena.GetExecutionAsync(id);
            return Results.Ok(new { executionId = id, status = execution.Status.State.Value });
        })
            .WithName("GetQueryStatus")
            .WithSummary("Poll query status")
            .WithDescription("Returns the current Athena execution state (QUEUED, RUNNING, SUCCEEDED, FAILED, CANCELLED). Returns 404 if unknown/expired, 403 if not the query owner.")
            .WithTags("Async Query")
            .RequireAuthorization();

        routes.MapGet("/query/{id}/results", async (
            HttpContext ctx,
            string id,
            AthenaService athena,
            QueryTrackingService tracking) =>
        {
            var userId = GetUserId(ctx);

            var owner = await tracking.GetOwnerAsync(id);
            if (owner is null) return Results.NotFound();
            if (owner != userId) return Results.Forbid();

            var execution = await athena.GetExecutionAsync(id);
            if (execution.Status.State != QueryExecutionState.SUCCEEDED)
                return Results.Conflict(new { executionId = id, status = execution.Status.State.Value });

            var results = await athena.FetchResultsAsync(id);
            return Results.Ok(new SearchResponse(execution.Query, results.Columns, results.Rows));
        })
            .WithName("GetQueryResults")
            .WithSummary("Fetch results for a completed query")
            .WithDescription("Returns query results once status is SUCCEEDED. Returns 409 Conflict with the current status if the query is still running.")
            .WithTags("Async Query")
            .Produces<SearchResponse>()
            .RequireAuthorization();
    }

    // API Gateway validates the JWT before forwarding to Lambda, so decoding without
    // re-validating the signature here is safe and avoids an outbound JWKS fetch on cold start.
    private static string GetUserId(HttpContext ctx)
    {
        var auth = ctx.Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized().ToString()!; // unreachable past the authorizer

        var parts = auth[7..].Split('.');
        if (parts.Length < 2) throw new InvalidOperationException("Malformed JWT");

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
        return doc.RootElement.TryGetProperty("sub", out var sub)
            ? sub.GetString() ?? throw new InvalidOperationException("Empty sub claim")
            : throw new InvalidOperationException("No sub claim in JWT");
    }
}
