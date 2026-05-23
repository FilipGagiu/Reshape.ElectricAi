using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Extensions;
using Reshape.ElectricAi.Plans.Persistence.Specifications;

namespace Reshape.ElectricAi.Plans.Services;

public sealed class AuthService(
    IRepository<User> userRepository,
    IRepository<RefreshToken> refreshTokenRepository,
    IRefreshTokenStore refreshTokenStore,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options) : IAuthService
{
    private readonly AuthOptions _options = options.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var emailTaken = await userRepository.AnyAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException("email-in-use", "Email already registered.");
        }

        var hash = passwordHasher.Hash(request.Password);
        var nowUtc = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = hash.Hash,
            PasswordSalt = hash.Salt,
            Role = UserRole.User,
            CreatedUtc = nowUtc,
            UpdatedUtc = nowUtc
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);

        if (user is null)
        {
            passwordHasher.VerifyDummy();
            throw new UnauthorizedException("invalid-credentials", "Invalid email or password.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedException("invalid-credentials", "Invalid email or password.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var incomingHash = tokenService.HashRefreshToken(request.RefreshToken);
        var nowUtc = DateTime.UtcNow;
        var refresh = tokenService.IssueRefreshToken();

        var rotated = await refreshTokenStore.ClaimAndRotateAsync(
            incomingHash,
            refresh.TokenHash,
            refresh.ExpiresUtc,
            nowUtc,
            cancellationToken);

        if (rotated is null)
        {
            throw new UnauthorizedException("invalid-refresh-token", "Refresh token invalid or expired.");
        }

        var access = tokenService.IssueAccessToken(new TokenSubject(rotated.UserId, rotated.Email, rotated.Role));

        return new AuthResponse(
            access.Token,
            refresh.PlainToken,
            _options.AccessTokenMinutes * 60,
            new UserDto(rotated.UserId, rotated.Email, rotated.Role));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("user-not-found", "User not found.");
        }
        return user.ToUserDto();
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var access = tokenService.IssueAccessToken(new TokenSubject(user.Id, user.Email, user.Role));
        var refresh = tokenService.IssueRefreshToken();

        var nowUtc = DateTime.UtcNow;
        var refreshRow = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            CreatedUtc = nowUtc,
            ExpiresUtc = refresh.ExpiresUtc
        };
        await refreshTokenRepository.AddAsync(refreshRow, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            access.Token,
            refresh.PlainToken,
            _options.AccessTokenMinutes * 60,
            user.ToUserDto());
    }

    private static string NormalizeEmail(string email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();
}
