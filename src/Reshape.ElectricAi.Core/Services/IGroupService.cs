using Reshape.ElectricAi.Core.Dtos.Groups;

namespace Reshape.ElectricAi.Core.Services;

public interface IGroupService
{
    Task<GroupDto> CreateAsync(Guid ownerUserId, CreateGroupRequest request, CancellationToken cancellationToken);

    Task<GroupDto> GetAsync(Guid groupId, Guid callerUserId, CancellationToken cancellationToken);

    Task<GroupMemberDto> AddMemberAsync(Guid groupId, Guid callerUserId, AddGroupMemberRequest request, CancellationToken cancellationToken);

    Task RemoveMemberAsync(Guid groupId, Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken);
}
