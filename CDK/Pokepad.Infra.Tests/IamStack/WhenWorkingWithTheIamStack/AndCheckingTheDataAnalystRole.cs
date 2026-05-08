using NUnit.Framework;

namespace Pokepad.Infra.Tests.IamStack.WhenWorkingWithTheIamStack;

public partial class WhenWorkingWithTheIamStack
{
    public class AndCheckingTheDataAnalystRole : IamStackTestBase
    {
        [Test]
        public void ThenItExposesTheDataAnalystRoleAsAPublicProperty()
        {
            Assert.That(this.IamStack.DataAnalystRole, Is.Not.Null);
        }

        [Test]
        public void ThenTheRoleNameIsPokepadDataAnalyst()
        {
            Assert.That(this.FindRoleByName("pokepad-data-analyst"), Is.Not.Null);
        }

        [Test]
        public void ThenItTrustsTheAccountPrincipal()
        {
            var role = this.FindRoleByName("pokepad-data-analyst");
            Assert.That(this.HasAwsPrincipal(role!), Is.True);
        }

        [Test]
        public void ThenItAllowsAthenaQueryExecution()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("athena:StartQueryExecution"), Is.True);
        }

        [Test]
        public void ThenItAllowsGlueCatalogAccess()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("glue:GetDatabase"), Is.True);
        }

        [Test]
        public void ThenItAllowsReadingFromGold()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("s3:GetObject"), Is.True);
        }
    }
}
