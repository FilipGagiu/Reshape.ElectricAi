using Reshape.ElectricAi.Core.Dtos.Auth;

namespace Reshape.ElectricAi.Core.Services;

public interface ITokenService
{
    AccessTokenResult IssueAccessToken(TokenSubject subject);

    RefreshTokenResult IssueRefreshToken();

    string HashRefreshToken(string plainToken);
}
