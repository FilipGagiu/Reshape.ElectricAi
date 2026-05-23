namespace Reshape.ElectricAi.Plans.Configuration;

public sealed class PlanGenerationOptions
{
    public const string SectionName = "Chat:PlanGeneration";

    public string? Model { get; set; }
    public int MaxCompletionTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.7;
    public RateLimitSection RateLimit { get; set; } = new();

    public sealed class RateLimitSection
    {
        public int PerHour { get; set; } = 5;
    }
}
