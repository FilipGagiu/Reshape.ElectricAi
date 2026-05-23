using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

public sealed class AuthApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "QmpiVmRhTGJZWmNkRlJ3WGV1S2pQa2hRcmRJZ09pTm5BYmNkMDEyMzQ1Njc4OTA";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", postgres.ConnectionString);
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
    }
}
