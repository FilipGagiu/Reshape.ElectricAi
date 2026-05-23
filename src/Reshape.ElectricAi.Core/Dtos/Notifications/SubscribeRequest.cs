namespace Reshape.ElectricAi.Core.Dtos.Notifications;

public record SubscribeRequest(
    string Endpoint,
    string P256dh,
    string Auth,
    string? UserAgent);
