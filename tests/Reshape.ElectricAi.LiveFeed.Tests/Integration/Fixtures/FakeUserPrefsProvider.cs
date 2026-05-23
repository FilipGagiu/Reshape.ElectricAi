using System.Collections.Concurrent;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class FakeUserPrefsProvider : IUserPrefsProvider
{
    private readonly ConcurrentDictionary<Guid, UserFeedPrefs> _map = new();

    public void Set(Guid userId, string[] artists, MusicGenre[] genres) =>
        _map[userId] = new UserFeedPrefs(new HashSet<string>(artists), new HashSet<MusicGenre>(genres));

    public Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct)
    {
        if (_map.TryGetValue(userId, out var p)) return Task.FromResult(p);
        return Task.FromResult(new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>()));
    }
}
