namespace Reshape.ElectricAi.Core.Dtos.Notifications;

public record PushPayload(
    string Title,
    string Body,
    string? Icon,
    string? Badge,
    string? Url);
