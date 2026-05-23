using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class FeedApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "QmpiVmRhTGJZWmNkRlJ3WGV1S2pQa2hRcmRJZ09pTm5BYmNkMDEyMzQ1Njc4OTA";

    private readonly PostgresFixture _postgres = postgres;

    public FakeUserPrefsProvider FakePrefs { get; } = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Auth__JwtSigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable("Auth__Issuer", "reshape-electric-ai");
        Environment.SetEnvironmentVariable("Auth__Audience", "reshape-electric-ai-api");
        Environment.SetEnvironmentVariable("Auth__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Auth__RefreshTokenDays", "7");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(IUserPrefsProvider));
            services.Remove(descriptor);
            services.AddScoped<IUserPrefsProvider>(_ => FakePrefs);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public HttpClient CreateClientForUser(Guid userId, UserRole role = UserRole.User, string email = "tester@example.com")
    {
        var client = CreateClient();
        using var scope = Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var token = tokenService.IssueAccessToken(new TokenSubject(userId, email, role));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }

    public HttpClient CreateAnonymousClient() => CreateClient();
}
