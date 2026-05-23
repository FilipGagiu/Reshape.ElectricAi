using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services.Itinerary.Sections;

public class TransportSectionTests
{
    private static UserPreferencesSnapshot Snap(TransportMode? mode, string? note) =>
        new(Guid.NewGuid(), null, null, null, null,
            [], [], [], [], [], [], mode, note, null, null, null, null);

    [Fact]
    public async Task Emits_mode_and_note()
    {
        var section = new TransportSection();
        var result = await section.BuildAsync(Snap(TransportMode.Car, "good route"), CancellationToken.None);

        Assert.Equal("transport", result.Key);
        Assert.Equal(20, result.Order);
        var data = result.Data.AsObject();
        Assert.Equal("Car", (string?)data["mode"]);
        Assert.Equal("good route", (string?)data["note"]);
    }

    [Fact]
    public async Task Handles_nulls()
    {
        var section = new TransportSection();
        var result = await section.BuildAsync(Snap(null, null), CancellationToken.None);

        var data = result.Data.AsObject();
        Assert.Null((string?)data["mode"]);
        Assert.Null((string?)data["note"]);
    }
}
