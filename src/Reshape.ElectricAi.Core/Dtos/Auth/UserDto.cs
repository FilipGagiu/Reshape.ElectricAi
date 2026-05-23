using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Auth;

public record UserDto(Guid Id, string Email, UserRole Role);
