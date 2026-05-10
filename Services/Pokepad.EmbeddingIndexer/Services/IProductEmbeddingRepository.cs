using Pokepad.EmbeddingIndexer.Models;

namespace Pokepad.EmbeddingIndexer.Services;

public interface IProductEmbeddingRepository
{
    Task UpsertAsync(ProductRecord product, float[] embedding);
}
