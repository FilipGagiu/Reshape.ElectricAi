using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class GenreBackfillBackgroundService(
    GenreBackfillJobChannel jobChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<GenreBackfillBackgroundService> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(3101, nameof(LogStarted)),
            "Genre backfill started");

    private static readonly Action<ILogger, int, int, int, Exception?> LogCompleted =
        LoggerMessage.Define<int, int, int>(LogLevel.Information, new EventId(3102, nameof(LogCompleted)),
            "Genre backfill completed: processed={Processed}, skipped={Skipped}, failed={Failed}");

    private static readonly Action<ILogger, Exception?> LogFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(3103, nameof(LogFailed)),
            "Genre backfill failed");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var _ in jobChannel.ReadAllAsync(stoppingToken))
        {
            try
            {
                LogStarted(logger, null);

                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<GenreBackfillService>();
                var result = await service.BackfillAsync(stoppingToken);

                LogCompleted(logger, result.Processed, result.Skipped, result.Failed, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogFailed(logger, ex);
            }
            finally
            {
                jobChannel.MarkComplete();
            }
        }
    }
}
