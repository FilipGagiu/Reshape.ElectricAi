namespace Reshape.ElectricAi.Core.Services;

public record RefreshTokenResult(string PlainToken, string TokenHash, DateTime ExpiresUtc);
