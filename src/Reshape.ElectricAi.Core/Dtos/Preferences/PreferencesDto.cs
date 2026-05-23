using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Preferences;

public sealed record PreferencesDto(
    string? Name,
    string? Origin,
    CrewDto? Crew,
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<MusicGenre> MusicGenres,
    IReadOnlyList<string> MustSeeArtists,
    IReadOnlyList<FoodRestriction> FoodRestrictions,
    IReadOnlyList<Cuisine> Cuisines,
    IReadOnlyList<ActivityType> ActivityInterests,
    TransportSuggestionDto? SuggestedTransport,
    AccommodationSuggestionDto? SuggestedAccommodation,
    TicketType? TicketType,
    AgeGroup? AgeGroup,
    int CompletionPercent,
    DateTime UpdatedUtc);
