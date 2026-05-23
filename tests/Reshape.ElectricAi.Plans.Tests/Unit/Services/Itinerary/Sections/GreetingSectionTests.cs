using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary.Sections;

public class GreetingSectionTests
{
    private static UserPreferencesSnapshot Snap(string? name, string? origin, CrewKind? kind, int? size) =>
        new(Guid.NewGuid(), name, origin, kind, size,
            [], [], [], [], [], [], null, null, null, null, null, null);

    [Fact]
    public async Task Emits_name_origin_crew()
    {
        var section = new GreetingSection();
        var result = await section.BuildAsync(Snap("Paul", "Cluj", CrewKind.WithGroup, 4), CancellationToken.None);

        Assert.Equal("greeting", result.Key);
        Assert.Equal(10, result.Order);
        Assert.Null(result.Diagnostic);

        var data = result.Data.AsObject();
        Assert.Equal("Paul", (string?)data["name"]);
        Assert.Equal("Cluj", (string?)data["origin"]);
        Assert.Equal("WithGroup", (string?)data["crew"]?["kind"]);
        Assert.Equal(4, (int?)data["crew"]?["size"]);
    }

    [Fact]
    public async Task Handles_nulls()
    {
        var section = new GreetingSection();
        var result = await section.BuildAsync(Snap(null, null, null, null), CancellationToken.None);

        var data = result.Data.AsObject();
        Assert.Null((string?)data["name"]);
        Assert.Null((string?)data["origin"]);
        Assert.Null(data["crew"]);
    }
}
