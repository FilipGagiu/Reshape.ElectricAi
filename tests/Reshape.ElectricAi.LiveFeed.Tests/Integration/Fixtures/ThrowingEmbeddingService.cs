using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class ThrowingEmbeddingService : IEmbeddingService
{
    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("simulated embed failure");

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("simulated embed failure");
}
