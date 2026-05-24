using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reshape.ElectricAi.AiChat.Persistence;

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RESHAPE_CHAT_CONNECTION")
            ?? "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseNpgsql(connection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "chat"))
            .Options;

        return new ChatDbContext(options);
    }
}
