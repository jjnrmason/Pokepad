namespace Pokepad.Models;

public record Customer(
    string CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Address,
    string City,
    string Country,
    DateTime CreatedAt
);
