namespace Reshape.ElectricAi.Core.Domain.Exceptions;

public sealed class TooManyRequestsException(int retryAfterSeconds, string message = "Rate limit exceeded.")
    : DomainException("rate-limit-exceeded", message)
{
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
