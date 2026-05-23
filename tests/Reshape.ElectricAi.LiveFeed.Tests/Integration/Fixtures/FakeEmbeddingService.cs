using System.Security.Cryptography;
using System.Text;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class FakeEmbeddingService(int dimensions) : IEmbeddingService
{
    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GenerateVector(text));

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(
            texts.Select(GenerateVector).ToList());

    private ReadOnlyMemory<float> GenerateVector(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var seed = BitConverter.ToInt32(hash, 0);
        var rng = new Random(seed);
        var floats = new float[dimensions];

        for (var i = 0; i < dimensions; i++)
            floats[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

        var magnitude = MathF.Sqrt(floats.Sum(f => f * f));
        for (var i = 0; i < dimensions; i++)
            floats[i] /= magnitude;

        return floats;
    }
}
