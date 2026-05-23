namespace Reshape.ElectricAi.Plans.Configuration;

public sealed class ItineraryGenerationOptions
{
    public const string SectionName = "ItineraryGeneration";

    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxCompletionTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.2;
    public RateLimitOptions RateLimit { get; set; } = new() { PerHour = 10 };
    public RateLimitOptions PrefsRateLimit { get; set; } = new() { PerHour = 30 };

    public sealed class RateLimitOptions
    {
        public int PerHour { get; set; }
    }
}
