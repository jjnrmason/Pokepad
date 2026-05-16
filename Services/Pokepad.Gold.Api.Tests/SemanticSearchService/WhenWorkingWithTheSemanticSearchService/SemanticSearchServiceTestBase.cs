using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Tests.SemanticSearchService.WhenWorkingWithTheSemanticSearchService;

public class SemanticSearchServiceTestBase
{
    protected IModerationService ModerationService { get; private set; } = null!;
    protected IEmbeddingService EmbeddingService { get; private set; } = null!;
    protected ISemanticSearchRepository SearchRepository { get; private set; } = null!;
    protected Services.SemanticSearchService SemanticSearchService { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        this.ModerationService = Substitute.For<IModerationService>();
        this.EmbeddingService = Substitute.For<IEmbeddingService>();
        this.SearchRepository = Substitute.For<ISemanticSearchRepository>();
        this.SemanticSearchService = new Services.SemanticSearchService(
            this.ModerationService,
            this.EmbeddingService,
            this.SearchRepository,
            Substitute.For<ILogger<Services.SemanticSearchService>>());
    }
}
