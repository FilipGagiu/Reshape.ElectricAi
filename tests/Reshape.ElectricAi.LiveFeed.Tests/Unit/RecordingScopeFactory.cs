using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

internal sealed class RecordingScopeFactory : IServiceScopeFactory
{
    public int ScopeCreatedCount { get; private set; }
    public IReadOnlyList<FeedEntryDto> ReplayResult { get; set; } = Array.Empty<FeedEntryDto>();

    public IServiceScope CreateScope()
    {
        ScopeCreatedCount++;
        return new RecordingScope(this);
    }

    private sealed class RecordingScope(RecordingScopeFactory parent) : IServiceScope, IServiceProvider
    {
        public IServiceProvider ServiceProvider => this;
        public void Dispose() { }
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IFeedService))
                return new StubFeedService(parent.ReplayResult);
            return null;
        }
    }

    private sealed class StubFeedService(IReadOnlyList<FeedEntryDto> result) : IFeedService
    {
        public Task<FeedEntryDto> PublishEntryAsync(Guid o, PublishFeedEntryCommand c, CancellationToken ct) => throw new NotSupportedException();
        public Task<FeedEntryDto> UpdateEntryByIdAsync(Guid id, UpdateFeedEntryCommand c, CancellationToken ct) => throw new NotSupportedException();
        public Task SoftDeleteEntryByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<FeedEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<FeedEntryDto>> ListRecentEntriesMatchingPrefsAsync(UserFeedPrefs p, Category? c, int take, CancellationToken ct) => Task.FromResult(result);
        public Task<IReadOnlyList<FeedEntryDto>> ListEntriesSinceEventIdMatchingPrefsAsync(string lastId, UserFeedPrefs p, int take, CancellationToken ct) => Task.FromResult(result);
    }
}
