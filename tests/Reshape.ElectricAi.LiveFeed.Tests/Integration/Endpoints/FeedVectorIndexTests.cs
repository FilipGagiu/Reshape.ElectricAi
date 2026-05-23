using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public class FeedVectorIndexTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;
    private readonly Guid _organizer = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _factory = new FeedApiFactory(postgres);
        await _factory.ResetDatabaseAsync();
        await _factory.ResetVectorEventsAsync();
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task Publishing_an_entry_persists_an_EventEntry_in_VectorDb()
    {
        var client = _factory.CreateClientForUser(_organizer, UserRole.Organizer);

        var resp = await client.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest(
                "Stage delay",
                "Main Stage delayed 30 min",
                Category.Music,
                false,
                [],
                [MusicGenre.Rock, MusicGenre.Techno]),
            TestJson.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<FeedEntryDto>(TestJson.Options);
        dto.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
        var stored = await vectorDb.EventEntries.SingleAsync(e => e.FeedEntryId == dto!.Id);

        stored.Title.Should().Be("Stage delay");
        stored.TextRepresentation.Should().Be("Stage delay\n\nMain Stage delayed 30 min");
        // Postgres timestamptz stores microsecond precision; .NET DateTime is 100ns ticks.
        // Allow 1ms tolerance for the round-trip truncation.
        stored.EventUtc.Should().BeCloseTo(dto!.PublishedUtc, TimeSpan.FromMilliseconds(1));
        stored.Embedding.Memory.Length.Should().Be(FeedApiFactory.TestEmbeddingDimensions);
        string[] expectedTags = ["Music.Rock", "Music.Techno"];
        stored.CategoryTags.Should().BeEquivalentTo(expectedTags);
    }

    [Fact]
    public async Task Publishing_returns_201_and_broadcasts_even_when_embedder_throws()
    {
        await using var throwingFactory = new ThrowingEmbedFeedApiFactory(postgres);
        await throwingFactory.ResetDatabaseAsync();
        await throwingFactory.ResetVectorEventsAsync();

        // IsGeneral=true on the published payload below means every subscriber matches —
        // no prefs registration needed. Test exercises the broadcast-not-suppressed path
        // when the embedder throws, not the targeting path.
        var subscriberUserId = Guid.NewGuid();

        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listenTask = ReadStreamForAsync(
            throwingFactory.CreateAnonymousClient(),
            $"/api/v1/feed/stream?userId={subscriberUserId}",
            listenCts.Token);

        await Task.Delay(300, listenCts.Token);

        var client = throwingFactory.CreateClientForUser(_organizer, UserRole.Organizer);
        var resp = await client.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest(
                "Weather alert",
                "Storm after 21:00",
                Category.Weather,
                true,
                [],
                []),
            TestJson.Options);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<FeedEntryDto>(TestJson.Options);
        dto.Should().NotBeNull();

        await Task.Delay(800, listenCts.Token);
        listenCts.Cancel();
        var raw = await listenTask;

        raw.Should().Contain("event: feed.created");
        raw.Should().Contain("\"title\":\"Weather alert\"");

        using var scope = throwingFactory.Services.CreateScope();
        var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
        (await vectorDb.EventEntries.AnyAsync()).Should().BeFalse();
    }

    // Duplicated from FeedSseTests.ReadStreamForAsync to keep this file self-contained.
    // Cancellation is the normal exit for an SSE consumer in these tests.
    private static async Task<string> ReadStreamForAsync(
        HttpClient client, string url, CancellationToken ct, int maxBytes = 8192)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
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
        catch (OperationCanceledException) { }
        catch (IOException) { }
        return Encoding.UTF8.GetString(buffer, 0, read);
    }
}

internal sealed class ThrowingEmbedFeedApiFactory(PostgresFixture postgres) : FeedApiFactory(postgres)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(IEmbeddingService));
            services.Remove(descriptor);
            services.AddScoped<IEmbeddingService, ThrowingEmbeddingService>();
        });
    }
}
