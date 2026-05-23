using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

public sealed class AuthApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "QmpiVmRhTGJZWmNkRlJ3WGV1S2pQa2hRcmRJZ09pTm5BYmNkMDEyMzQ1Njc4OTA";

    private readonly PostgresFixture _postgres = postgres;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.ConnectionString,
                ["Auth:JwtSigningKey"] = TestSigningKey,
                ["Auth:Issuer"] = "reshape-electric-ai",
                ["Auth:Audience"] = "reshape-electric-ai-api",
                ["Auth:AccessTokenMinutes"] = "15",
                ["Auth:RefreshTokenDays"] = "7"
            });
        });
    }
}
