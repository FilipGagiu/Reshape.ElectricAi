using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class GroupByIdWithMembersSpec : Specification<Group>
{
    public GroupByIdWithMembersSpec(Guid groupId)
    {
        Where(g => g.Id == groupId);
        AddInclude("Members.User");
    }
}
