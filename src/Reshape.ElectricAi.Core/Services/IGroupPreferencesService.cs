using Reshape.ElectricAi.Core.Dtos.Groups;

namespace Reshape.ElectricAi.Core.Services;

public interface IGroupPreferencesService
{
    Task<GroupPreferencesDto> GetAsync(Guid groupId, Guid callerUserId, CancellationToken cancellationToken);

    Task<GroupPreferencesDto> ReplaceAsync(Guid groupId, Guid callerUserId, GroupPreferencesReplaceRequest request, CancellationToken cancellationToken);

    Task<GroupPreferencesDto> PatchAsync(Guid groupId, Guid callerUserId, GroupPreferencesPatchRequest request, CancellationToken cancellationToken);
}
