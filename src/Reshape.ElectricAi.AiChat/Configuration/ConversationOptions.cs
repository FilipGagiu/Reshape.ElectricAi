namespace Reshape.ElectricAi.AiChat.Configuration;

public sealed class ConversationOptions
{
    public const string SectionName = "Conversation";

    public string Model { get; set; } = "gpt-5-mini";
    public int MaxCompletionTokens { get; set; } = 1024;
    public float ScoreThreshold { get; set; } = 0.4f;
    public int TopKPerSource { get; set; } = 4;
    public int TopKFinal { get; set; } = 6;
    public int UserMessageCap { get; set; } = 20;
    public int MaxMessageChars { get; set; } = 1000;
    public int TitleMaxChars { get; set; } = 60;
    public int LockTimeoutSeconds { get; set; } = 60;
}
