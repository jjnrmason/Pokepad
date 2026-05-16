namespace Pokepad.Gold.Api.Models;

public sealed record SemanticSearchRequest(
    string? Question,
    int TopK = 10,
    bool Synthesise = false);
