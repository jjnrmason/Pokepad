namespace Pokepad.Gold.Api.Services;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
}
