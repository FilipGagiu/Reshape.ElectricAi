using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Groups;

public sealed record GroupPreferencesReplaceRequest(
    TicketType? TicketType,
    Accommodation? Accommodation,
    TransportMode? Transport,
    AgeGroup? AgeGroup,
    IReadOnlyList<MusicGenre>? MusicGenres,
    IReadOnlyList<FoodRestriction>? FoodRestrictions,
    IReadOnlyList<ActivityType>? Activities,
    IReadOnlyList<string>? Artists,
    IReadOnlyList<Cuisine>? Cuisines);
