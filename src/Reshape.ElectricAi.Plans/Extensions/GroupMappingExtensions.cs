using Reshape.ElectricAi.Core.Dtos.Groups;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Extensions;

internal static class GroupMappingExtensions
{
    public static GroupDto ToGroupDto(this Group group)
    {
        var members = group.Members
            .OrderBy(m => m.JoinedUtc)
            .Select(m => new GroupMemberDto(m.UserId, m.User?.Email ?? string.Empty, m.JoinedUtc))
            .ToArray();
        return new GroupDto(group.Id, group.Name, group.OwnerUserId, group.CreatedUtc, members);
    }

    public static GroupMemberDto ToMemberDto(this GroupMember member, string email) =>
        new(member.UserId, email, member.JoinedUtc);
}
