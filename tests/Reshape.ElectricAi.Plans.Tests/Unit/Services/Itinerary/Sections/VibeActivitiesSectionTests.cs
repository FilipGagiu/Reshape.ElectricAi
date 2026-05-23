using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary.Sections;

public class VibeActivitiesSectionTests
{
    private static UserPreferencesSnapshot Snap(IReadOnlyList<string> vibe, IReadOnlyList<ActivityType> acts) =>
        new(Guid.NewGuid(), null, null, null, null,
            vibe, [], [], [], [], acts, null, null, null, null, null, null);

    [Fact]
    public async Task Composes_query_and_emits_top_activities()
    {
        var vector = new FakeVectorSearchService
        {
            DocumentResults =
            [
                new RetrievedChunk(Guid.NewGuid(), "Stage A", 0, "Loud and proud", 0.92f),
                new RetrievedChunk(Guid.NewGuid(), "Workshop", 0, "DIY crafts", 0.81f),
            ]
        };
        var section = new VibeActivitiesSection(vector);

        var result = await section.BuildAsync(Snap(["full row", "party"], [ActivityType.Creative]), CancellationToken.None);

        Assert.Equal("vibeActivities", result.Key);
        Assert.Equal(30, result.Order);
        Assert.Single(vector.DocumentCalls);
        var call = vector.DocumentCalls[0];
        Assert.Contains("full row", call.QueryText);
        Assert.Contains("party", call.QueryText);
        Assert.Contains("creative", call.QueryText);
        Assert.Equal(5, call.TopK);
        Assert.NotNull(call.UserContext);
        Assert.True(call.UserContext!.ContainsKey(Category.Activity));

        var data = result.Data.AsObject();
        var tags = data["vibeTags"]!.AsArray();
        Assert.Equal(2, tags.Count);
        var top = data["topActivities"]!.AsArray();
        Assert.Equal(2, top.Count);
        Assert.Equal("Stage A", (string?)top[0]!["title"]);
        Assert.Equal("Loud and proud", (string?)top[0]!["snippet"]);
    }

    [Fact]
    public async Task Falls_back_to_default_query_when_empty()
    {
        var vector = new FakeVectorSearchService();
        var section = new VibeActivitiesSection(vector);

        await section.BuildAsync(Snap([], []), CancellationToken.None);

        Assert.Equal("festival activity", vector.DocumentCalls[0].QueryText);
    }
}
