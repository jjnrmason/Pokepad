using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingBucketVersioning : DataLakeStackTestBase
    {
        [Test]
        public void ThenAllBucketsHaveVersioningEnabled()
        {
            var buckets = this.Template.FindResources("AWS::S3::Bucket");

            var allVersioned = buckets.Values.All(resource =>
            {
                var props = (IDictionary<string, object>)resource["Properties"];
                var versioning = (IDictionary<string, object>)props["VersioningConfiguration"];
                return versioning["Status"]?.ToString() == "Enabled";
            });

            Assert.That(allVersioned, Is.True);
        }
    }
}
