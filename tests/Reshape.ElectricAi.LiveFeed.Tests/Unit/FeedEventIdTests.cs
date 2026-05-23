using Reshape.ElectricAi.LiveFeed.Broadcasting;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

public class FeedEventIdTests
{
    [Fact]
    public void FormatForEntry_ProducesIso8601WithGuidSuffix()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var utc = new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc);
        var s = FeedEventId.FormatForEntry(id, utc);
        s.Should().StartWith("2026-05-23T10:00:00");
        s.Should().EndWith("-00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public void TryParseEntryIdFromEventId_RoundTripsCleanly()
    {
        var id = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        var s = FeedEventId.FormatForEntry(id, utc);
        FeedEventId.TryParseEntryIdFromEventId(s, out var parsedId, out var parsedUtc).Should().BeTrue();
        parsedId.Should().Be(id);
        parsedUtc.Should().BeCloseTo(utc, TimeSpan.FromMilliseconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("garbage")]
    [InlineData("2026-05-23T10:00:00Z-not-a-guid")]
    [InlineData("not-a-date-00000000-0000-0000-0000-000000000001")]
    public void TryParseEntryIdFromEventId_WhenInputIsMalformed_ReturnsFalse(string? input) =>
        FeedEventId.TryParseEntryIdFromEventId(input, out _, out _).Should().BeFalse();
}
