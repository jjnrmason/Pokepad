using NUnit.Framework;

namespace Pokepad.Infra.Tests.IamStack.WhenWorkingWithTheIamStack;

public partial class WhenWorkingWithTheIamStack
{
    public class AndCheckingTheDataIngestionRole : IamStackTestBase
    {
        [Test]
        public void ThenItExposesTheDataIngestionRoleAsAPublicProperty()
        {
            Assert.That(this.IamConstruct.DataIngestionRole, Is.Not.Null);
        }

        [Test]
        public void ThenTheRoleNameIsPokepadDataIngestion()
        {
            Assert.That(this.FindRoleByName("pokepad-data-ingestion"), Is.Not.Null);
        }

        [Test]
        public void ThenItTrustsTheLambdaService()
        {
            var role = this.FindRoleByName("pokepad-data-ingestion");
            Assert.That(this.GetServicePrincipal(role!), Is.EqualTo("lambda.amazonaws.com"));
        }

        [Test]
        public void ThenItAllowsPutObjectOnBronze()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("s3:PutObject"), Is.True);
        }

        [Test]
        public void ThenItAllowsListBucketOnBronze()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("s3:ListBucket"), Is.True);
        }
    }
}
