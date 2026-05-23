namespace Reshape.ElectricAi.Core.Domain.Exceptions;

public class ConflictException(string code, string message) : DomainException(code, message);
