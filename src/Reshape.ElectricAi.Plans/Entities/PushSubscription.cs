namespace Reshape.ElectricAi.Plans.Entities;

public class PushSubscription
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
}
