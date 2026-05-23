namespace Reshape.ElectricAi.Core.Services;

public interface IRateLimiter
{
    Task AcquireAsync(
        string key,
        RateLimitWindow window,
        CancellationToken cancellationToken);
}
