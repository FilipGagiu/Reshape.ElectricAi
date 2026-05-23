using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class UserPreferences
{
    public Guid UserId { get; set; }
    public TicketType? TicketType { get; set; }
    public Accommodation? Accommodation { get; set; }
    public TransportMode? Transport { get; set; }
    public AgeGroup? AgeGroup { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public User? User { get; set; }
    public List<UserPreferenceGenre> Genres { get; set; } = [];
    public List<UserPreferenceFoodRestriction> FoodRestrictions { get; set; } = [];
    public List<UserPreferenceActivity> Activities { get; set; } = [];
    public List<UserPreferenceArtist> Artists { get; set; } = [];
    public List<UserPreferenceCuisine> Cuisines { get; set; } = [];
}
