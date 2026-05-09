using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingTheAthenaResultsBucket : DataLakeStackTestBase
    {
        [Test]
        public void ThenItExposesTheAthenaResultsBucketAsAPublicProperty()
        {
            Assert.That(this.DataLakeConstruct.AthenaResults, Is.Not.Null);
        }

        [Test]
        public void ThenTheAthenaResultsBucketHasTheExpectedConstructId()
        {
            Assert.That(this.DataLakeConstruct.AthenaResults.Node.Id, Is.EqualTo("athena-results-bucket"));
        }
    }
}
