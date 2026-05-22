using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class GroupPreferenceGenre
{
    public Guid GroupId { get; set; }
    public MusicGenre Genre { get; set; }

    public GroupPreferences? GroupPreferences { get; set; }
}
