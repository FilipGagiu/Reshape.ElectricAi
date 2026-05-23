namespace Reshape.ElectricAi.AiChat.Configuration;

public sealed class ConversationOptions
{
    public const string SectionName = "Conversation";

    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxCompletionTokens { get; set; } = 512;
    public float ScoreThreshold { get; set; } = 0.6f;
    public int TopKPerSource { get; set; } = 3;
    public int TopKFinal { get; set; } = 3;
}
