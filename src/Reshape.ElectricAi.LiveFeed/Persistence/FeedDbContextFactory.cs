using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reshape.ElectricAi.LiveFeed.Persistence;

public class FeedDbContextFactory : IDesignTimeDbContextFactory<FeedDbContext>
{
    public FeedDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RESHAPE_FEED_CONNECTION")
            ?? "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<FeedDbContext>()
            .UseNpgsql(connection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "feed"))
            .Options;

        return new FeedDbContext(options);
    }
}
