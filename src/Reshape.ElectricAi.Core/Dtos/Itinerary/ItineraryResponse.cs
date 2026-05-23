using Reshape.ElectricAi.Core.Dtos.Preferences;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryResponse(PreferencesDto Preferences, ItineraryDto Itinerary);
