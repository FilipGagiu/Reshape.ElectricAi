namespace Reshape.ElectricAi.Core.Dtos.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto User);
