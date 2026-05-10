using NUnit.Framework;

namespace Pokepad.DataGeneration.Tests.ECommerceDataGenerator.WhenWorkingWithTheECommerceDataGenerator;

public partial class WhenWorkingWithTheECommerceDataGenerator
{
    public class AndValidatingProductData : ECommerceDataGeneratorTestBase
    {
        private static readonly string[] AllowedCategories =
            ["Electronics", "Clothing", "Home & Garden", "Sports", "Books", "Toys", "Beauty", "Automotive"];

        [Test]
        public void ThenAllProductsHaveANonEmptyName()
        {
            Assert.That(this.Result.Products.All(p => !string.IsNullOrWhiteSpace(p.Name)), Is.True);
        }

        [Test]
        public void ThenAllProductPricesAreAtLeastOne()
        {
            Assert.That(this.Result.Products.All(p => p.Price >= 1.0), Is.True);
        }

        [Test]
        public void ThenAllProductPricesAreAtMost999()
        {
            Assert.That(this.Result.Products.All(p => p.Price <= 999.0), Is.True);
        }

        [Test]
        public void ThenAllProductCategoriesAreFromTheAllowedSet()
        {
            Assert.That(this.Result.Products.All(p => AllowedCategories.Contains(p.Category)), Is.True);
        }

        [Test]
        public void ThenAllProductStockQuantitiesAreNonNegative()
        {
            Assert.That(this.Result.Products.All(p => p.StockQuantity >= 0), Is.True);
        }

        [Test]
        public void ThenAllProductIdsAreValidGuids()
        {
            Assert.That(this.Result.Products.All(p => Guid.TryParse(p.ProductId, out _)), Is.True);
        }
    }
}
