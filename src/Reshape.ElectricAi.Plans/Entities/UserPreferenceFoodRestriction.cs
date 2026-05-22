using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class UserPreferenceFoodRestriction
{
    public Guid UserId { get; set; }
    public FoodRestriction Restriction { get; set; }

    public UserPreferences? UserPreferences { get; set; }
}
