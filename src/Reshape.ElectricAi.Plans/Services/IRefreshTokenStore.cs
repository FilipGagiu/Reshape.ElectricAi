namespace Reshape.ElectricAi.Plans.Services;

public interface IRefreshTokenStore
{
    Task<RotatedRefreshToken?> ClaimAndRotateAsync(
        string incomingHash,
        string newTokenHash,
        DateTime newTokenExpiresUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
