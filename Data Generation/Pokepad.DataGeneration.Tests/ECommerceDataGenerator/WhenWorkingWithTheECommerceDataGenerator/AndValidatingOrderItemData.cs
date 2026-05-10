using NUnit.Framework;

namespace Pokepad.DataGeneration.Tests.ECommerceDataGenerator.WhenWorkingWithTheECommerceDataGenerator;

public partial class WhenWorkingWithTheECommerceDataGenerator
{
    public class AndValidatingOrderItemData : ECommerceDataGeneratorTestBase
    {
        [Test]
        public void ThenEachOrderHasAtLeastOneOrderItem()
        {
            var orderIdsWithItems = this.Result.OrderItems.Select(i => i.OrderId).ToHashSet();
            Assert.That(this.Result.Orders.All(o => orderIdsWithItems.Contains(o.OrderId)), Is.True);
        }

        [Test]
        public void ThenAllOrderItemsReferenceAValidOrder()
        {
            var orderIds = this.Result.Orders.Select(o => o.OrderId).ToHashSet();
            Assert.That(this.Result.OrderItems.All(i => orderIds.Contains(i.OrderId)), Is.True);
        }

        [Test]
        public void ThenAllOrderItemsReferenceAValidProduct()
        {
            var productIds = this.Result.Products.Select(p => p.ProductId).ToHashSet();
            Assert.That(this.Result.OrderItems.All(i => productIds.Contains(i.ProductId)), Is.True);
        }

        [Test]
        public void ThenAllOrderItemQuantitiesAreAtLeastOne()
        {
            Assert.That(this.Result.OrderItems.All(i => i.Quantity >= 1), Is.True);
        }

        [Test]
        public void ThenAllOrderItemSubtotalsMatchUnitPriceMultipliedByQuantity()
        {
            Assert.That(
                this.Result.OrderItems.All(i => i.Subtotal == Math.Round(i.UnitPrice * i.Quantity, 2)),
                Is.True);
        }

        [Test]
        public void ThenAllOrderItemIdsAreUnique()
        {
            var ids = this.Result.OrderItems.Select(i => i.OrderItemId).ToList();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
        }
    }
}
