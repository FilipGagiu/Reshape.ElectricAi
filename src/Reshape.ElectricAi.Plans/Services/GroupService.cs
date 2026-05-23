using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Groups;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Extensions;
using Reshape.ElectricAi.Plans.Persistence.Specifications;

namespace Reshape.ElectricAi.Plans.Services;

public sealed class GroupService(
    IRepository<Group> groupRepository,
    IRepository<GroupMember> memberRepository,
    IRepository<User> userRepository) : IGroupService
{
    public async Task<GroupDto> CreateAsync(Guid ownerUserId, CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var name = (request.Name ?? string.Empty).Trim();

        var owner = await userRepository.GetByIdAsync(ownerUserId, cancellationToken)
            ?? throw new NotFoundException("user-not-found", "Authenticated user not found.");

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerUserId = ownerUserId,
            CreatedUtc = nowUtc,
            Owner = owner
        };
        group.Members.Add(new GroupMember
        {
            GroupId = group.Id,
            UserId = ownerUserId,
            JoinedUtc = nowUtc,
            User = owner
        });

        await groupRepository.AddAsync(group, cancellationToken);
        await groupRepository.SaveChangesAsync(cancellationToken);

        return group.ToGroupDto();
    }

    public async Task<GroupDto> GetAsync(Guid groupId, Guid callerUserId, CancellationToken cancellationToken)
    {
        var group = await LoadGroupOr404Async(groupId, cancellationToken);
        EnsureMemberOr404(group, callerUserId);
        return group.ToGroupDto();
    }

    public async Task<GroupMemberDto> AddMemberAsync(Guid groupId, Guid callerUserId, AddGroupMemberRequest request, CancellationToken cancellationToken)
    {
        var group = await LoadGroupOr404Async(groupId, cancellationToken);
        EnsureOwner(group, callerUserId);

        var normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        var target = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), cancellationToken)
            ?? throw new NotFoundException("user-not-found", "No user is registered with that email.");

        if (group.Members.Any(m => m.UserId == target.Id))
        {
            throw new ConflictException("already-member", "User is already a member of this group.");
        }

        var nowUtc = DateTime.UtcNow;
        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = target.Id,
            JoinedUtc = nowUtc,
            User = target
        };

        await memberRepository.AddAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);

        return member.ToMemberDto(target.Email);
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid callerUserId, Guid targetUserId, CancellationToken cancellationToken)
    {
        var group = await LoadGroupOr404Async(groupId, cancellationToken);
        EnsureOwner(group, callerUserId);

        if (targetUserId == group.OwnerUserId)
        {
            throw new ConflictException("cannot-remove-owner", "Cannot remove the group owner.");
        }

        var member = group.Members.FirstOrDefault(m => m.UserId == targetUserId)
            ?? throw new NotFoundException("member-not-found", "User is not a member of this group.");

        memberRepository.Remove(member);
        await memberRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Group> LoadGroupOr404Async(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await groupRepository.FirstOrDefaultAsync(new GroupByIdWithMembersSpec(groupId), cancellationToken);
        return group ?? throw new NotFoundException("group-not-found", "Group not found.");
    }

    private static void EnsureMemberOr404(Group group, Guid userId)
    {
        if (group.OwnerUserId == userId) return;
        if (group.Members.Any(m => m.UserId == userId)) return;
        throw new NotFoundException("group-not-found", "Group not found.");
    }

    private static void EnsureOwner(Group group, Guid userId)
    {
        if (group.OwnerUserId == userId) return;
        if (group.Members.Any(m => m.UserId == userId))
        {
            throw new ForbiddenException("forbidden", "Only the group owner can perform this action.");
        }
        throw new NotFoundException("group-not-found", "Group not found.");
    }
}
