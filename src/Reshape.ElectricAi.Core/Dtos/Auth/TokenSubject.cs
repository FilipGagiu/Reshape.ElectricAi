using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Auth;

public record TokenSubject(Guid Id, string Email, UserRole Role);
