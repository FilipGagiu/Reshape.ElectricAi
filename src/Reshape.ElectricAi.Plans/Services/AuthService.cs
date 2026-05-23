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
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options) : IAuthService
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<RefreshToken> _refreshTokenRepository = refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenService _tokenService = tokenService;
    private readonly AuthOptions _options = options.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var emailTaken = await _userRepository.AnyAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException("email-in-use", "Email already registered.");
        }

        var hash = _passwordHasher.Hash(request.Password);
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

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);

        if (user is null)
        {
            if (_passwordHasher is PasswordHasher hasher)
            {
                hasher.VerifyDummy();
            }
            else
            {
                _ = _passwordHasher.Verify(request.Password, string.Empty, []);
            }
            throw new UnauthorizedException("invalid-credentials", "Invalid email or password.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedException("invalid-credentials", "Invalid email or password.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var incomingHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var existing = await _refreshTokenRepository.FirstOrDefaultAsync(
            new ActiveRefreshTokenByHashSpec(incomingHash, DateTime.UtcNow),
            cancellationToken);

        if (existing is null || existing.User is null)
        {
            throw new UnauthorizedException("invalid-refresh-token", "Refresh token invalid or expired.");
        }

        var nowUtc = DateTime.UtcNow;
        var refresh = _tokenService.IssueRefreshToken();

        existing.RevokedUtc = nowUtc;
        existing.ReplacedByHash = refresh.TokenHash;
        _refreshTokenRepository.Update(existing);

        var newRow = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = refresh.TokenHash,
            CreatedUtc = nowUtc,
            ExpiresUtc = refresh.ExpiresUtc
        };
        await _refreshTokenRepository.AddAsync(newRow, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var access = _tokenService.IssueAccessToken(new TokenSubject(existing.User.Id, existing.User.Email, existing.User.Role));

        return new AuthResponse(
            access.Token,
            refresh.PlainToken,
            _options.AccessTokenMinutes * 60,
            existing.User.ToUserDto());
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("user-not-found", "User not found.");
        }
        return user.ToUserDto();
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var access = _tokenService.IssueAccessToken(new TokenSubject(user.Id, user.Email, user.Role));
        var refresh = _tokenService.IssueRefreshToken();

        var nowUtc = DateTime.UtcNow;
        var refreshRow = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            CreatedUtc = nowUtc,
            ExpiresUtc = refresh.ExpiresUtc
        };
        await _refreshTokenRepository.AddAsync(refreshRow, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            access.Token,
            refresh.PlainToken,
            _options.AccessTokenMinutes * 60,
            user.ToUserDto());
    }

    private static string NormalizeEmail(string email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();
}
