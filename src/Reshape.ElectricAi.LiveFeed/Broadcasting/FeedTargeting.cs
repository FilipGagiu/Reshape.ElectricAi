using Reshape.ElectricAi.Core.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal static class FeedTargeting
{
    public static bool EntryMatchesUserPrefs(FeedEntryDto entry, UserFeedPrefs prefs)
    {
        if (entry.IsGeneral) return true;

        for (var i = 0; i < entry.TargetArtists.Count; i++)
            if (prefs.Artists.Contains(entry.TargetArtists[i])) return true;

        for (var i = 0; i < entry.TargetGenres.Count; i++)
            if (prefs.Genres.Contains(entry.TargetGenres[i])) return true;

        return false;
    }
}
