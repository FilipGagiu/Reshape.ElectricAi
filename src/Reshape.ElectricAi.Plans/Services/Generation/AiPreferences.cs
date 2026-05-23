using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Services.Generation;

internal sealed record AiPreferences
{
    public TicketType? TicketType { get; init; }
    public Accommodation? Accommodation { get; init; }
    public TransportMode? Transport { get; init; }
    public AgeGroup? AgeGroup { get; init; }
    public List<MusicGenre>? MusicGenres { get; init; }
    public List<FoodRestriction>? FoodRestrictions { get; init; }
    public List<ActivityType>? Activities { get; init; }
    public List<string>? Artists { get; init; }
    public List<Cuisine>? Cuisines { get; init; }
}
