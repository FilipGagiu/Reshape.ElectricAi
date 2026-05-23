namespace Reshape.ElectricAi.Core.Dtos.Notifications;

public record SendRequest(
    string Title,
    string Body,
    string? Icon,
    string? Badge,
    string? Url);
