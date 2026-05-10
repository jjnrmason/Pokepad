using System.Text.Json;
using Npgsql;
using Pokepad.EmbeddingIndexer.Models;

namespace Pokepad.EmbeddingIndexer.Services;

public sealed class ProductEmbeddingRepository : IProductEmbeddingRepository
{
    private readonly NpgsqlConnection _conn;

    public ProductEmbeddingRepository(NpgsqlConnection conn)
    {
        _conn = conn;
    }

    public async Task UpsertAsync(ProductRecord product, float[] embedding)
    {
        var vectorStr = $"[{string.Join(",", embedding)}]";
        var metadata = JsonSerializer.Serialize(new { product.Name, product.Category, product.Price });

        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO products_embeddings (product_id, embedding, metadata)
            VALUES (@productId, @embedding::vector, @metadata::jsonb)
            ON CONFLICT (product_id) DO UPDATE
            SET embedding = EXCLUDED.embedding,
                metadata  = EXCLUDED.metadata
            """;
        cmd.Parameters.AddWithValue("productId", product.ProductId);
        cmd.Parameters.AddWithValue("embedding", vectorStr);
        cmd.Parameters.AddWithValue("metadata", metadata);
        await cmd.ExecuteNonQueryAsync();
    }
}
