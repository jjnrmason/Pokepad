namespace Pokepad.Gold.Api.Models;

public sealed record SemanticSearchResponse(
    IReadOnlyList<SemanticSearchResult> Results,
    string? Answer);
