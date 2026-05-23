using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Broadcasting;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

public class FeedBroadcasterTests
{
    private static UserFeedPrefs Prefs(string[] a) => new(new HashSet<string>(a), new HashSet<MusicGenre>());
    private static FeedEntryDto Entry(bool general, string[] artists) =>
        new(Guid.NewGuid(), "t", "b", Category.General, general, artists, [], DateTime.UtcNow, null);

    [Fact]
    public async Task BroadcastEventToMatchingSubscribers_WhenSubscriberMatches_WritesEnvelopeToChannel()
    {
        var bc = new FeedBroadcaster(new RecordingScopeFactory());
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var received = new List<FeedEventEnvelope>();
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs(["JT"]), null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(150, cts.Token);
        bc.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, Entry(false, ["JT"]));
        await Task.Delay(250, cts.Token);
        cts.Cancel();
        await consume;

        received.Should().Contain(e => e.Kind == FeedEventKind.Created);
    }

    [Fact]
    public async Task BroadcastEventToMatchingSubscribers_WhenSubscriberDoesNotMatch_DoesNotWrite()
    {
        var bc = new FeedBroadcaster(new RecordingScopeFactory());
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var received = new List<FeedEventEnvelope>();
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs(["JT"]), null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(150, cts.Token);
        bc.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, Entry(false, ["Other"]));
        await Task.Delay(250, cts.Token);
        cts.Cancel();
        await consume;

        received.Should().BeEmpty();
    }

    [Fact]
    public async Task SubscribeUserToStreamAsync_WhenLastEventIdNullAndNoHistory_YieldsZeroReplayEntries()
    {
        var factory = new RecordingScopeFactory { ReplayResult = Array.Empty<FeedEntryDto>() };
        var bc = new FeedBroadcaster(factory);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));

        var received = new List<FeedEventEnvelope>();
        try
        {
            await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs([]), null, cts.Token))
                received.Add(env);
        }
        catch (OperationCanceledException) { }

        received.Should().BeEmpty();
        factory.ScopeCreatedCount.Should().Be(1);
    }

    [Fact]
    public async Task SubscribeUserToStreamAsync_OnReplay_ResolvesFreshIFeedServiceScopePerCall()
    {
        var factory = new RecordingScopeFactory { ReplayResult = Array.Empty<FeedEntryDto>() };
        var bc = new FeedBroadcaster(factory);
        using var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        async Task Drain(CancellationToken ct)
        {
            try
            {
                await foreach (var _ in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs([]), null, ct)) { }
            }
            catch (OperationCanceledException) { }
        }

        await Task.WhenAll(Drain(cts1.Token), Drain(cts2.Token));
        factory.ScopeCreatedCount.Should().Be(2);
    }
}
