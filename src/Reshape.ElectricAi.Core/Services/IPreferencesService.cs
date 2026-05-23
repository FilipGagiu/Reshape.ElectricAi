using Reshape.ElectricAi.Core.Dtos.Preferences;

namespace Reshape.ElectricAi.Core.Services;

public interface IPreferencesService
{
    Task<PreferencesDto> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task<PreferencesDto> ReplaceAsync(Guid userId, PreferencesReplaceRequest request, CancellationToken cancellationToken);

    Task<PreferencesDto> PatchAsync(Guid userId, PreferencesPatchRequest request, CancellationToken cancellationToken);
}
