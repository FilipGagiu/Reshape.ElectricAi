namespace Reshape.ElectricAi.AiChat.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; set; } = string.Empty;
    public LimitsSection Limits { get; set; } = new();
    public Dictionary<string, ModelPricing> Models { get; set; } = new();

    public sealed class LimitsSection
    {
        public int MaxPromptTokens { get; set; } = 8000;
        public int MaxCompletionTokens { get; set; } = 1024;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public sealed class ModelPricing
    {
        public decimal PromptCentsPer1K { get; set; }
        public decimal CompletionCentsPer1K { get; set; }
    }
}
