namespace Reshape.ElectricAi.Core.Configuration;

public sealed class PushOptions
{
    public const string SectionName = "Push";

    public string VapidPublicKey { get; init; } = string.Empty;
    public string VapidPrivateKey { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
}
