using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary.Sections;

public class TopArtistsSectionTests
{
    private static UserPreferencesSnapshot Snap(
        IReadOnlyList<string> mustSee,
        IReadOnlyList<MusicGenre> genres,
        IReadOnlyList<string> vibe) =>
        new(Guid.NewGuid(), null, null, null, null,
            vibe, genres, mustSee, [], [], [], null, null, null, null, null, null);

    [Fact]
    public async Task Must_see_always_in_top_overall_and_groups_by_day()
    {
        var day1 = new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero);
        var day2 = new DateTimeOffset(2026, 7, 16, 20, 0, 0, TimeSpan.Zero);

        var mustSeeId = Guid.NewGuid();
        var lookup = new FakeEventLookupService
        {
            Results = [new MatchedEvent(mustSeeId, "Teddy Swims", day1)]
        };
        var vector = new FakeVectorSearchService
        {
            EventResults =
            [
                new RetrievedEvent(Guid.NewGuid(), "Filler A", "txt", day1, 0.50f),
                new RetrievedEvent(Guid.NewGuid(), "Filler B", "txt", day1, 0.40f),
                new RetrievedEvent(Guid.NewGuid(), "Filler C", "txt", day1, 0.30f),
                new RetrievedEvent(Guid.NewGuid(), "Filler D", "txt", day1, 0.20f),
                new RetrievedEvent(Guid.NewGuid(), "Filler E", "txt", day2, 0.45f),
                new RetrievedEvent(Guid.NewGuid(), "Filler F", "txt", day2, 0.35f),
                new RetrievedEvent(mustSeeId, "Teddy Swims", "duplicate", day1, 0.10f),
            ]
        };

        var section = new TopArtistsSection(vector, lookup);
        var result = await section.BuildAsync(Snap(["Teddy Swims"], [MusicGenre.Pop], ["party"]), CancellationToken.None);

        Assert.Equal("topArtists", result.Key);
        Assert.Equal(50, result.Order);

        var data = result.Data.AsObject();
        var overall = data["topOverall"]!.AsArray();
        Assert.True(overall.Count <= 5);
        Assert.Contains(overall, n => (string?)n!["title"] == "Teddy Swims");

        var byDay = data["byDay"]!.AsArray();
        Assert.Equal(2, byDay.Count);
        Assert.Equal("2026-07-15", (string?)byDay[0]!["date"]);
        Assert.Equal("2026-07-16", (string?)byDay[1]!["date"]);
        Assert.True(byDay[0]!["artists"]!.AsArray().Count <= 3);
        Assert.True(byDay[1]!["artists"]!.AsArray().Count <= 3);

        // dedup: must-see entry appears only once in day 1
        var day1Artists = byDay[0]!["artists"]!.AsArray();
        Assert.Single(day1Artists, a => (string?)a!["title"] == "Teddy Swims");
    }

    [Fact]
    public async Task No_must_see_match_skipped_silently()
    {
        var lookup = new FakeEventLookupService { Results = [] };
        var vector = new FakeVectorSearchService { EventResults = [] };
        var section = new TopArtistsSection(vector, lookup);
        var result = await section.BuildAsync(Snap(["Phantom Artist"], [], []), CancellationToken.None);

        Assert.Null(result.Diagnostic);
        var data = result.Data.AsObject();
        Assert.Empty(data["topOverall"]!.AsArray());
        Assert.Empty(data["byDay"]!.AsArray());
    }
}
