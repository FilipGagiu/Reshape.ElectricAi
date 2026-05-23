using Reshape.ElectricAi.Core.Dtos.Notifications;

namespace Reshape.ElectricAi.Core.Services;

public interface IPushService
{
    string GetVapidPublicKey();

    Task SubscribeAsync(SubscribeRequest request, Guid? userId, CancellationToken cancellationToken);

    Task UnsubscribeAsync(string endpoint, CancellationToken cancellationToken);

    Task<SendResult> SendToAllAsync(PushPayload payload, CancellationToken cancellationToken);
}
