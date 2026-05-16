using Pokepad.Gold.Api.Exceptions;
using Pokepad.Gold.Api.Models;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Endpoints.V1;

public static class SemanticSearchEndpoints
{
    public static void MapSemanticSearchEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/semantic-search", HandleAsync)
            .WithName("SemanticSearch")
            .WithSummary("Semantic product search")
            .WithDescription("Embeds a natural-language question, finds the nearest product embeddings in pgvector, and optionally synthesises a plain-English answer from those results.")
            .WithTags("Search")
            .Produces<SemanticSearchResponse>()
            .RequireAuthorization();
    }

    public static async Task<IResult> HandleAsync(
        SemanticSearchRequest request,
        SemanticSearchService semanticSearch,
        OpenAiService openAi)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new InputValidationException("Question is required.");
        }

        var topK = request.TopK > 0 ? request.TopK : 10;
        var results = await semanticSearch.SearchAsync(request.Question, topK);
        var answer = request.Synthesise
            ? await openAi.GenerateSemanticAnswerAsync(request.Question, results)
            : null;

        return Results.Ok(new SemanticSearchResponse(results, answer));
    }
}
