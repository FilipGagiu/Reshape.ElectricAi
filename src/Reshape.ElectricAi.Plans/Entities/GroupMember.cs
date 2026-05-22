namespace Reshape.ElectricAi.Plans.Entities;

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedUtc { get; set; }

    public Group? Group { get; set; }
    public User? User { get; set; }
}
