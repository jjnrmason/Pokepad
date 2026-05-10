using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.SqlValidator.WhenWorkingWithTheSqlValidator;

public partial class WhenWorkingWithTheSqlValidator
{
    public class AndValidatingANonSelectStatement : SqlValidatorTestBase
    {
        [Test]
        public void ThenItThrowsAnInvalidOperationException()
        {
            Assert.That(
                () => this.SqlValidator.Validate("UPDATE products SET name = 'x' WHERE product_id = '1'"),
                Throws.TypeOf<InvalidOperationException>());
        }
    }
}
