using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary.Sections;

public class AccommodationSectionTests
{
    private static UserPreferencesSnapshot Snap(Accommodation? type, string? note) =>
        new(Guid.NewGuid(), null, null, null, null,
            [], [], [], [], [], [], null, null, type, note, null, null);

    [Fact]
    public async Task Emits_type_and_note()
    {
        var section = new AccommodationSection();
        var result = await section.BuildAsync(Snap(Accommodation.Camping, "near main stage"), CancellationToken.None);

        Assert.Equal("accommodation", result.Key);
        Assert.Equal(60, result.Order);
        var data = result.Data.AsObject();
        Assert.Equal("Camping", (string?)data["type"]);
        Assert.Equal("near main stage", (string?)data["note"]);
    }

    [Fact]
    public async Task Handles_nulls()
    {
        var section = new AccommodationSection();
        var result = await section.BuildAsync(Snap(null, null), CancellationToken.None);

        var data = result.Data.AsObject();
        Assert.Null((string?)data["type"]);
        Assert.Null((string?)data["note"]);
    }
}
