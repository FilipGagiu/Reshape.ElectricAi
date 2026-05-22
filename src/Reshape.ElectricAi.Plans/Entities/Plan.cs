using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class Plan
{
    public Guid Id { get; set; }
    public PlanScope Scope { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? GroupId { get; set; }
    public TicketType TicketType { get; set; }
    public string ContentJson { get; set; } = "{}";
    public DateTime GeneratedUtc { get; set; }
    public DateTime? ExportedUtc { get; set; }

    public User? Owner { get; set; }
    public Group? Group { get; set; }
}
