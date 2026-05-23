using System.Net;
using System.Net.Http.Json;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public class FeedCrudTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;
    private readonly Guid _organizer = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _factory = new FeedApiFactory(postgres);
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    private HttpClient OrganizerClient() => _factory.CreateClientForUser(_organizer, UserRole.Organizer);
    private HttpClient UserClient() => _factory.CreateClientForUser(_user, UserRole.User);

    [Fact]
    public async Task PublishEntryAsOrganizer_WhenAuthenticatedAsOrganizer_Returns201AndDtoMatchingInput()
    {
        var resp = await OrganizerClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Rain", "Light shower 21:00", Category.Weather, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<FeedEntryDto>();
        dto!.Title.Should().Be("Rain");
        dto.IsGeneral.Should().BeTrue();
    }

    [Fact]
    public async Task PublishEntryAsOrganizer_WhenAuthenticatedAsUser_Returns403Envelope()
    {
        var resp = await UserClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("x", "y", Category.General, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PublishEntryAsOrganizer_WhenAnonymous_Returns401Envelope()
    {
        var resp = await _factory.CreateAnonymousClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("x", "y", Category.General, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListRecentEntries_WhenAuthenticated_ReturnsOrderedByPublishedDescending()
    {
        var org = OrganizerClient();
        for (var i = 0; i < 3; i++)
        {
            await org.PostAsJsonAsync("/api/v1/feed",
                new PublishFeedEntryRequest($"E{i}", "b", Category.General, true, [], []));
            await Task.Delay(20);
        }

        var resp = await UserClient().GetAsync("/api/v1/feed");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<FeedEntryDto>>();
        list!.Should().BeInDescendingOrder(e => e.PublishedUtc);
    }

    [Fact]
    public async Task ListRecentEntries_WhenAnonymous_Returns401Envelope() =>
        (await _factory.CreateAnonymousClient().GetAsync("/api/v1/feed")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task SoftDeleteEntryById_WhenEntryExistsAsOrganizer_RemovesFromList()
    {
        var org = OrganizerClient();
        var publish = await org.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Doomed", "b", Category.General, true, [], []));
        var dto = await publish.Content.ReadFromJsonAsync<FeedEntryDto>();

        var del = await org.DeleteAsync($"/api/v1/feed/{dto!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await UserClient().GetFromJsonAsync<List<FeedEntryDto>>("/api/v1/feed");
        list!.Any(e => e.Id == dto.Id).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEntryById_WhenEntryMissing_Returns404Envelope()
    {
        var resp = await OrganizerClient().PutAsJsonAsync($"/api/v1/feed/{Guid.NewGuid()}",
            new UpdateFeedEntryRequest("x", "y", Category.General, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("feed-entry-not-found");
    }

    [Fact]
    public async Task PublishEntry_WhenNotGeneralAndNoTargeting_Returns400ValidationEnvelope()
    {
        var resp = await OrganizerClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("t", "b", Category.General, false, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
