using Pokepad.EmbeddingIndexer.Models;

namespace Pokepad.EmbeddingIndexer.Services;

public static class ProductTextFormatter
{
    public static string Format(ProductRecord product) =>
        $"{product.Name} {product.Description} {product.Category} price:{product.Price:F2}";
}
