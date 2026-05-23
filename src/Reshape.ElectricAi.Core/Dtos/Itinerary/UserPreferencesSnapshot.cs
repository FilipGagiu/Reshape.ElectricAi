using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record UserPreferencesSnapshot(
    Guid UserId,
    string? Name,
    string? Origin,
    CrewKind? CrewKind,
    int? CrewEstimatedSize,
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<MusicGenre> MusicGenres,
    IReadOnlyList<string> MustSeeArtists,
    IReadOnlyList<FoodRestriction> FoodRestrictions,
    IReadOnlyList<Cuisine> Cuisines,
    IReadOnlyList<ActivityType> ActivityInterests,
    TransportMode? TransportMode,
    string? TransportNote,
    Accommodation? AccommodationType,
    string? AccommodationNote,
    TicketType? TicketType,
    AgeGroup? AgeGroup);
