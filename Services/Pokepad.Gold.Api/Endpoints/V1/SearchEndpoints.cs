using Pokepad.Gold.Api.Middleware;
using Pokepad.Gold.Api.Models;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Endpoints.V1;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/search", async (
            SearchRequest request,
            GlueSchemaService glue,
            OpenAiService openAi,
            AthenaService athena,
            SqlValidator validator) =>
        {
            var schema = await glue.GetSchemaAsync();
            var sql = await openAi.GenerateSqlAsync(request.Question, schema);

            validator.Validate(sql);

            var results = await athena.ExecuteAsync(sql);
            return Results.Ok(new SearchResponse(sql, results.Columns, results.Rows));
        })
            .WithName("Search")
            .WithSummary("Synchronous natural language search")
            .WithDescription("Translates a natural-language question into SQL via OpenAI, executes it against Athena, and returns the results. Blocks until the query completes (up to the 30 s Lambda timeout).")
            .WithTags("Search")
            .Produces<SearchResponse>()
            .RequireAuthorization();
    }
}
