using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services;

public sealed class TokenServiceTests
{
    private const string SigningKey = "QmpiVmRhTGJZWmNkRlJ3WGV1S2pQa2hRcmRJZ09pTm5BYmNkMDEyMzQ1Njc4OTA";

    private readonly TokenService _tokenService;
    private readonly AuthOptions _options;

    public TokenServiceTests()
    {
        _options = new AuthOptions
        {
            Issuer = "reshape-electric-ai",
            Audience = "reshape-electric-ai-api",
            JwtSigningKey = SigningKey,
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };
        _tokenService = new TokenService(Options.Create(_options));
    }

    [Fact]
    public void IssueAccessToken_EncodesExpectedClaims()
    {
        var subject = new TokenSubject(Guid.NewGuid(), "alice@example.com", UserRole.User);

        var result = _tokenService.IssueAccessToken(subject);
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(JwtSigningKey.Decode(SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(result.Token, validation, out var validated);

        principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(subject.Id.ToString());
        principal.FindFirstValue(ClaimTypes.Role).Should().Be(subject.Role.ToString());
        validated.Issuer.Should().Be(_options.Issuer);
        result.ExpiresUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IssueRefreshToken_ProducesPlainHashAndExpiry()
    {
        var first = _tokenService.IssueRefreshToken();
        var second = _tokenService.IssueRefreshToken();

        first.PlainToken.Should().NotBeNullOrEmpty();
        first.TokenHash.Should().NotBeNullOrEmpty();
        first.PlainToken.Should().NotBe(second.PlainToken);
        first.TokenHash.Should().NotBe(second.TokenHash);
        first.ExpiresUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(_options.RefreshTokenDays), TimeSpan.FromSeconds(5));
        _tokenService.HashRefreshToken(first.PlainToken).Should().Be(first.TokenHash);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic()
    {
        const string Plain = "fixed-input-token";

        var hashA = _tokenService.HashRefreshToken(Plain);
        var hashB = _tokenService.HashRefreshToken(Plain);

        hashA.Should().Be(hashB);
    }
}
