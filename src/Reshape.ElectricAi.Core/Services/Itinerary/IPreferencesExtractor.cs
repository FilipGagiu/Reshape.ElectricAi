using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Preferences;

namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IPreferencesExtractor
{
    Task<AiExtractedPreferences> ExtractAsync(
        IReadOnlyList<WizardAnswer> answers,
        string? freeText,
        string locale,
        CancellationToken cancellationToken);
}
