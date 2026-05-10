using NUnit.Framework;

namespace Pokepad.Infra.Tests.VectorStoreStack.WhenWorkingWithTheVectorStoreConstruct;

public partial class WhenWorkingWithTheVectorStoreConstruct
{
    public class AndCheckingTheSecurityGroups : VectorStoreConstructTestBase
    {
        [Test]
        public void ThenTheLambdaSecurityGroupIsCreated()
        {
            Assert.That(this.FindSecurityGroupByName("pokepad-lambda-sg"), Is.Not.Null);
        }

        [Test]
        public void ThenTheLambdaSecurityGroupAllowsAllOutbound()
        {
            var sg = this.FindSecurityGroupByName("pokepad-lambda-sg")!;
            var props = (IDictionary<string, object>)sg["Properties"];
            Assert.That(props["SecurityGroupEgress"], Is.Not.Null);
        }

        [Test]
        public void ThenTheRdsSecurityGroupIsCreated()
        {
            Assert.That(this.FindSecurityGroupByName("pokepad-rds-sg"), Is.Not.Null);
        }

        [Test]
        public void ThenTheBastionSecurityGroupIsCreated()
        {
            Assert.That(this.FindSecurityGroupByName("pokepad-bastion-sg"), Is.Not.Null);
        }

        [Test]
        public void ThenTheBastionInstanceIsCreated()
        {
            var instances = this.Template.FindResources("AWS::EC2::Instance");
            var hasBastion = instances.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props.TryGetValue("Tags", out var tags) &&
                    tags is IList<object> tagList &&
                    tagList.Cast<IDictionary<string, object>>()
                        .Any(t => t["Key"]?.ToString() == "Name" && t["Value"]?.ToString() == "pokepad-bastion");
            });
            Assert.That(hasBastion, Is.True);
        }

        [Test]
        public void ThenTheBastionOutputIsCreated()
        {
            var outputs = this.Template.FindOutputs("*");
            Assert.That(outputs.Keys.Any(k => k.Contains("BastionInstanceId")), Is.True);
        }
    }
}
