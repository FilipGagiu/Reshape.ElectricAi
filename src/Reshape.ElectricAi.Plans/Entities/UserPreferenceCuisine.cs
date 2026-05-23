using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class UserPreferenceCuisine
{
    public Guid UserId { get; set; }
    public Cuisine Cuisine { get; set; }

    public UserPreferences? UserPreferences { get; set; }
}
