namespace Reshape.ElectricAi.Core.Dtos.Notifications;

public record SendResult(int Delivered, int Pruned, int Failed);
