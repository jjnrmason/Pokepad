using Npgsql;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Services;

public sealed class PgVectorSemanticSearchRepository(NpgsqlDataSource dataSource) : ISemanticSearchRepository
{
    public async Task<IReadOnlyList<SemanticSearchResult>> SearchAsync(float[] queryEmbedding, int limit)
    {
        var vector = $"[{string.Join(",", queryEmbedding)}]";
        var results = new List<SemanticSearchResult>();

        await using var command = dataSource.CreateCommand("""
            SELECT product_id, metadata::text, embedding <=> @queryEmbedding::vector AS distance
            FROM products_embeddings
            ORDER BY distance
            LIMIT @limit
            """);

        command.Parameters.AddWithValue("queryEmbedding", vector);
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var productId = reader.GetString(0);
            var metadata = reader.GetString(1);
            var distance = reader.GetDouble(2);

            results.Add(new SemanticSearchResult(
                productId,
                metadata,
                1 - distance));
        }

        return results;
    }
}
