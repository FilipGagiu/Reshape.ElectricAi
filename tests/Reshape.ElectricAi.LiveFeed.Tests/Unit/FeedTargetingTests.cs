using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Broadcasting;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

public class FeedTargetingTests
{
    private static FeedEntryDto Entry(bool isGeneral, string[] artists, MusicGenre[] genres) =>
        new(Guid.NewGuid(), "t", "b", Category.General, isGeneral, artists, genres, DateTime.UtcNow, null);

    private static UserFeedPrefs Prefs(string[] artists, MusicGenre[] genres) =>
        new(new HashSet<string>(artists), new HashSet<MusicGenre>(genres));

    [Fact]
    public void EntryMatchesUserPrefs_WhenIsGeneralTrue_ReturnsTrueForAnyUser() =>
        FeedTargeting.EntryMatchesUserPrefs(Entry(true, [], []), Prefs([], [])).Should().BeTrue();

    [Fact]
    public void EntryMatchesUserPrefs_WhenArtistOverlapsUserPrefs_ReturnsTrue() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, ["Justin Timberlake"], []),
            Prefs(["Justin Timberlake", "Yungblud"], [])).Should().BeTrue();

    [Fact]
    public void EntryMatchesUserPrefs_WhenGenreOverlapsUserPrefs_ReturnsTrue() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, [], [MusicGenre.Techno]),
            Prefs([], [MusicGenre.Techno, MusicGenre.House])).Should().BeTrue();

    [Fact]
    public void EntryMatchesUserPrefs_WhenNoOverlapAndNotGeneral_ReturnsFalse() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, ["Other"], [MusicGenre.Folk]),
            Prefs(["Justin Timberlake"], [MusicGenre.Techno])).Should().BeFalse();

    [Fact]
    public void EntryMatchesUserPrefs_WhenArtistMatchIsCaseSensitive_DocumentsBehavior() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, ["justin timberlake"], []),
            Prefs(["Justin Timberlake"], [])).Should().BeFalse();
}
