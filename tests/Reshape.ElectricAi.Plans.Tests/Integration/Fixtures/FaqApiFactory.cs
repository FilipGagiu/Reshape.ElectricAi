using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

public sealed class FaqApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Auth__JwtSigningKey", AuthApiFactory.TestSigningKey);
        Environment.SetEnvironmentVariable("Auth__Issuer", "reshape-electric-ai");
        Environment.SetEnvironmentVariable("Auth__Audience", "reshape-electric-ai-api");
        Environment.SetEnvironmentVariable("Auth__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Auth__RefreshTokenDays", "7");
        Environment.SetEnvironmentVariable("OpenAi__ApiKey", "dummy-key-not-used-in-tests");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmbeddingService));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddScoped<IEmbeddingService, FaqFakeEmbeddingService>();
        });
    }
}
