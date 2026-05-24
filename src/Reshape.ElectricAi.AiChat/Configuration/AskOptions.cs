namespace Reshape.ElectricAi.AiChat.Configuration;

public sealed class AskOptions
{
    public const string SectionName = "Ask";

    public string Model { get; set; } = "gpt-5-mini";
    public int MaxCompletionTokens { get; set; } = 512;
    public float ScoreThreshold { get; set; } = 0.4f;
    public int TopKPerSource { get; set; } = 4;
    public int TopKFinal { get; set; } = 6;
}
