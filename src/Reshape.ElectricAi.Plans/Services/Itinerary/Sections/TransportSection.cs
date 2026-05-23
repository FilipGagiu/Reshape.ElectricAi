using System.Text.Json;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class TransportSection : IItinerarySection
{
    public string Key => "transport";
    public int Order => 20;

    public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken)
    {
        var data = new TransportSectionData(prefs.TransportMode, prefs.TransportNote);
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return Task.FromResult(new ItinerarySectionResult(Key, Order, node, null));
    }
}
