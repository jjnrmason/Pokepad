namespace Pokepad.EmbeddingIndexer.Models;

public sealed class ProductRecord
{
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public double Price { get; set; }
}
