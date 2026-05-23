using System.Text.Json;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class FoodSection(IVectorSearchService vectorSearch) : IItinerarySection
{
    private const int TopK = 5;
    private const string DefaultQuery = "restaurant";
    private static readonly IReadOnlyList<string> AllCategory = ["all"];

    public string Key => "food";
    public int Order => 40;

    public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken)
    {
        var queryParts = prefs.FoodRestrictions.Select(EnumNaturalLanguage.ForEmbedding)
            .Concat(prefs.Cuisines.Select(c => c.ToString().ToLowerInvariant()))
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
                [Category.Food] = AllCategory
            });

        var hits = await vectorSearch.SearchDocumentsAsync(filter, cancellationToken);

        var data = new FoodSectionData(
            prefs.FoodRestrictions,
            prefs.Cuisines,
            hits.Select(h => new RecommendedActivityDto(h.DocumentId, h.DocumentTitle, h.Content, h.Score)).ToList());

        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return new ItinerarySectionResult(Key, Order, node, null);
    }
}
