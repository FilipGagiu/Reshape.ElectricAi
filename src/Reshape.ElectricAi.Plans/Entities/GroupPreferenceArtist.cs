namespace Reshape.ElectricAi.Plans.Entities;

public class GroupPreferenceArtist
{
    public Guid GroupId { get; set; }
    public string ArtistName { get; set; } = string.Empty;

    public GroupPreferences? GroupPreferences { get; set; }
}
