using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reshape.ElectricAi.Plans.Persistence;

public class PlansDbContextFactory : IDesignTimeDbContextFactory<PlansDbContext>
{
    public PlansDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RESHAPE_PLANS_CONNECTION")
            ?? "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<PlansDbContext>()
            .UseNpgsql(connection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plans"))
            .Options;

        return new PlansDbContext(options);
    }
}
