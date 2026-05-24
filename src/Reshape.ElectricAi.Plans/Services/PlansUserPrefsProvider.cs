using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence.Specifications;

namespace Reshape.ElectricAi.Plans.Services;

internal sealed class PlansUserPrefsProvider(IRepository<UserPreferences> repository) : IUserPrefsProvider
{
    public async Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var entity = await repository.FirstOrDefaultAsync(new UserPreferencesWithChildrenSpec(userId), ct);
        if (entity is null)
        {
            return new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>());
        }

        // OrdinalIgnoreCase shields against casing drift between published target-artist
        // strings and stored prefs. Does NOT normalize away typos/diacritics — those still
        // miss (e.g. "Nicolae" vs "Nicolaie"). Genre uses enum equality, already exact.
        var artists = new HashSet<string>(
            entity.Artists.Select(a => a.ArtistName),
            StringComparer.OrdinalIgnoreCase);
        var genres = new HashSet<MusicGenre>(entity.Genres.Select(g => g.Genre));
        return new UserFeedPrefs(artists, genres);
    }
}
