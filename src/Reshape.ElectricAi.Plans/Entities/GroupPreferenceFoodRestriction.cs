using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class GroupPreferenceFoodRestriction
{
    public Guid GroupId { get; set; }
    public FoodRestriction Restriction { get; set; }

    public GroupPreferences? GroupPreferences { get; set; }
}
