namespace Reshape.ElectricAi.Plans.Entities;

public class UserPreferenceArtist
{
    public Guid UserId { get; set; }
    public string ArtistName { get; set; } = string.Empty;

    public UserPreferences? UserPreferences { get; set; }
}
