namespace Reshape.ElectricAi.Core.Domain.Exceptions;

public class PreconditionFailedException(string code, string message) : DomainException(code, message);
