using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Extensions;
using Reshape.ElectricAi.Plans.Persistence.Specifications;

namespace Reshape.ElectricAi.Plans.Services;

public sealed class PreferencesService(IRepository<UserPreferences> repository) : IPreferencesService
{
    public async Task<PreferencesDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await repository.FirstOrDefaultAsync(new UserPreferencesWithChildrenSpec(userId), cancellationToken);
        return entity.ToDto();
    }

    public async Task<PreferencesDto> ReplaceAsync(Guid userId, PreferencesReplaceRequest request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var entity = await repository.FirstOrDefaultAsync(new UserPreferencesWithChildrenSpec(userId), cancellationToken);

        if (entity is null)
        {
            entity = new UserPreferences { UserId = userId, UpdatedUtc = nowUtc };
            entity.ApplyReplace(request, nowUtc);
            await repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.ApplyReplace(request, nowUtc);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<PreferencesDto> PatchAsync(Guid userId, PreferencesPatchRequest request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var entity = await repository.FirstOrDefaultAsync(new UserPreferencesWithChildrenSpec(userId), cancellationToken);

        if (entity is null)
        {
            entity = new UserPreferences { UserId = userId, UpdatedUtc = nowUtc };
            entity.ApplyPatch(request, nowUtc);
            await repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.ApplyPatch(request, nowUtc);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }
}
