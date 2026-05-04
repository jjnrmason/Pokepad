using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingBucketEncryption : DataLakeStackTestBase
    {
        [Test]
        public void ThenAllBucketsUseS3ManagedEncryption()
        {
            var buckets = this.Template.FindResources("AWS::S3::Bucket");

            var allEncrypted = buckets.Values.All(resource =>
            {
                var props = (IDictionary<string, object>)resource["Properties"];
                var encryption = (IDictionary<string, object>)props["BucketEncryption"];
                var rules = (IList<object>)encryption["ServerSideEncryptionConfiguration"];
                var rule = (IDictionary<string, object>)rules[0];
                var sse = (IDictionary<string, object>)rule["ServerSideEncryptionByDefault"];
                return sse["SSEAlgorithm"]?.ToString() == "AES256";
            });

            Assert.That(allEncrypted, Is.True);
        }
    }
}
