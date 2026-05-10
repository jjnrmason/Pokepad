using OpenAI;
using OpenAI.Chat;

namespace Pokepad.Gold.Api.Services;

public sealed class OpenAiChatService(OpenAIClient client, ILogger<OpenAiChatService> logger) : IChatService
{
    private const string Model = "gpt-4o";

    public async Task<string> CompleteChatAsync(string systemMessage, string userMessage)
    {
        var chatClient = client.GetChatClient(Model);

        var response = await chatClient.CompleteChatAsync(
            [
                ChatMessage.CreateSystemMessage(systemMessage),
                ChatMessage.CreateUserMessage(userMessage)
            ],
            new ChatCompletionOptions { MaxOutputTokenCount = 1024 });

        var text = response.Value.Content[0].Text.Trim();

        logger.LogInformation("Chat completed ({InputTokens} in / {OutputTokens} out)",
            response.Value.Usage.InputTokenCount, response.Value.Usage.OutputTokenCount);

        return text;
    }
}
