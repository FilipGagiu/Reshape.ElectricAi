namespace Reshape.ElectricAi.Plans.Entities;

public class Group
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public User? Owner { get; set; }
    public List<GroupMember> Members { get; set; } = [];
}
