using System.Text.Json;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class GreetingSection : IItinerarySection
{
    public string Key => "greeting";
    public int Order => 10;

    public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken)
    {
        var crew = prefs.CrewKind is null
            ? null
            : new GreetingCrewDto(prefs.CrewKind.Value, prefs.CrewEstimatedSize);
        var data = new GreetingSectionData(prefs.Name, prefs.Origin, crew);
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return Task.FromResult(new ItinerarySectionResult(Key, Order, node, null));
    }
}
