using NUnit.Framework;
using Pokepad.EmbeddingIndexer.Models;
using Pokepad.EmbeddingIndexer.Services;

namespace Pokepad.EmbeddingIndexer.Tests.ProductTextFormatter.WhenWorkingWithTheProductTextFormatter;

public partial class WhenWorkingWithTheProductTextFormatter
{
    public class AndFormattingAProduct : ProductTextFormatterTestBase
    {
        [Test]
        public void ThenItReturnsTheExpectedFormatString()
        {
            var product = new ProductRecord
            {
                Name = "Charizard",
                Description = "A powerful fire-type Pokemon",
                Category = "Pokemon Cards",
                Price = 9.99
            };

            var result = Services.ProductTextFormatter.Format(product);

            Assert.That(result, Is.EqualTo("Charizard A powerful fire-type Pokemon Pokemon Cards price:9.99"));
        }

        [Test]
        public void ThenItFormatsZeroPriceWithTwoDecimalPlaces()
        {
            var product = new ProductRecord
            {
                Name = "Bulbasaur",
                Description = "A grass-type Pokemon",
                Category = "Pokemon Cards",
                Price = 0
            };

            var result = Services.ProductTextFormatter.Format(product);

            Assert.That(result, Is.EqualTo("Bulbasaur A grass-type Pokemon Pokemon Cards price:0.00"));
        }

        [Test]
        public void ThenItFormatsPriceWithExactlyTwoDecimalPlaces()
        {
            var product = new ProductRecord
            {
                Name = "Pikachu",
                Description = "An electric-type Pokemon",
                Category = "Pokemon Cards",
                Price = 12.5
            };

            var result = Services.ProductTextFormatter.Format(product);

            Assert.That(result, Is.EqualTo("Pikachu An electric-type Pokemon Pokemon Cards price:12.50"));
        }
    }
}
