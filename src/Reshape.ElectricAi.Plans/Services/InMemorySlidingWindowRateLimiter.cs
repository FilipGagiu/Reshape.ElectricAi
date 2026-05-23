using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Services;

public sealed partial class InMemorySlidingWindowRateLimiter(
    ILogger<InMemorySlidingWindowRateLimiter> logger) : IRateLimiter
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _hits = new();

    public Task AcquireAsync(string key, RateLimitWindow window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - window.WindowSize;

        var queue = _hits.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() < cutoff)
            {
                queue.Dequeue();
            }

            if (queue.Count >= window.MaxRequests)
            {
                var oldest = queue.Peek();
                var resetAt = oldest + window.WindowSize;
                var retryAfter = Math.Max(1, (int)Math.Ceiling((resetAt - now).TotalSeconds));
                LogRateLimited(logger, key, resetAt);
                throw new TooManyRequestsException(retryAfter);
            }

            queue.Enqueue(now);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Warning,
        Message = "Rate limit hit for key={Key}, resetAt={ResetAt}")]
    private static partial void LogRateLimited(ILogger logger, string key, DateTimeOffset resetAt);
}
