using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Tests.Fakes;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

public sealed class AuthApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "QmpiVmRhTGJZWmNkRlJ3WGV1S2pQa2hRcmRJZ09pTm5BYmNkMDEyMzQ1Njc4OTA";

    private FakeOpenAiClient? _fakeOpenAi;

    public FakeOpenAiClient WithFakeOpenAi()
    {
        _fakeOpenAi = new FakeOpenAiClient();
        return _fakeOpenAi;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", postgres.ConnectionString);
        Environment.SetEnvironmentVariable("Auth__JwtSigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable("Auth__Issuer", "reshape-electric-ai");
        Environment.SetEnvironmentVariable("Auth__Audience", "reshape-electric-ai-api");
        Environment.SetEnvironmentVariable("Auth__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("Auth__RefreshTokenDays", "7");
        // OpenAi env vars are only needed by tests that exercise plan generation.
        // Set them lazily here so they don't leak into other test fixtures' Development-env hosts
        // (LiveFeed/VectorDb seeders that require a real OpenAI key). Cleared in DisposeAsync.
        Environment.SetEnvironmentVariable("OpenAi__ApiKey", "test-key");
        Environment.SetEnvironmentVariable("OpenAi__Limits__TimeoutSeconds", "30");
        Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__PromptCentsPer1K", "0.015");
        Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__CompletionCentsPer1K", "0.060");
        return base.CreateHost(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        // Env vars are process-global. Cleanup here relies on xUnit collection-fixture
        // serialization keeping concurrent factories in the same collection sequenced.
        // Tests in *other* collections (e.g. LiveFeed) must set their own env vars in
        // their own factory's CreateHost to be safe against cross-collection ordering.
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
        builder.ConfigureServices(services =>
        {
            if (_fakeOpenAi is null) return;

            for (var i = services.Count - 1; i >= 0; i--)
            {
                if (services[i].ServiceType == typeof(IOpenAiClient))
                {
                    services.RemoveAt(i);
                }
            }
            services.AddSingleton<IOpenAiClient>(_fakeOpenAi);
        });
    }
}
