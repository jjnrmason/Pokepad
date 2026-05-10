namespace Pokepad.Gold.Api.Services;

public interface IModerationService
{
    Task<bool> IsFlaggedAsync(string text);
}
