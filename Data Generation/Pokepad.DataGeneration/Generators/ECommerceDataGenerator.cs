using Bogus;
using Pokepad.Models;

namespace Pokepad.DataGeneration.Generators;

public class ECommerceDataGenerator
{
    private static readonly string[] OrderStatuses = ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"];
    private static readonly string[] ProductCategories = ["Electronics", "Clothing", "Home & Garden", "Sports", "Books", "Toys", "Beauty", "Automotive"];

    private readonly Faker _faker;

    public ECommerceDataGenerator(int seed = 42)
    {
        Randomizer.Seed = new Random(seed);
        _faker = new Faker();
    }
    
    public (List<Customer> Customers, List<Product> Products, List<Order> Orders, List<OrderItem> OrderItems) Generate(
        int customerCount = 500,
        int productCount = 200,
        int orderCount = 2000)
    {
        var customers = GenerateCustomers(customerCount);
        var products = GenerateProducts(productCount);
        var orders = GenerateOrders(customers, orderCount);
        var orderItems = GenerateOrderItems(orders, products);

        return (customers, products, orders, orderItems);
    }

    private List<Customer> GenerateCustomers(int count) =>
        Enumerable.Range(0, count).Select(_ => new Customer(
            _faker.Random.Guid().ToString(),
            _faker.Name.FirstName(),
            _faker.Name.LastName(),
            _faker.Internet.Email(),
            _faker.Phone.PhoneNumber(),
            _faker.Address.StreetAddress(),
            _faker.Address.City(),
            _faker.Address.Country(),
            _faker.Date.Past(3)
        )).ToList();

    private List<Product> GenerateProducts(int count) =>
        Enumerable.Range(0, count).Select(_ => new Product(
            _faker.Random.Guid().ToString(),
            _faker.Commerce.ProductName(),
            _faker.PickRandom(ProductCategories),
            _faker.Commerce.ProductDescription(),
            _faker.Finance.Amount(1, 999, 2),
            _faker.Random.Int(0, 500)
        )).ToList();

    private List<Order> GenerateOrders(List<Customer> customers, int count) =>
        Enumerable.Range(0, count).Select(_ =>
        {
            var customer = _faker.PickRandom(customers);
            return new Order(
                _faker.Random.Guid().ToString(),
                customer.CustomerId,
                _faker.Date.Past(2),
                _faker.PickRandom(OrderStatuses),
                _faker.Finance.Amount(5, 2000, 2),
                $"{_faker.Address.StreetAddress()}, {_faker.Address.City()}, {_faker.Address.Country()}"
            );
        }).ToList();

    private List<OrderItem> GenerateOrderItems(List<Order> orders, List<Product> products)
    {
        var items = new List<OrderItem>();

        foreach (var order in orders)
        {
            var itemCount = _faker.Random.Int(1, 5);
            for (var i = 0; i < itemCount; i++)
            {
                var product = _faker.PickRandom(products);
                var quantity = _faker.Random.Int(1, 10);
                var unitPrice = product.Price;
                items.Add(new OrderItem(
                    _faker.Random.Guid().ToString(),
                    order.OrderId,
                    product.ProductId,
                    quantity,
                    unitPrice,
                    Math.Round(unitPrice * quantity, 2)
                ));
            }
        }

        return items;
    }
}
