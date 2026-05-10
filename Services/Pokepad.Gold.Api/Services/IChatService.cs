namespace Pokepad.Gold.Api.Services;

public interface IChatService
{
    Task<string> CompleteChatAsync(string systemMessage, string userMessage);
}
