using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.SqlValidator.WhenWorkingWithTheSqlValidator;

public partial class WhenWorkingWithTheSqlValidator
{
    public class AndValidatingASelectStatement : SqlValidatorTestBase
    {
        [Test]
        public void ThenItDoesNotThrow()
        {
            Assert.That(() => this.SqlValidator.Validate("SELECT product_id FROM products LIMIT 10"), Throws.Nothing);
        }
    }
}
