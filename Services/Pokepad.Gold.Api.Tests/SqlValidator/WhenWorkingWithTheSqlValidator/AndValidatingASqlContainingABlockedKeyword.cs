using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.SqlValidator.WhenWorkingWithTheSqlValidator;

public partial class WhenWorkingWithTheSqlValidator
{
    public class AndValidatingASqlContainingABlockedKeyword : SqlValidatorTestBase
    {
        [Test]
        public void ThenItThrowsAnInvalidOperationException()
        {
            Assert.That(
                () => this.SqlValidator.Validate("SELECT * FROM products; DROP TABLE products"),
                Throws.TypeOf<InvalidOperationException>());
        }
    }
}
