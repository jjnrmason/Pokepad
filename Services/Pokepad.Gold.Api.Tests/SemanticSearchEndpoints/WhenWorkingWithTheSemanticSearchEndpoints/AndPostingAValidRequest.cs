using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Tests.SemanticSearchEndpoints.WhenWorkingWithTheSemanticSearchEndpoints;

public partial class WhenWorkingWithTheSemanticSearchEndpoints
{
    public class AndPostingAValidRequest : SemanticSearchEndpointsTestBase
    {
        [Test]
        public async Task ThenItReturnsTheSemanticSearchResults()
        {
            var request = new SemanticSearchRequest("Show me hiking boots", 3);
            var embedding = new[] { 0.1f, 0.2f, 0.3f };
            IReadOnlyList<SemanticSearchResult> expected =
            [
                new SemanticSearchResult("product-1", """{"Name":"Boots"}""", 0.91)
            ];
            this.ModerationService.IsFlaggedAsync(request.Question!).Returns(false);
            this.EmbeddingService.EmbedAsync(request.Question!).Returns(embedding);
            this.SearchRepository.SearchAsync(embedding, request.TopK).Returns(expected);

            var result = await global::Pokepad.Gold.Api.Endpoints.V1.SemanticSearchEndpoints.HandleAsync(request, this.SemanticSearchService, this.OpenAiService);
            var ok = (Ok<SemanticSearchResponse>)result;

            Assert.That(ok.Value!.Results, Is.EqualTo(expected));
        }
    }
}
