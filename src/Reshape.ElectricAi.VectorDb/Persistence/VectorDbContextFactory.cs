using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;

namespace Reshape.ElectricAi.VectorDb.Persistence;

public class VectorDbContextFactory : IDesignTimeDbContextFactory<VectorDbContext>
{
    public VectorDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RESHAPE_VECTOR_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set RESHAPE_VECTOR_CONNECTION to run EF design-time commands.");

        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseNpgsql(connection, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "vector");
                npgsql.UseVector();
            })
            .Options;

        return new VectorDbContext(options, Options.Create(new ChatOptions()));
    }
}
