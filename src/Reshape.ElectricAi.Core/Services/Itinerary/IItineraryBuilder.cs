using Reshape.ElectricAi.Core.Dtos.Itinerary;

namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IItineraryBuilder
{
    Task<ItineraryDto> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken);
}
