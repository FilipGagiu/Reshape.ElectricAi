using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.VectorDb.Persistence;
using Testcontainers.PostgreSql;

namespace Reshape.ElectricAi.VectorDb.Tests;

[CollectionDefinition("VectorDb")]
public sealed class VectorDbCollection : ICollectionFixture<VectorDbFixture>;

public sealed class VectorDbFixture : IAsyncLifetime
{
    public const int TestDimensions = 32;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .Build();

    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        await using var context = CreateContext();
        // NOTE: EnsureCreatedAsync builds the schema from the runtime model — vector(TestDimensions=32).
        // The production migration hardcodes vector(1536). Do NOT switch to MigrateAsync without first
        // aligning the migration to the configured EmbeddingDimensions (CODE.md "Vector DB" section).
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    public VectorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseNpgsql(_connectionString, npgsql => npgsql.UseVector())
            .Options;
        return new VectorDbContext(options, Options.Create(new ChatOptions { EmbeddingDimensions = TestDimensions }));
    }

    public FakeEmbeddingService CreateEmbeddingService() => new(TestDimensions);
}
