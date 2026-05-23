using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class GroupPreferences
{
    public Guid GroupId { get; set; }
    public TicketType? TicketType { get; set; }
    public Accommodation? Accommodation { get; set; }
    public TransportMode? Transport { get; set; }
    public AgeGroup? AgeGroup { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public Group? Group { get; set; }
    public List<GroupPreferenceGenre> Genres { get; set; } = [];
    public List<GroupPreferenceFoodRestriction> FoodRestrictions { get; set; } = [];
    public List<GroupPreferenceActivity> Activities { get; set; } = [];
    public List<GroupPreferenceArtist> Artists { get; set; } = [];
    public List<GroupPreferenceCuisine> Cuisines { get; set; } = [];
}
