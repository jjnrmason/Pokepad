using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Tests.SemanticSearchEndpoints.WhenWorkingWithTheSemanticSearchEndpoints;

public partial class WhenWorkingWithTheSemanticSearchEndpoints
{
    public class AndPostingARequestWithSynthesis : SemanticSearchEndpointsTestBase
    {
        [Test]
        public async Task ThenItReturnsTheSynthesisedAnswer()
        {
            var request = new SemanticSearchRequest("Show me hiking boots", 3, true);
            var embedding = new[] { 0.1f, 0.2f, 0.3f };
            IReadOnlyList<SemanticSearchResult> results =
            [
                new SemanticSearchResult("product-1", """{"Name":"Boots"}""", 0.91)
            ];
            this.ModerationService.IsFlaggedAsync(request.Question!).Returns(false);
            this.EmbeddingService.EmbedAsync(request.Question!).Returns(embedding);
            this.SearchRepository.SearchAsync(embedding, request.TopK).Returns(results);
            this.ChatService.CompleteChatAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns("These boots are the closest match.");

            var result = await global::Pokepad.Gold.Api.Endpoints.V1.SemanticSearchEndpoints.HandleAsync(request, this.SemanticSearchService, this.OpenAiService);
            var ok = (Ok<SemanticSearchResponse>)result;

            Assert.That(ok.Value!.Answer, Is.EqualTo("These boots are the closest match."));
        }
    }
}
