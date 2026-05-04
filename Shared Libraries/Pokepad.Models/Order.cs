namespace Pokepad.Models;

public record Order(
    string OrderId,
    string CustomerId,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    string ShippingAddress
);
