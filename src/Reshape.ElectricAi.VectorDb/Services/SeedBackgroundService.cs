using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class SeedBackgroundService(
    SeedJobChannel jobChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<SeedBackgroundService> logger) : BackgroundService
{
    private static readonly Action<ILogger, string, Exception?> LogStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2001, nameof(LogStarted)), "Seed started: {DataPath}");

    private static readonly Action<ILogger, string, Exception?> LogCompleted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2002, nameof(LogCompleted)), "Seed completed: {DataPath}");

    private static readonly Action<ILogger, string, Exception?> LogFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2003, nameof(LogFailed)), "Seed failed: {DataPath}");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var dataPath in jobChannel.ReadAllAsync(stoppingToken))
        {
            try
            {
                LogStarted(logger, dataPath, null);

                await using var scope = scopeFactory.CreateAsyncScope();
                var seeder = scope.ServiceProvider.GetRequiredService<EcDataSeeder>();
                await seeder.SeedAsync(dataPath, stoppingToken);

                LogCompleted(logger, dataPath, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailed(logger, dataPath, ex);
            }
            finally
            {
                jobChannel.MarkComplete();
            }
        }
    }
}
