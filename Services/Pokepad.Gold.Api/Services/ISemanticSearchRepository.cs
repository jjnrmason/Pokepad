using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Services;

public interface ISemanticSearchRepository
{
    Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(float[] queryEmbedding, int limit);
}
