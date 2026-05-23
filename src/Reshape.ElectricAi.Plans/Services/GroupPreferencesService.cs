using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Groups;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Extensions;
using Reshape.ElectricAi.Plans.Persistence.Specifications;

namespace Reshape.ElectricAi.Plans.Services;

public sealed class GroupPreferencesService(
    IRepository<Group> groupRepository,
    IRepository<GroupPreferences> preferencesRepository) : IGroupPreferencesService
{
    public async Task<GroupPreferencesDto> GetAsync(Guid groupId, Guid callerUserId, CancellationToken cancellationToken)
    {
        var group = await LoadGroupOr404Async(groupId, cancellationToken);
        EnsureMemberOr404(group, callerUserId);

        var entity = await preferencesRepository.FirstOrDefaultAsync(new GroupPreferencesWithChildrenSpec(groupId), cancellationToken);
        return entity.ToGroupPreferencesDto();
    }

    public async Task<GroupPreferencesDto> ReplaceAsync(Guid groupId, Guid callerUserId, GroupPreferencesReplaceRequest request, CancellationToken cancellationToken)
    {
        var group = await LoadGroupOr404Async(groupId, cancellationToken);
        EnsureOwner(group, callerUserId);

        var nowUtc = DateTime.UtcNow;
        var entity = await preferencesRepository.FirstOrDefaultAsync(new GroupPreferencesWithChildrenSpec(groupId), cancellationToken);

        if (entity is null)
        {
            entity = new GroupPreferences { GroupId = groupId, UpdatedUtc = nowUtc };
            entity.ApplyReplace(request, nowUtc);
            await preferencesRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.ApplyReplace(request, nowUtc);
        }

        await preferencesRepository.SaveChangesAsync(cancellationToken);
        return entity.ToGroupPreferencesDto();
    }

    public async Task<GroupPreferencesDto> PatchAsync(Guid groupId, Guid callerUserId, GroupPreferencesPatchRequest request, CancellationToken cancellationToken)
    {
        var group = await LoadGroupOr404Async(groupId, cancellationToken);
        EnsureOwner(group, callerUserId);

        var nowUtc = DateTime.UtcNow;
        var entity = await preferencesRepository.FirstOrDefaultAsync(new GroupPreferencesWithChildrenSpec(groupId), cancellationToken);

        if (entity is null)
        {
            entity = new GroupPreferences { GroupId = groupId, UpdatedUtc = nowUtc };
            entity.ApplyPatch(request, nowUtc);
            await preferencesRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.ApplyPatch(request, nowUtc);
        }

        await preferencesRepository.SaveChangesAsync(cancellationToken);
        return entity.ToGroupPreferencesDto();
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
