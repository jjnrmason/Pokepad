using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingTheGoldBucket : DataLakeStackTestBase
    {
        [Test]
        public void ThenItExposesTheGoldBucketAsAPublicProperty()
        {
            Assert.That(this.DataLakeStack.Gold, Is.Not.Null);
        }

        [Test]
        public void ThenTheGoldBucketHasTheExpectedConstructId()
        {
            Assert.That(this.DataLakeStack.Gold.Node.Id, Is.EqualTo("gold-bucket"));
        }
    }
}
