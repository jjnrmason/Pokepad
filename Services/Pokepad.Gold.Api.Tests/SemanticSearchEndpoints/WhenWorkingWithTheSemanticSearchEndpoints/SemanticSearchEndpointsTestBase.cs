using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Tests.SemanticSearchEndpoints.WhenWorkingWithTheSemanticSearchEndpoints;

public class SemanticSearchEndpointsTestBase
{
    protected IModerationService ModerationService { get; private set; } = null!;
    protected IEmbeddingService EmbeddingService { get; private set; } = null!;
    protected ISemanticSearchRepository SearchRepository { get; private set; } = null!;
    protected IChatService ChatService { get; private set; } = null!;
    protected Services.SemanticSearchService SemanticSearchService { get; private set; } = null!;
    protected Services.OpenAiService OpenAiService { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        this.ModerationService = Substitute.For<IModerationService>();
        this.EmbeddingService = Substitute.For<IEmbeddingService>();
        this.SearchRepository = Substitute.For<ISemanticSearchRepository>();
        this.ChatService = Substitute.For<IChatService>();
        this.SemanticSearchService = new Services.SemanticSearchService(
            this.ModerationService,
            this.EmbeddingService,
            this.SearchRepository,
            Substitute.For<ILogger<Services.SemanticSearchService>>());
        this.OpenAiService = new Services.OpenAiService(
            this.ChatService,
            this.ModerationService,
            Substitute.For<ILogger<Services.OpenAiService>>());
    }
}
