using NUnit.Framework;

namespace Pokepad.DataGeneration.Tests.ECommerceDataGenerator.WhenWorkingWithTheECommerceDataGenerator;

public partial class WhenWorkingWithTheECommerceDataGenerator
{
    public class AndValidatingOrderData : ECommerceDataGeneratorTestBase
    {
        private static readonly string[] AllowedStatuses =
            ["Pending", "Processing", "Shipped", "Delivered", "Cancelled"];

        [Test]
        public void ThenAllOrdersReferenceACustomerThatExists()
        {
            var customerIds = this.Result.Customers.Select(c => c.CustomerId).ToHashSet();
            Assert.That(this.Result.Orders.All(o => customerIds.Contains(o.CustomerId)), Is.True);
        }

        [Test]
        public void ThenAllOrderStatusesAreFromTheAllowedSet()
        {
            Assert.That(this.Result.Orders.All(o => AllowedStatuses.Contains(o.Status)), Is.True);
        }

        [Test]
        public void ThenAllOrderTotalAmountsArePositive()
        {
            Assert.That(this.Result.Orders.All(o => o.TotalAmount > 0), Is.True);
        }

        [Test]
        public void ThenAllOrdersHaveANonEmptyShippingAddress()
        {
            Assert.That(this.Result.Orders.All(o => !string.IsNullOrWhiteSpace(o.ShippingAddress)), Is.True);
        }

        [Test]
        public void ThenAllOrderDatesAreInThePast()
        {
            Assert.That(this.Result.Orders.All(o => o.OrderDate < DateTime.Now), Is.True);
        }
    }
}
