using NUnit.Framework;
using Pokepad.Gold.Api.Exceptions;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Tests.SemanticSearchEndpoints.WhenWorkingWithTheSemanticSearchEndpoints;

public partial class WhenWorkingWithTheSemanticSearchEndpoints
{
    public class AndPostingAnEmptyQuestion : SemanticSearchEndpointsTestBase
    {
        [Test]
        public void ThenItThrowsAnInputValidationException()
        {
            var request = new SemanticSearchRequest(" ");

            Assert.That(
                async () => await global::Pokepad.Gold.Api.Endpoints.V1.SemanticSearchEndpoints.HandleAsync(request, this.SemanticSearchService, this.OpenAiService),
                Throws.TypeOf<InputValidationException>());
        }
    }
}
