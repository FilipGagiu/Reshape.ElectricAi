using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public class FeedServiceBroadcastOrderingTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new FeedApiFactory(postgres);
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task PublishEntry_AfterSaveChanges_BroadcastsCreatedEnvelope()
    {
        using var scope = _factory.Services.CreateScope();
        var feed = scope.ServiceProvider.GetRequiredService<IFeedService>();
        var bc = scope.ServiceProvider.GetRequiredService<IFeedBroadcaster>();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();

        var received = new List<FeedEventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var prefs = new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>());
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), prefs, null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(200, cts.Token);
        var dto = await feed.PublishEntryAsync(
            Guid.NewGuid(),
            new PublishFeedEntryCommand("OrderTest", "b", Category.General, true, [], []),
            cts.Token);

        await Task.Delay(300, cts.Token);
        cts.Cancel();
        await consume;

        db.FeedEntries.Any(e => e.Id == dto.Id).Should().BeTrue();
        received.Should().Contain(e => e.Entry.Id == dto.Id && e.Kind == FeedEventKind.Created);
    }

    [Fact]
    public async Task DeleteEntryById_WhenAlreadyDeleted_DoesNotBroadcastAndDoesNotThrow()
    {
        using var scope = _factory.Services.CreateScope();
        var feed = scope.ServiceProvider.GetRequiredService<IFeedService>();
        var bc = scope.ServiceProvider.GetRequiredService<IFeedBroadcaster>();

        var dto = await feed.PublishEntryAsync(
            Guid.NewGuid(),
            new PublishFeedEntryCommand("Doomed", "b", Category.General, true, [], []),
            CancellationToken.None);
        await feed.DeleteEntryByIdAsync(dto.Id, CancellationToken.None);

        var received = new List<FeedEventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var prefs = new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>());
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), prefs, null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(200, cts.Token);
        var act = async () => await feed.DeleteEntryByIdAsync(dto.Id, CancellationToken.None);
        await act.Should().NotThrowAsync();

        await Task.Delay(200, cts.Token);
        cts.Cancel();
        await consume;

        received.Should().NotContain(e => e.Kind == FeedEventKind.Deleted);
    }
}
