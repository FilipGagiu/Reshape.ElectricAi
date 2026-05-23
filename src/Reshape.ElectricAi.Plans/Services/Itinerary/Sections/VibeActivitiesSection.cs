using System.Text.Json;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class VibeActivitiesSection(IVectorSearchService vectorSearch) : IItinerarySection
{
    private const int TopK = 5;
    private const string DefaultQuery = "festival activity";
    private static readonly IReadOnlyList<string> AllCategory = ["all"];

    public string Key => "vibeActivities";
    public int Order => 30;

    public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken)
    {
        var queryParts = prefs.VibeTags
            .Concat(prefs.ActivityInterests.Select(EnumNaturalLanguage.ForEmbedding))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var queryText = string.Join(' ', queryParts);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            queryText = DefaultQuery;
        }

        var filter = new DocumentSearchFilter(
            QueryText: queryText,
            TopK: TopK,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
            {
                [Category.Activity] = AllCategory
            });

        var hits = await vectorSearch.SearchDocumentsAsync(filter, cancellationToken);

        var data = new VibeActivitiesSectionData(
            prefs.VibeTags,
            hits.Select(h => new RecommendedActivityDto(h.DocumentId, h.DocumentTitle, h.Content, h.Score)).ToList());

        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return new ItinerarySectionResult(Key, Order, node, null);
    }
}
