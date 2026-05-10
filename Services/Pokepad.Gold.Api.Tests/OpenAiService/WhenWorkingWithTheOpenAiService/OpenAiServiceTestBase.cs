using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Tests.OpenAiService.WhenWorkingWithTheOpenAiService;

public class OpenAiServiceTestBase
{
    protected IChatService ChatService { get; private set; } = null!;
    protected IModerationService ModerationService { get; private set; } = null!;
    protected Services.OpenAiService OpenAiService { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.ChatService = Substitute.For<IChatService>();
        this.ModerationService = Substitute.For<IModerationService>();
        this.OpenAiService = new Services.OpenAiService(
            this.ChatService,
            this.ModerationService,
            Substitute.For<ILogger<Services.OpenAiService>>());
    }

    [SetUp]
    public virtual void SetUp()
    {
        this.ChatService.ClearReceivedCalls();
        this.ModerationService.ClearReceivedCalls();
    }
}
