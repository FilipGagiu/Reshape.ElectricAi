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
        // AiChatModule + VectorDbModule fail-fast on missing OpenAi:ApiKey at startup.
        // LiveFeed tests don't exercise LLM calls but the host still loads both modules.
        Environment.SetEnvironmentVariable("OpenAi__ApiKey", "test-key");
        Environment.SetEnvironmentVariable("OpenAi__Limits__TimeoutSeconds", "30");
        Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__PromptCentsPer1K", "0.015");
        Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__CompletionCentsPer1K", "0.060");
        return base.CreateHost(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable("OpenAi__ApiKey", null);
        Environment.SetEnvironmentVariable("OpenAi__Limits__TimeoutSeconds", null);
        Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__PromptCentsPer1K", null);
        Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__CompletionCentsPer1K", null);
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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
        // TRUNCATE the feed schema tables. Avoids DROP DATABASE, which terminates
        // Npgsql's static process-global pool connections and produces a 57P01 race
        // on the next host's startup migration query. Migrations are applied by
        // Program.cs at host startup (Development|Testing env gate), so the schema
        // is already in place by the time this method runs.
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE feed.feed_entries, feed.feed_entry_artists, feed.feed_entry_genres RESTART IDENTITY CASCADE;");
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
