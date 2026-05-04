using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingTheBucketCount : DataLakeStackTestBase
    {
        [Test]
        public void ThenItCreatesFourS3Buckets()
        {
            Assert.That(this.Template.FindResources("AWS::S3::Bucket"), Has.Count.EqualTo(4));
        }

        [Test]
        public void ThenItCreatesFourBucketPolicies()
        {
            Assert.That(this.Template.FindResources("AWS::S3::BucketPolicy"), Has.Count.EqualTo(4));
        }
    }
}
