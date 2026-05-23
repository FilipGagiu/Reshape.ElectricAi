using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Notifications;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Persistence.Specifications;
using WebPush;
using PushSubscriptionEntity = Reshape.ElectricAi.Plans.Entities.PushSubscription;
using WebPushSubscription = WebPush.PushSubscription;

namespace Reshape.ElectricAi.Plans.Services;

public sealed partial class PushService(
    IRepository<PushSubscriptionEntity> repository,
    IOptions<PushOptions> options,
    ILogger<PushService> logger) : IPushService, IDisposable
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly PushOptions _options = options.Value;
    private readonly WebPushClient _webPushClient = new();

    public string GetVapidPublicKey() => _options.VapidPublicKey;

    public async Task SubscribeAsync(SubscribeRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var existing = await repository.FirstOrDefaultAsync(
            new PushSubscriptionByEndpointSpec(request.Endpoint), cancellationToken);

        var nowUtc = DateTime.UtcNow;

        if (existing is not null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.UserAgent = request.UserAgent;
            existing.UserId = userId;
            existing.LastSeenUtc = nowUtc;
            repository.Update(existing);
        }
        else
        {
            var entity = new PushSubscriptionEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                UserAgent = request.UserAgent,
                CreatedUtc = nowUtc,
                LastSeenUtc = nowUtc
            };
            await repository.AddAsync(entity, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnsubscribeAsync(string endpoint, CancellationToken cancellationToken)
    {
        var existing = await repository.FirstOrDefaultAsync(
            new PushSubscriptionByEndpointSpec(endpoint), cancellationToken);

        if (existing is null)
        {
            return;
        }

        repository.Remove(existing);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<SendResult> SendToAllAsync(PushPayload payload, CancellationToken cancellationToken)
    {
        var vapidDetails = new VapidDetails(_options.Subject, _options.VapidPublicKey, _options.VapidPrivateKey);
        var serializedPayload = JsonSerializer.Serialize(payload, PayloadJsonOptions);

        var subscriptions = await repository.ListAsync(cancellationToken);
        var delivered = 0;
        var pruned = 0;
        var failed = 0;
        var deadSubscriptions = new List<PushSubscriptionEntity>();

        foreach (var subscription in subscriptions)
        {
            try
            {
                await _webPushClient.SendNotificationAsync(
                    new WebPushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                    serializedPayload,
                    vapidDetails,
                    cancellationToken);
                delivered++;
            }
            catch (WebPushException ex) when (IsGone(ex.StatusCode))
            {
                deadSubscriptions.Add(subscription);
                pruned++;
            }
            catch (WebPushException ex)
            {
                failed++;
                LogPushFailed(logger, (int)ex.StatusCode, subscription.Endpoint, ex);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                LogPushUnhandled(logger, subscription.Endpoint, ex);
            }
        }

        if (deadSubscriptions.Count > 0)
        {
            foreach (var dead in deadSubscriptions)
            {
                repository.Remove(dead);
            }
            await repository.SaveChangesAsync(cancellationToken);
            LogPruned(logger, deadSubscriptions.Count);
        }

        return new SendResult(delivered, pruned, failed);
    }

    private static bool IsGone(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Web push delivery failed with status {Status} for endpoint {Endpoint}.")]
    private static partial void LogPushFailed(ILogger logger, int status, string endpoint, Exception exception);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Error, Message = "Unhandled error while delivering push to {Endpoint}.")]
    private static partial void LogPushUnhandled(ILogger logger, string endpoint, Exception exception);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Pruned {Count} dead push subscriptions.")]
    private static partial void LogPruned(ILogger logger, int count);

    public void Dispose() => _webPushClient.Dispose();
}
