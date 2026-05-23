namespace Reshape.ElectricAi.Core.Configuration;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; init; } = 1536;
}
