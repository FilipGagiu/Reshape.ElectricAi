using System.Text.Json;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class AccommodationSection : IItinerarySection
{
    public string Key => "accommodation";
    public int Order => 60;

    public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken)
    {
        var data = new AccommodationSectionData(prefs.AccommodationType, prefs.AccommodationNote);
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return Task.FromResult(new ItinerarySectionResult(Key, Order, node, null));
    }
}
