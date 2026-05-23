using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Preferences;

public sealed record PreferencesDto(
    TicketType? TicketType,
    Accommodation? Accommodation,
    TransportMode? Transport,
    AgeGroup? AgeGroup,
    IReadOnlyList<MusicGenre> MusicGenres,
    IReadOnlyList<FoodRestriction> FoodRestrictions,
    IReadOnlyList<ActivityType> Activities,
    IReadOnlyList<string> Artists,
    IReadOnlyList<Cuisine> Cuisines,
    int CompletionPercent,
    DateTime UpdatedUtc);
