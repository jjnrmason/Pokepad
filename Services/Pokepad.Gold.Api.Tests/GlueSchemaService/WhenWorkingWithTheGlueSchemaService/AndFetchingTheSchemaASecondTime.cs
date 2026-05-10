using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.GlueSchemaService.WhenWorkingWithTheGlueSchemaService;

public partial class WhenWorkingWithTheGlueSchemaService
{
    public class AndFetchingTheSchemaASecondTime : GlueSchemaServiceTestBase
    {
        private string _firstResult = null!;
        private string _secondResult = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            _firstResult = await this.GlueSchemaService.GetSchemaAsync();
            _secondResult = await this.GlueSchemaService.GetSchemaAsync();
        }

        [Test]
        public void ThenItReturnsTheSameResultAsTheFirstCall()
        {
            Assert.That(_secondResult, Is.EqualTo(_firstResult));
        }
    }
}
