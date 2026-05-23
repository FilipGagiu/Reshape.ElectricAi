using Reshape.ElectricAi.Core.Dtos;

namespace Reshape.ElectricAi.Core.Services;

public interface IUserPrefsProvider
{
    Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct);
}
