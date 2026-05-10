using OpenAI;

namespace Pokepad.Gold.Api.Services;

public sealed class OpenAiModerationService(OpenAIClient client, ILogger<OpenAiModerationService> logger) : IModerationService
{
    private const string Model = "omni-moderation-latest";

    public async Task<bool> IsFlaggedAsync(string text)
    {
        var moderationClient = client.GetModerationClient(Model);
        var result = await moderationClient.ClassifyTextAsync(text);

        if (result.Value.Flagged)
        {
            logger.LogWarning("Moderation flagged input");
        }

        return result.Value.Flagged;
    }
}
