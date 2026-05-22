using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class GroupPreferenceActivity
{
    public Guid GroupId { get; set; }
    public ActivityType Activity { get; set; }

    public GroupPreferences? GroupPreferences { get; set; }
}
