using System.Text.Json;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class TopArtistsSection(
    IVectorSearchService vectorSearch,
    IEventLookupService eventLookup) : IItinerarySection
{
    private const int VectorTopK = 30;
    private const int PerDay = 3;
    private const int Overall = 5;
    private const float MustSeeScore = 1.0f;
    private const string DefaultQuery = "live music";
    private static readonly IReadOnlyList<string> AllCategory = ["all"];

    public string Key => "topArtists";
    public int Order => 50;

    public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken)
    {
        var mustSeeTask = eventLookup.FindByTitlesAsync(prefs.MustSeeArtists, cancellationToken);

        var queryParts = prefs.MusicGenres.Select(g => g.ToString().ToLowerInvariant())
            .Concat(prefs.VibeTags)
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var queryText = string.Join(' ', queryParts);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            queryText = DefaultQuery;
        }

        var vectorTask = vectorSearch.SearchEventsAsync(new EventSearchFilter(
            QueryText: queryText,
            TopK: VectorTopK,
            UserContext: new Dictionary<Category, IReadOnlyList<string>>
            {
                [Category.Music] = AllCategory
            }), cancellationToken);

        await Task.WhenAll(mustSeeTask, vectorTask);

        var mustSeeArtists = (await mustSeeTask)
            .Select(m => new RecommendedArtistDto(m.Id, m.Title, m.EventUtc, MustSeeScore))
            .ToList();
        var vectorArtists = (await vectorTask)
            .Select(v => new RecommendedArtistDto(v.FeedEntryId, v.Title, v.EventUtc, v.Score))
            .ToList();

        var merged = mustSeeArtists
            .Concat(vectorArtists)
            .GroupBy(a => a.Id)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .ToList();

        var topOverall = merged
            .OrderByDescending(a => a.Score)
            .Take(Overall)
            .ToList();

        var byDay = merged
            .GroupBy(a => DateOnly.FromDateTime(a.EventUtc.UtcDateTime.Date))
            .OrderBy(g => g.Key)
            .Select(g => new ArtistDayDto(
                g.Key,
                g.OrderByDescending(a => a.Score).Take(PerDay).ToList()))
            .ToList();

        var data = new TopArtistsSectionData(topOverall, byDay);
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return new ItinerarySectionResult(Key, Order, node, null);
    }
}
