namespace Reshape.ElectricAi.Core.Services;

public sealed record RateLimitWindow(
    int MaxRequests,
    TimeSpan WindowSize);
