using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingBucketPublicAccess : DataLakeStackTestBase
    {
        private IEnumerable<IDictionary<string, object>> GetPublicAccessConfigs()
        {
            return this.Template.FindResources("AWS::S3::Bucket").Values.Select(resource =>
            {
                var props = (IDictionary<string, object>)resource["Properties"];
                return (IDictionary<string, object>)props["PublicAccessBlockConfiguration"];
            });
        }

        [Test]
        public void ThenAllBucketsBlockPublicAcls()
        {
            Assert.That(
                this.GetPublicAccessConfigs().All(config => config["BlockPublicAcls"] is true),
                Is.True);
        }

        [Test]
        public void ThenAllBucketsBlockPublicPolicy()
        {
            Assert.That(
                this.GetPublicAccessConfigs().All(config => config["BlockPublicPolicy"] is true),
                Is.True);
        }

        [Test]
        public void ThenAllBucketsIgnorePublicAcls()
        {
            Assert.That(
                this.GetPublicAccessConfigs().All(config => config["IgnorePublicAcls"] is true),
                Is.True);
        }

        [Test]
        public void ThenAllBucketsRestrictPublicBuckets()
        {
            Assert.That(
                this.GetPublicAccessConfigs().All(config => config["RestrictPublicBuckets"] is true),
                Is.True);
        }
    }
}
