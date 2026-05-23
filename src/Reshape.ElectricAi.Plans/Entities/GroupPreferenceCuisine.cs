using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class GroupPreferenceCuisine
{
    public Guid GroupId { get; set; }
    public Cuisine Cuisine { get; set; }

    public GroupPreferences? GroupPreferences { get; set; }
}
