using NUnit.Framework;

namespace Pokepad.Infra.Tests.CognitoStack.WhenWorkingWithTheCognitoConstruct;

public partial class WhenWorkingWithTheCognitoConstruct
{
    public class AndCheckingThePasswordPolicy : CognitoConstructTestBase
    {
        private IDictionary<string, object> GetPasswordPolicy()
        {
            var props = this.GetUserPoolProps();
            var policies = (IDictionary<string, object>)props["Policies"];
            return (IDictionary<string, object>)policies["PasswordPolicy"];
        }

        [Test]
        public void ThenMinimumLengthIsEight()
        {
            Assert.That(Convert.ToInt32(this.GetPasswordPolicy()["MinimumLength"]), Is.EqualTo(8));
        }

        [Test]
        public void ThenLowercaseIsRequired()
        {
            Assert.That(this.GetPasswordPolicy()["RequireLowercase"], Is.True);
        }

        [Test]
        public void ThenUppercaseIsRequired()
        {
            Assert.That(this.GetPasswordPolicy()["RequireUppercase"], Is.True);
        }

        [Test]
        public void ThenDigitsAreRequired()
        {
            Assert.That(this.GetPasswordPolicy()["RequireNumbers"], Is.True);
        }

        [Test]
        public void ThenSymbolsAreNotRequired()
        {
            Assert.That(this.GetPasswordPolicy()["RequireSymbols"], Is.False);
        }
    }
}
