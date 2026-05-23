using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.LiveFeed.Persistence.Specifications;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Persistence;

[Collection(PostgresCollection.Name)]
public class FeedRepositorySpecificationTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new FeedApiFactory(postgres);
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    private static FeedEntry CreateEntry(string title, DateTime utc) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Body = "b",
            PrimaryCategory = Category.General,
            IsGeneral = true,
            PublishedByUserId = Guid.NewGuid(),
            PublishedUtc = utc
        };

    [Fact]
    public async Task ListAsync_WithRecentFeedEntriesSpec_ReturnsLatestFirst()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<FeedEntry>>();

        db.FeedEntries.Add(CreateEntry("Older", DateTime.UtcNow.AddMinutes(-5)));
        db.FeedEntries.Add(CreateEntry("Newer", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var list = await repo.ListAsync(new RecentFeedEntriesSpec(null, 10), CancellationToken.None);
        list.Select(e => e.Title).Should().ContainInOrder("Newer", "Older");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithFeedEntryByIdSpec_ReturnsEntryWithIncludedTargets()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<FeedEntry>>();

        var entry = CreateEntry("Included", DateTime.UtcNow);
        entry.TargetArtists.Add(new FeedEntryArtist { FeedEntryId = entry.Id, ArtistName = "JT" });
        db.FeedEntries.Add(entry);
        await db.SaveChangesAsync();

        var loaded = await repo.FirstOrDefaultAsync(new FeedEntryByIdSpec(entry.Id), CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.TargetArtists.Should().ContainSingle(a => a.ArtistName == "JT");
    }
}
