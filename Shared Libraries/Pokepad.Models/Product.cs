namespace Pokepad.Models;

public record Product(
    string ProductId,
    string Name,
    string Category,
    string Description,
    double Price,
    int StockQuantity
);
