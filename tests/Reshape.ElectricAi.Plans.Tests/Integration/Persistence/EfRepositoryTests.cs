using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Persistence.Specifications;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class EfRepositoryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private PlansDbContext _context = null!;
    private PlansRepository<User> _users = null!;
    private PlansRepository<RefreshToken> _refreshTokens = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlansDbContext>()
            .UseNpgsql(postgres.ConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plans"))
            .Options;
        _context = new PlansDbContext(options);
        await _context.Database.MigrateAsync();

        _users = new PlansRepository<User>(_context);
        _refreshTokens = new PlansRepository<RefreshToken>(_context);
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FirstOrDefaultAsync_AppliesSpecCriteria()
    {
        var email = UniqueEmail("repo-find");
        await SeedUserAsync(email);

        var found = await _users.FirstOrDefaultAsync(new UserByEmailSpec(email), CancellationToken.None);
        var notFound = await _users.FirstOrDefaultAsync(new UserByEmailSpec(UniqueEmail("repo-missing")), CancellationToken.None);

        found.Should().NotBeNull();
        found!.Email.Should().Be(email);
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task AnyAsync_ReturnsTrueWhenMatched()
    {
        var email = UniqueEmail("repo-any");
        await SeedUserAsync(email);

        var matched = await _users.AnyAsync(new UserByEmailSpec(email), CancellationToken.None);
        var missing = await _users.AnyAsync(new UserByEmailSpec(UniqueEmail("repo-any-missing")), CancellationToken.None);

        matched.Should().BeTrue();
        missing.Should().BeFalse();
    }

    [Fact]
    public async Task ActiveRefreshTokenByHashSpec_LoadsUser_AndFiltersExpiredOrRevoked()
    {
        var email = UniqueEmail("repo-refresh");
        var user = await SeedUserAsync(email);
        var nowUtc = DateTime.UtcNow;

        var active = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = $"hash-active-{Guid.NewGuid():N}", CreatedUtc = nowUtc, ExpiresUtc = nowUtc.AddDays(7) };
        var expired = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = $"hash-expired-{Guid.NewGuid():N}", CreatedUtc = nowUtc.AddDays(-10), ExpiresUtc = nowUtc.AddDays(-1) };
        var revoked = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = $"hash-revoked-{Guid.NewGuid():N}", CreatedUtc = nowUtc, ExpiresUtc = nowUtc.AddDays(7), RevokedUtc = nowUtc };

        await _refreshTokens.AddAsync(active, CancellationToken.None);
        await _refreshTokens.AddAsync(expired, CancellationToken.None);
        await _refreshTokens.AddAsync(revoked, CancellationToken.None);
        await _refreshTokens.SaveChangesAsync(CancellationToken.None);

        var foundActive = await _refreshTokens.FirstOrDefaultAsync(new ActiveRefreshTokenByHashSpec(active.TokenHash, DateTime.UtcNow), CancellationToken.None);
        var foundExpired = await _refreshTokens.FirstOrDefaultAsync(new ActiveRefreshTokenByHashSpec(expired.TokenHash, DateTime.UtcNow), CancellationToken.None);
        var foundRevoked = await _refreshTokens.FirstOrDefaultAsync(new ActiveRefreshTokenByHashSpec(revoked.TokenHash, DateTime.UtcNow), CancellationToken.None);

        foundActive.Should().NotBeNull();
        foundActive!.User.Should().NotBeNull();
        foundActive.User!.Email.Should().Be(email);
        foundExpired.Should().BeNull();
        foundRevoked.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_PersistsEntity()
    {
        var email = UniqueEmail("repo-add");
        var user = await SeedUserAsync(email);

        var stored = await _users.GetByIdAsync(user.Id, CancellationToken.None);

        stored.Should().NotBeNull();
        stored!.Email.Should().Be(email);
    }

    private async Task<User> SeedUserAsync(string email)
    {
        var nowUtc = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "$2a$12$abcdefghijklmnopqrstuv",
            PasswordSalt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
            Role = UserRole.User,
            CreatedUtc = nowUtc,
            UpdatedUtc = nowUtc
        };
        await _users.AddAsync(user, CancellationToken.None);
        await _users.SaveChangesAsync(CancellationToken.None);
        return user;
    }

    private static string UniqueEmail(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@example.com";
}
