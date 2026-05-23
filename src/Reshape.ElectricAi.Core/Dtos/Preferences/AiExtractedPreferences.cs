using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Preferences;

public sealed record AiExtractedPreferences(
    string? Name,
    string? Origin,
    AiExtractedCrew? Crew,
    IReadOnlyList<string>? VibeTags,
    IReadOnlyList<MusicGenre>? MusicGenres,
    IReadOnlyList<string>? MustSeeArtists,
    IReadOnlyList<FoodRestriction>? FoodRestrictions,
    IReadOnlyList<Cuisine>? Cuisines,
    IReadOnlyList<ActivityType>? ActivityInterests,
    AiExtractedTransportSuggestion? SuggestedTransport,
    AiExtractedAccommodationSuggestion? SuggestedAccommodation,
    TicketType? TicketType,
    AgeGroup? AgeGroup);
