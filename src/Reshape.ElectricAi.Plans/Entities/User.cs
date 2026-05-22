using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public byte[] PasswordSalt { get; set; } = [];
    public UserRole Role { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public UserPreferences? Preferences { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<GroupMember> GroupMemberships { get; set; } = [];
}
