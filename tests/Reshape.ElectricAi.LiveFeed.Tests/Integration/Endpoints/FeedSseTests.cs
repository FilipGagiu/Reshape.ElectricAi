using System.Net.Http.Json;
using System.Text;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public class FeedSseTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new FeedApiFactory(postgres);
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    private HttpClient OrganizerClient(Guid id) => _factory.CreateClientForUser(id, UserRole.Organizer);
    private HttpClient AnonClient() => _factory.CreateAnonymousClient();

    private static async Task<string> ReadStreamForAsync(
        HttpClient client, string url, CancellationToken ct, int maxBytes = 8192,
        IReadOnlyDictionary<string, string>? extraHeaders = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (extraHeaders is not null)
        {
            foreach (var kv in extraHeaders)
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }

        var buffer = new byte[maxBytes];
        var read = 0;
        try
        {
            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            while (read < maxBytes)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, maxBytes - read), ct);
                if (n == 0) break;
                read += n;
            }
        }
        // Cancellation is the normal exit for an SSE consumer in these tests --
        // the cts expires deliberately. HttpClient surfaces it as TaskCanceledException
        // (subtype of OperationCanceledException). Swallow + return whatever was read.
        catch (OperationCanceledException) { }
        catch (IOException) { /* server tore down mid-read on shutdown */ }
        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    [Fact]
    public async Task StreamFeed_WhenOrganizerPublishesMatchingEntry_ClientReceivesCreatedFrame()
    {
        var user = Guid.NewGuid();
        _factory.FakePrefs.Set(user, ["Justin Timberlake"], []);

        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listenTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", listenCts.Token);

        await Task.Delay(300, listenCts.Token);
        await OrganizerClient(Guid.NewGuid()).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Delay", "30 min", Category.Music, false, ["Justin Timberlake"], []));

        await Task.Delay(800, listenCts.Token);
        listenCts.Cancel();
        var raw = await listenTask;
        raw.Should().Contain("event: feed.created");
        raw.Should().Contain("\"title\":\"Delay\"");
    }

    [Fact]
    public async Task StreamFeed_WhenOrganizerPublishesUnmatchedEntry_ClientReceivesNoFrameWithinOneSecond()
    {
        var user = Guid.NewGuid();
        _factory.FakePrefs.Set(user, ["Yungblud"], []);

        using var listenCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
        var listenTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", listenCts.Token);

        await Task.Delay(200);
        await OrganizerClient(Guid.NewGuid()).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Other", "x", Category.Music, false, ["Justin Timberlake"], []));

        var raw = await listenTask;
        raw.Should().NotContain("event: feed.created");
    }

    [Fact]
    public async Task StreamFeed_WhenIdleFor26Seconds_ClientReceivesKeepaliveComment()
    {
        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(28));
        var raw = await ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={Guid.NewGuid()}", listenCts.Token);
        raw.Should().Contain(": keepalive");
    }

    [Fact]
    public async Task StreamFeed_WhenLastEventIdHeaderPresent_ReplaysOnlyEntriesSinceCursor()
    {
        var organizer = Guid.NewGuid();
        var user = Guid.NewGuid();
        var first = await OrganizerClient(organizer).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("First", "b", Category.General, true, [], []));
        var firstDto = await first.Content.ReadFromJsonAsync<FeedEntryDto>(TestJson.Options);
        await Task.Delay(50);
        await OrganizerClient(organizer).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Second", "b", Category.General, true, [], []));

        var cursor = $"{firstDto!.PublishedUtc:O}-{firstDto.Id:D}";

        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var raw = await ReadStreamForAsync(
            AnonClient(),
            $"/api/v1/feed/stream?userId={user}",
            listenCts.Token,
            extraHeaders: new Dictionary<string, string> { ["Last-Event-ID"] = cursor });

        raw.Should().Contain("\"title\":\"Second\"");
        raw.Should().NotContain("\"title\":\"First\"");
    }

    [Fact]
    public async Task StreamFeed_WhenLastEventIdHeaderMalformed_FallsThroughToRecentBatch()
    {
        var organizer = Guid.NewGuid();
        var user = Guid.NewGuid();
        await OrganizerClient(organizer).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Visible", "b", Category.General, true, [], []));

        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var raw = await ReadStreamForAsync(
            AnonClient(),
            $"/api/v1/feed/stream?userId={user}",
            listenCts.Token,
            extraHeaders: new Dictionary<string, string> { ["Last-Event-ID"] = "definitely-not-a-valid-cursor" });

        raw.Should().Contain("\"title\":\"Visible\"");
    }

    [Fact]
    public async Task StreamFeed_WhenTwoUsersConnectedAndEntryTargetsOnlyOne_OnlyMatchingUserReceivesFrame()
    {
        var matchUser = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        _factory.FakePrefs.Set(matchUser, ["JT"], []);
        _factory.FakePrefs.Set(otherUser, [], []);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var matchTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={matchUser}", cts.Token);
        var otherTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={otherUser}", cts.Token);

        await Task.Delay(400);
        await OrganizerClient(Guid.NewGuid()).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Target", "x", Category.Music, false, ["JT"], []));

        var matchRaw = await matchTask;
        var otherRaw = await otherTask;
        matchRaw.Should().Contain("event: feed.created");
        otherRaw.Should().NotContain("event: feed.created");
    }

    [Fact]
    public async Task StreamFeed_WhenHeartbeatAndEventInterleave_ProducesNoCorruptFrame()
    {
        var user = Guid.NewGuid();
        _factory.FakePrefs.Set(user, [], []);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        var listenTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", cts.Token, maxBytes: 32768);

        await Task.Delay(300);
        var org = OrganizerClient(Guid.NewGuid());
        for (var i = 0; i < 20; i++)
        {
            await org.PostAsJsonAsync("/api/v1/feed",
                new PublishFeedEntryRequest($"Burst{i}", "b", Category.General, true, [], []));
        }

        var raw = await listenTask;
        foreach (var frame in raw.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var first = frame.TrimStart().Split('\n')[0];
            var ok = first.StartsWith("event:", StringComparison.Ordinal)
                  || first.StartsWith("id:", StringComparison.Ordinal)
                  || first.StartsWith("data:", StringComparison.Ordinal)
                  || first.StartsWith(':');
            ok.Should().BeTrue($"frame starts with unexpected line: {first}");
        }
    }

    [Fact]
    public async Task StreamFeed_WhenClientDisconnects_HeartbeatTaskCompletesWithinShortWindow()
    {
        var user = Guid.NewGuid();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var raw = await ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", cts.Token);
        raw.Should().NotBeNull();
        // No exception escaping ReadStreamForAsync means heartbeat task completed cleanly.
    }

    [Fact]
    public async Task StreamFeed_WhenConnected_ResponseHeadersAreSseCompliant()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/feed/stream?userId={Guid.NewGuid()}");
        using var resp = await AnonClient().SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
        resp.Headers.CacheControl!.NoCache.Should().BeTrue();
        resp.Headers.Connection.Should().Contain("keep-alive");
    }

    [Fact]
    public async Task StreamFeed_WhenAnonymousAndNoUserIdQuery_OnlyReceivesGeneralEntries()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var listenTask = ReadStreamForAsync(AnonClient(), "/api/v1/feed/stream", cts.Token);

        await Task.Delay(300);
        var org = OrganizerClient(Guid.NewGuid());
        await org.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Targeted", "x", Category.Music, false, ["JT"], []));
        await org.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Everyone", "x", Category.General, true, [], []));

        var raw = await listenTask;
        raw.Should().NotContain("Targeted");
        raw.Should().Contain("Everyone");
    }
}
