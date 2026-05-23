using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Entities;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public class FeedVectorCascadeDeleteTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task DeleteEntry_WhenEntryExists_RemovesMatchingVectorEventEntry()
    {
        var client = _factory.CreateClientForUser(_organizer, UserRole.Organizer);

        var publishResp = await client.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest(
                "Stage delay",
                "Main Stage delayed 30 min",
                Category.Music,
                false,
                [],
                [MusicGenre.Rock]),
            TestJson.Options);
        publishResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await publishResp.Content.ReadFromJsonAsync<FeedEntryDto>(TestJson.Options);
        dto.Should().NotBeNull();

        // Confirm the vector row was indexed by publish before we delete.
        using (var preScope = _factory.Services.CreateScope())
        {
            var preVector = preScope.ServiceProvider.GetRequiredService<VectorDbContext>();
            (await preVector.EventEntries.AnyAsync(e => e.FeedEntryId == dto!.Id))
                .Should().BeTrue();
        }

        var deleteResp = await client.DeleteAsync($"/api/v1/feed/{dto!.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var postScope = _factory.Services.CreateScope();
        var feedDb = postScope.ServiceProvider.GetRequiredService<FeedDbContext>();
        (await feedDb.FeedEntries.AnyAsync(e => e.Id == dto.Id)).Should().BeFalse();

        var vectorDb = postScope.ServiceProvider.GetRequiredService<VectorDbContext>();
        (await vectorDb.EventEntries.AnyAsync(e => e.FeedEntryId == dto.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEntry_WhenVectorRemoveThrows_StillReturns204AndRemovesFeedEntry()
    {
        await using var throwingFactory = new ThrowingRemoveEventFactory(postgres);
        await throwingFactory.ResetDatabaseAsync();
        await throwingFactory.ResetVectorEventsAsync();

        var client = throwingFactory.CreateClientForUser(_organizer, UserRole.Organizer);

        // Pre-seed an entry directly. We bypass the publish endpoint because the throwing
        // IIngestService stand-in stubs IngestEventAsync as a no-op -- pre-seeding keeps the
        // entry shape under our control and makes the test's failure modes obvious.
        var entryId = Guid.NewGuid();
        using (var seedScope = throwingFactory.Services.CreateScope())
        {
            var feedDb = seedScope.ServiceProvider.GetRequiredService<FeedDbContext>();
            feedDb.FeedEntries.Add(new FeedEntry
            {
                Id = entryId,
                Title = "Doomed entry",
                Body = "About to be hard-deleted",
                PrimaryCategory = Category.General,
                IsGeneral = true,
                PublishedByUserId = _organizer,
                PublishedUtc = DateTime.UtcNow
            });
            await feedDb.SaveChangesAsync();
        }

        var resp = await client.DeleteAsync($"/api/v1/feed/{entryId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = throwingFactory.Services.CreateScope();
        var feedDbVerify = verifyScope.ServiceProvider.GetRequiredService<FeedDbContext>();
        (await feedDbVerify.FeedEntries.AnyAsync(e => e.Id == entryId)).Should().BeFalse();
    }
}

internal sealed class ThrowingRemoveEventFactory(PostgresFixture postgres) : FeedApiFactory(postgres)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(IIngestService));
            services.Remove(descriptor);
            services.AddScoped<IIngestService, ThrowingRemoveIngestService>();
        });
    }
}

// Stand-in IIngestService. Publish/Document/QA paths are stubbed because this fixture
// is used only by the vector-remove-throws scenario, which pre-seeds the FeedEntry and
// never hits the publish flow. Only RemoveEventAsync is exercised; it throws to drive
// the SafeRemoveEventAsync swallow-on-error branch.
internal sealed class ThrowingRemoveIngestService : IIngestService
{
    public Task IngestDocumentAsync(IngestDocumentRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task IngestQAAsync(IngestQARequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task IngestEventAsync(IngestEventRequest request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveEventAsync(Guid feedEntryId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("simulated vector remove failure");
}
