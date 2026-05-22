using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class UserPreferenceGenre
{
    public Guid UserId { get; set; }
    public MusicGenre Genre { get; set; }

    public UserPreferences? UserPreferences { get; set; }
}
