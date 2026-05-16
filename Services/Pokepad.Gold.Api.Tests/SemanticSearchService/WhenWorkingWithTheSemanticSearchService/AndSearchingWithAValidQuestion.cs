using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Tests.SemanticSearchService.WhenWorkingWithTheSemanticSearchService;

public partial class WhenWorkingWithTheSemanticSearchService
{
    public class AndSearchingWithAValidQuestion : SemanticSearchServiceTestBase
    {
        [Test]
        public async Task ThenItReturnsTheRepositoryResults()
        {
            var question = "Which products are good for camping?";
            var embedding = new[] { 0.1f, 0.2f, 0.3f };
            IReadOnlyList<SemanticSearchResult> expected =
            [
                new SemanticSearchResult("product-1", """{"Name":"Tent"}""", 0.84)
            ];
            this.ModerationService.IsFlaggedAsync(question).Returns(false);
            this.EmbeddingService.EmbedAsync(question).Returns(embedding);
            this.SearchRepository.SearchAsync(embedding, 10).Returns(expected);

            var results = await this.SemanticSearchService.SearchAsync(question);

            Assert.That(results, Is.EqualTo(expected));
        }

        [Test]
        public async Task ThenItUsesTheRequestedLimit()
        {
            var question = "Which products are good for camping?";
            var embedding = new[] { 0.1f, 0.2f, 0.3f };
            IReadOnlyList<SemanticSearchResult> expected = [];
            this.ModerationService.IsFlaggedAsync(question).Returns(false);
            this.EmbeddingService.EmbedAsync(question).Returns(embedding);
            this.SearchRepository.SearchAsync(embedding, 5).Returns(expected);

            var results = await this.SemanticSearchService.SearchAsync(question, 5);

            Assert.That(results, Is.EqualTo(expected));
        }
    }
}
