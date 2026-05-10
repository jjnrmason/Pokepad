using NUnit.Framework;
using Pokepad.Models;

namespace Pokepad.DataGeneration.Tests.ECommerceDataGenerator.WhenWorkingWithTheECommerceDataGenerator;

public class ECommerceDataGeneratorTestBase
{
    protected const int CustomerCount = 10;
    protected const int ProductCount = 5;
    protected const int OrderCount = 20;

    protected global::Pokepad.DataGeneration.Generators.ECommerceDataGenerator ECommerceDataGenerator { get; private set; } = null!;
    protected (List<Customer> Customers, List<Product> Products, List<Order> Orders, List<OrderItem> OrderItems) Result { get; private set; }

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.ECommerceDataGenerator = new global::Pokepad.DataGeneration.Generators.ECommerceDataGenerator(seed: 42);
        this.Result = this.ECommerceDataGenerator.Generate(CustomerCount, ProductCount, OrderCount);
    }
}
