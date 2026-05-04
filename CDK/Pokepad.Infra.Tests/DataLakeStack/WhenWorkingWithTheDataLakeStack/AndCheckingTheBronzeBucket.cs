using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingTheBronzeBucket : DataLakeStackTestBase
    {
        [Test]
        public void ThenItExposesTheBronzeBucketAsAPublicProperty()
        {
            Assert.That(this.DataLakeStack.Bronze, Is.Not.Null);
        }

        [Test]
        public void ThenTheBronzeBucketHasTheExpectedConstructId()
        {
            Assert.That(this.DataLakeStack.Bronze.Node.Id, Is.EqualTo("bronze-bucket"));
        }
    }
}
