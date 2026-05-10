using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Exceptions;

namespace Pokepad.Gold.Api.Tests.OpenAiService.WhenWorkingWithTheOpenAiService;

public partial class WhenWorkingWithTheOpenAiService
{
    public class AndGeneratingSqlWhenModerationFlagsTheInput : OpenAiServiceTestBase
    {
        [OneTimeSetUp]
        public void SetUpScenario()
        {
            this.ModerationService.IsFlaggedAsync("harmful content").Returns(true);
        }

        [Test]
        public void ThenItThrowsAnInputValidationException()
        {
            Assert.That(
                async () => await this.OpenAiService.GenerateSqlAsync("harmful content", "Table: products"),
                Throws.TypeOf<InputValidationException>());
        }
    }
}
