using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Services;

public sealed class TokenService(IOptions<AuthOptions> options) : ITokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly AuthOptions _options = options.Value;

    public AccessTokenResult IssueAccessToken(TokenSubject subject)
    {
        var nowUtc = DateTime.UtcNow;
        var expiresUtc = nowUtc.AddMinutes(_options.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, subject.Email),
            new Claim(ClaimTypes.Role, subject.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(nowUtc, TimeSpan.Zero).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(JwtSigningKey.Decode(_options.JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresUtc,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new AccessTokenResult(encoded, expiresUtc);
    }

    public RefreshTokenResult IssueRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenBytes);
        var plain = Base64UrlEncode(bytes);
        var hash = HashRefreshToken(plain);
        var expiresUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
        return new RefreshTokenResult(plain, hash, expiresUtc);
    }

    public string HashRefreshToken(string plainToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainToken);
        var bytes = Encoding.UTF8.GetBytes(plainToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        var s = Convert.ToBase64String(bytes);
        return s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
