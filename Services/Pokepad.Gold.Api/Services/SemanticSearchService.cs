using Pokepad.Gold.Api.Exceptions;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Services;

public sealed class SemanticSearchService(
    IModerationService moderationService,
    IEmbeddingService embeddingService,
    ISemanticSearchRepository searchRepository,
    ILogger<SemanticSearchService> logger)
{
    private const int DefaultLimit = 10;

    public Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(string question) =>
        this.SearchAsync(question, DefaultLimit);

    public async Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(string question, int limit)
    {
        logger.LogInformation("Running semantic search for question: {Question}", question);

        if (await moderationService.IsFlaggedAsync(question))
        {
            throw new InputValidationException("Input was flagged by content moderation.");
        }

        var embedding = await embeddingService.EmbedAsync(question);
        var results = await searchRepository.SearchAsync(embedding, limit);

        logger.LogInformation("Semantic search returned {ResultCount} results", results.Count);

        return results;
    }
}
