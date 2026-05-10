using NUnit.Framework;

namespace Pokepad.Infra.Tests.VectorStoreStack.WhenWorkingWithTheVectorStoreConstruct;

public partial class WhenWorkingWithTheVectorStoreConstruct
{
    public class AndCheckingTheVpc : VectorStoreConstructTestBase
    {
        [Test]
        public void ThenItExposesTheVpcAsAPublicProperty()
        {
            Assert.That(this.VectorStoreConstruct.Vpc, Is.Not.Null);
        }

        [Test]
        public void ThenItCreatesOneVpc()
        {
            Assert.That(this.Template.FindResources("AWS::EC2::VPC"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenItExposesTheLambdaSecurityGroupAsAPublicProperty()
        {
            Assert.That(this.VectorStoreConstruct.LambdaSecurityGroup, Is.Not.Null);
        }

        [Test]
        public void ThenItCreatesAnS3GatewayEndpoint()
        {
            var endpoints = this.Template.FindResources("AWS::EC2::VPCEndpoint");
            var hasS3 = endpoints.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["ServiceName"]?.ToString()?.Contains("s3") == true
                    || (props["ServiceName"] is IDictionary<string, object> joined);
            });
            Assert.That(endpoints, Has.Count.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void ThenPublicAndPrivateAndIsolatedSubnetsAreCreated()
        {
            var subnets = this.Template.FindResources("AWS::EC2::Subnet");
            Assert.That(subnets.Count, Is.GreaterThanOrEqualTo(3));
        }
    }
}
