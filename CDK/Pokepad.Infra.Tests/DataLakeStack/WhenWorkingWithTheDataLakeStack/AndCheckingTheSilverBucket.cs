using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingTheSilverBucket : DataLakeStackTestBase
    {
        [Test]
        public void ThenItExposesTheSilverBucketAsAPublicProperty()
        {
            Assert.That(this.DataLakeConstruct.Silver, Is.Not.Null);
        }

        [Test]
        public void ThenTheSilverBucketHasTheExpectedConstructId()
        {
            Assert.That(this.DataLakeConstruct.Silver.Node.Id, Is.EqualTo("silver-bucket"));
        }
    }
}
