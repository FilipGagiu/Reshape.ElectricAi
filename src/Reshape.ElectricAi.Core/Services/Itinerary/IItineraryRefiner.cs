using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Preferences;

namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IItineraryRefiner
{
    Task<AiExtractedPreferences> RefineAsync(
        UserPreferencesSnapshot current,
        string freeText,
        string locale,
        CancellationToken cancellationToken);
}
