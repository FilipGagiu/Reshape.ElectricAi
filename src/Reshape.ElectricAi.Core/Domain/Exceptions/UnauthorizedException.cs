namespace Reshape.ElectricAi.Core.Domain.Exceptions;

public class UnauthorizedException(string code, string message) : DomainException(code, message);
