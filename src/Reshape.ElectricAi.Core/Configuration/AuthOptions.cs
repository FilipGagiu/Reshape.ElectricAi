namespace Reshape.ElectricAi.Core.Configuration;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; init; } = "reshape-electric-ai";
    public string Audience { get; init; } = "reshape-electric-ai-api";
    public string JwtSigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
    public bool SingleSession { get; init; }
}
