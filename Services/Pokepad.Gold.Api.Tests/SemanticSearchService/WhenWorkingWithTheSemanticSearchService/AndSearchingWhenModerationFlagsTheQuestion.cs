using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Exceptions;

namespace Pokepad.Gold.Api.Tests.SemanticSearchService.WhenWorkingWithTheSemanticSearchService;

public partial class WhenWorkingWithTheSemanticSearchService
{
    public class AndSearchingWhenModerationFlagsTheQuestion : SemanticSearchServiceTestBase
    {
        [Test]
        public void ThenItThrowsAnInputValidationException()
        {
            var question = "harmful product query";
            this.ModerationService.IsFlaggedAsync(question).Returns(true);

            Assert.That(
                async () => await this.SemanticSearchService.SearchAsync(question),
                Throws.TypeOf<InputValidationException>());
        }
    }
}
