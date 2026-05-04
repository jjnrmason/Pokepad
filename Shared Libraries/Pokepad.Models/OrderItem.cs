namespace Pokepad.Models;

public record OrderItem(
    string OrderItemId,
    string OrderId,
    string ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);
