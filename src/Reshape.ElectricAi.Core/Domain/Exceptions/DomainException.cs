namespace Reshape.ElectricAi.Core.Domain.Exceptions;

public abstract class DomainException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}
