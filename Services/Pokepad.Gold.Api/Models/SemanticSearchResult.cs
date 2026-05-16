namespace Pokepad.Gold.Api.Models;

public sealed record SemanticSearchResult(
    string ProductId,
    string Metadata,
    double SimilarityScore);
