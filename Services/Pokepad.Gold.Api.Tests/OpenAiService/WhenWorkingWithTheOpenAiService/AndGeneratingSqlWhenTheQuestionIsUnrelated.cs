using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Exceptions;

namespace Pokepad.Gold.Api.Tests.OpenAiService.WhenWorkingWithTheOpenAiService;

public partial class WhenWorkingWithTheOpenAiService
{
    public class AndGeneratingSqlWhenTheQuestionIsUnrelated : OpenAiServiceTestBase
    {
        [OneTimeSetUp]
        public void SetUpScenario()
        {
            this.ModerationService.IsFlaggedAsync("What is the weather today?").Returns(false);
            this.ChatService.CompleteChatAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns("INVALID_QUERY");
        }

        [Test]
        public void ThenItThrowsAnInputValidationException()
        {
            Assert.That(
                async () => await this.OpenAiService.GenerateSqlAsync("What is the weather today?", "Table: products"),
                Throws.TypeOf<InputValidationException>());
        }
    }
}
