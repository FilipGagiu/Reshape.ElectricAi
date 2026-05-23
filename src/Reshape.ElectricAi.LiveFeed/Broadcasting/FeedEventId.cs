using System.Globalization;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal static class FeedEventId
{
    public static string FormatForEntry(Guid entryId, DateTime publishedUtc)
    {
        return $"{publishedUtc.ToString("O", CultureInfo.InvariantCulture)}-{entryId:D}";
    }

    public static bool TryParseEntryIdFromEventId(
        string? eventId, out Guid entryId, out DateTime publishedUtc)
    {
        entryId = default;
        publishedUtc = default;
        if (string.IsNullOrWhiteSpace(eventId)) return false;
        if (eventId.Length < 37) return false;

        var guidStart = eventId.Length - 36;
        var separatorAt = guidStart - 1;
        if (eventId[separatorAt] != '-') return false;

        var tsPart = eventId[..separatorAt];
        var guidPart = eventId[guidStart..];

        if (!Guid.TryParseExact(guidPart, "D", out entryId)) return false;
        if (!DateTime.TryParse(tsPart, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out publishedUtc)) return false;

        return true;
    }
}
