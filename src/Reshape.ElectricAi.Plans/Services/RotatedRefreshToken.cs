using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Services;

public sealed record RotatedRefreshToken(Guid UserId, string Email, UserRole Role);
