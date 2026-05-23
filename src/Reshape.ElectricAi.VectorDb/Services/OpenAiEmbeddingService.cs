using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class OpenAiEmbeddingService(EmbeddingClient client, IOptions<ChatOptions> chatOptions) : IEmbeddingService
{
    private readonly EmbeddingGenerationOptions _options = new() { Dimensions = chatOptions.Value.EmbeddingDimensions };

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await client.GenerateEmbeddingAsync(text, _options, cancellationToken);
        return result.Value.ToFloats();
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var result = await client.GenerateEmbeddingsAsync(texts, _options, cancellationToken);
        return result.Value.Select(e => e.ToFloats()).ToList();
    }
}
