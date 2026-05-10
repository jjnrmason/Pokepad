using NUnit.Framework;

namespace Pokepad.DataGeneration.Tests.ECommerceDataGenerator.WhenWorkingWithTheECommerceDataGenerator;

public partial class WhenWorkingWithTheECommerceDataGenerator
{
    public class AndGeneratingData : ECommerceDataGeneratorTestBase
    {
        [Test]
        public void ThenItReturnsTheRequestedNumberOfCustomers()
        {
            Assert.That(this.Result.Customers, Has.Count.EqualTo(CustomerCount));
        }

        [Test]
        public void ThenItReturnsTheRequestedNumberOfProducts()
        {
            Assert.That(this.Result.Products, Has.Count.EqualTo(ProductCount));
        }

        [Test]
        public void ThenItReturnsTheRequestedNumberOfOrders()
        {
            Assert.That(this.Result.Orders, Has.Count.EqualTo(OrderCount));
        }

        [Test]
        public void ThenAllCustomerIdsAreUnique()
        {
            var ids = this.Result.Customers.Select(c => c.CustomerId).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }

        [Test]
        public void ThenAllProductIdsAreUnique()
        {
            var ids = this.Result.Products.Select(p => p.ProductId).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }

        [Test]
        public void ThenAllOrderIdsAreUnique()
        {
            var ids = this.Result.Orders.Select(o => o.OrderId).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }
    }
}
