using OpenAI;
using OpenAI.Embeddings;

namespace Pokepad.Gold.Api.Services;

public sealed class OpenAiEmbeddingService(OpenAIClient client, ILogger<OpenAiEmbeddingService> logger) : IEmbeddingService
{
    private const string Model = "text-embedding-3-small";

    public async Task<float[]> EmbedAsync(string text)
    {
        var embeddingClient = client.GetEmbeddingClient(Model);
        var result = await embeddingClient.GenerateEmbeddingAsync(text);
        var embedding = result.Value.ToFloats().ToArray();

        logger.LogInformation("Generated embedding with {Dimensions} dimensions", embedding.Length);

        return embedding;
    }
}
