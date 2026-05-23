using Reshape.ElectricAi.Core.Dtos.Itinerary;

namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IItinerarySection
{
    string Key { get; }
    int Order { get; }
    Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken);
}
