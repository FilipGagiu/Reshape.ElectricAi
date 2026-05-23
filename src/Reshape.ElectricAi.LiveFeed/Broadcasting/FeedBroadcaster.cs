using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal sealed class FeedBroadcaster(IServiceScopeFactory scopeFactory) : IFeedBroadcaster
{
    private readonly ConcurrentDictionary<Guid, FeedSubscription> _subs = new();

    public async IAsyncEnumerable<FeedEventEnvelope> SubscribeUserToStreamAsync(
        Guid userId,
        UserFeedPrefs prefs,
        string? lastEventId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var sub = CreateSubscriptionForUser(userId, prefs);
        RegisterSubscription(sub);
        try
        {
            IReadOnlyList<FeedEntryDto> replay;
            using (var scope = scopeFactory.CreateScope())
            {
                var feed = scope.ServiceProvider.GetRequiredService<IFeedService>();
                replay = lastEventId is not null
                    ? await feed.ListEntriesSinceEventIdMatchingPrefsAsync(lastEventId, prefs, 10, ct)
                    : await feed.ListRecentEntriesMatchingPrefsAsync(prefs, null, 10, ct);
            }

            foreach (var entry in replay)
            {
                yield return new FeedEventEnvelope(
                    FeedEventKind.Created,
                    FeedEventId.FormatForEntry(entry.Id, entry.PublishedUtc),
                    entry);
            }

            await foreach (var env in sub.Channel.Reader.ReadAllAsync(ct))
                yield return env;
        }
        finally
        {
            RemoveSubscriptionById(sub.SubscriptionId);
            sub.Channel.Writer.TryComplete();
        }
    }

    public void BroadcastEventToMatchingSubscribers(FeedEventKind kind, FeedEntryDto entry)
    {
        var env = new FeedEventEnvelope(
            kind,
            FeedEventId.FormatForEntry(entry.Id, entry.PublishedUtc),
            entry);

        foreach (var sub in _subs.Values)
        {
            if (FeedTargeting.EntryMatchesUserPrefs(entry, sub.Prefs))
                sub.Channel.Writer.TryWrite(env);
        }
    }

    private void RegisterSubscription(FeedSubscription sub) => _subs[sub.SubscriptionId] = sub;
    private void RemoveSubscriptionById(Guid id) => _subs.TryRemove(id, out _);

    private static FeedSubscription CreateSubscriptionForUser(Guid userId, UserFeedPrefs prefs) =>
        new(
            Guid.NewGuid(),
            userId,
            prefs,
            Channel.CreateBounded<FeedEventEnvelope>(
                new BoundedChannelOptions(100)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                }));
}
