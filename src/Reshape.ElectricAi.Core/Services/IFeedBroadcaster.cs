using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Services;

public interface IFeedBroadcaster
{
    IAsyncEnumerable<FeedEventEnvelope> SubscribeUserToStreamAsync(
        Guid userId, UserFeedPrefs prefs, string? lastEventId, CancellationToken ct);

    void BroadcastEventToMatchingSubscribers(FeedEventKind kind, FeedEntryDto entry);
}
