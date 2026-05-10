using NSubstitute;
using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.OpenAiService.WhenWorkingWithTheOpenAiService;

public partial class WhenWorkingWithTheOpenAiService
{
    public class AndGeneratingSqlFromAValidQuestion : OpenAiServiceTestBase
    {
        private string _sql = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.ModerationService.IsFlaggedAsync("What are the top products?").Returns(false);
            this.ChatService.CompleteChatAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns("SELECT product_id FROM products LIMIT 10");

            _sql = await this.OpenAiService.GenerateSqlAsync("What are the top products?", "Table: products");
        }

        [Test]
        public void ThenItReturnsTheSqlFromTheChatService()
        {
            Assert.That(_sql, Is.EqualTo("SELECT product_id FROM products LIMIT 10"));
        }
    }
}
