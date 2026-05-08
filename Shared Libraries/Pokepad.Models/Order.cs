namespace Pokepad.Models;

public record Order(
    string OrderId,
    string CustomerId,
    DateTime OrderDate,
    string Status,
    double TotalAmount,
    string ShippingAddress
);
