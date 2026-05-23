using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal static class UserPreferencesSnapshotFactory
{
    public static UserPreferencesSnapshot FromEntity(UserPreferences entity)
    {
        return new UserPreferencesSnapshot(
            UserId: entity.UserId,
            Name: entity.Name,
            Origin: entity.Origin,
            CrewKind: entity.CrewKind,
            CrewEstimatedSize: entity.CrewEstimatedSize,
            VibeTags: entity.VibeTags.Select(v => v.Value).ToArray(),
            MusicGenres: entity.Genres.Select(g => g.Genre).ToArray(),
            MustSeeArtists: entity.Artists.Select(a => a.ArtistName).ToArray(),
            FoodRestrictions: entity.FoodRestrictions.Select(f => f.Restriction).ToArray(),
            Cuisines: entity.Cuisines.Select(c => c.Cuisine).ToArray(),
            ActivityInterests: entity.Activities.Select(a => a.Activity).ToArray(),
            TransportMode: entity.Transport,
            TransportNote: entity.TransportNote,
            AccommodationType: entity.Accommodation,
            AccommodationNote: entity.AccommodationNote,
            TicketType: entity.TicketType,
            AgeGroup: entity.AgeGroup);
    }
}
