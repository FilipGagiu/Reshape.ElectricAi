using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Services;

internal sealed class EmptyUserPrefsProvider : IUserPrefsProvider
{
    private static readonly IReadOnlySet<string> _emptyArtists = new HashSet<string>();
    private static readonly IReadOnlySet<MusicGenre> _emptyGenres = new HashSet<MusicGenre>();
    private static readonly UserFeedPrefs _emptyPrefs = new(_emptyArtists, _emptyGenres);

    public Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(_emptyPrefs);
}
