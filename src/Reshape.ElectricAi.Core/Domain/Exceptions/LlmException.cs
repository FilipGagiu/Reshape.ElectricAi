namespace Reshape.ElectricAi.Core.Domain.Exceptions;

public class LlmException(string code, string message, Exception? innerException = null)
    : DomainException(code, message, innerException);
