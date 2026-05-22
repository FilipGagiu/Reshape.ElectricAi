using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class UserPreferenceActivity
{
    public Guid UserId { get; set; }
    public ActivityType Activity { get; set; }

    public UserPreferences? UserPreferences { get; set; }
}
