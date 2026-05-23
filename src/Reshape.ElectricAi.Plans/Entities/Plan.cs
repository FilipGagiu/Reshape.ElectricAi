namespace Reshape.ElectricAi.Plans.Entities;

public sealed class Plan
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string ContentJson { get; set; } = "{}";
    public DateTime GeneratedUtc { get; set; }

    public User? Owner { get; set; }
}
