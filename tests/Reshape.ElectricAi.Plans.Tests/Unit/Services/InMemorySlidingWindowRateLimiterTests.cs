using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Services;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services;

public sealed class InMemorySlidingWindowRateLimiterTests
{
    private static readonly RateLimitWindow Five = new(MaxRequests: 5, WindowSize: TimeSpan.FromHours(1));

    [Fact]
    public async Task AcquireAsync_UnderLimit_DoesNotThrow()
    {
        var limiter = new InMemorySlidingWindowRateLimiter(NullLogger<InMemorySlidingWindowRateLimiter>.Instance);

        for (var i = 0; i < 5; i++)
        {
            await limiter.AcquireAsync("user-a", Five, CancellationToken.None);
        }
    }

    [Fact]
    public async Task AcquireAsync_ExceedingLimit_ThrowsWithRetryAfter()
    {
        var limiter = new InMemorySlidingWindowRateLimiter(NullLogger<InMemorySlidingWindowRateLimiter>.Instance);
        for (var i = 0; i < 5; i++)
        {
            await limiter.AcquireAsync("user-b", Five, CancellationToken.None);
        }

        var act = () => limiter.AcquireAsync("user-b", Five, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<TooManyRequestsException>();
        ex.Which.RetryAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AcquireAsync_DifferentKeys_AreIndependent()
    {
        var limiter = new InMemorySlidingWindowRateLimiter(NullLogger<InMemorySlidingWindowRateLimiter>.Instance);
        for (var i = 0; i < 5; i++)
        {
            await limiter.AcquireAsync("user-c", Five, CancellationToken.None);
        }

        await limiter.AcquireAsync("user-d", Five, CancellationToken.None);
    }
}
