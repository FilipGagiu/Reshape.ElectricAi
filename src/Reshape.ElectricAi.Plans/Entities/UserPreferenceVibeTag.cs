namespace Reshape.ElectricAi.Plans.Entities;

public sealed class UserPreferenceVibeTag
{
    public Guid UserId { get; set; }
    public string Value { get; set; } = string.Empty;

    public UserPreferences? UserPreferences { get; set; }
}
