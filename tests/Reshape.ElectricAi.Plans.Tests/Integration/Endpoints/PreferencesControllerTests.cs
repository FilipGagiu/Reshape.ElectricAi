using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class PreferencesControllerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ValidPassword = "ValidPass1!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private AuthApiFactory _factory = null!;
    private HttpClient _client = null!;
    private FakeVectorSearchService _vector = null!;
    private FakeEventLookupService _lookup = null!;

    public Task InitializeAsync()
    {
        _factory = new AuthApiFactory(postgres);
        _factory.WithFakeOpenAi();
        _vector = _factory.WithFakeVectorSearch();
        _lookup = _factory.WithFakeEventLookup();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Put_VibeTagsWithWhitespaceEntries_PersistsOnlyNormalizedValues()
    {
        var token = await RegisterAndGetTokenAsync("vibe-drop");
        SeedVectorResults();

        var putBody = EmptyReplace() with
        {
            VibeTags = new[] { "", "   ", "ok", "  trim-me  " },
        };

        var put = await SendAsync(HttpMethod.Put, "/api/v1/preferences", putBody, token);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, token);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await get.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);

        dto!.VibeTags.Should().BeEquivalentTo(new[] { "ok", "trim-me" });
    }

    [Fact]
    public async Task Put_MustSeeArtistsWithWhitespaceEntries_PersistsOnlyNormalizedValues()
    {
        var token = await RegisterAndGetTokenAsync("artists-drop");
        SeedVectorResults();

        var putBody = EmptyReplace() with
        {
            MustSeeArtists = new[] { "", " ", "Headliner", "  Padded  " },
        };

        var put = await SendAsync(HttpMethod.Put, "/api/v1/preferences", putBody, token);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, token);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await get.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);

        dto!.MustSeeArtists.Should().BeEquivalentTo(new[] { "Headliner", "Padded" });
    }

    [Fact]
    public async Task Patch_CrewWithNullKindAndValidSize_PersistsSizeWithClearedKind()
    {
        var token = await RegisterAndGetTokenAsync("crew-partial");
        SeedVectorResults();

        var seed = EmptyReplace() with { Crew = new CrewDto(CrewKind.WithGroup, 3) };
        (await SendAsync(HttpMethod.Put, "/api/v1/preferences", seed, token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var patch = EmptyPatch() with { Crew = new CrewDto(Kind: null, EstimatedSize: 7) };
        (await SendAsync(HttpMethod.Patch, "/api/v1/preferences", patch, token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, token);
        var dto = await get.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);

        dto!.Crew.Should().NotBeNull();
        dto.Crew!.Kind.Should().BeNull();
        dto.Crew.EstimatedSize.Should().Be(7);
    }

    [Fact]
    public async Task Put_TransportWithNullModeAndNote_PersistsNoteWithClearedMode()
    {
        var token = await RegisterAndGetTokenAsync("transport-partial");
        SeedVectorResults();

        var body = EmptyReplace() with
        {
            SuggestedTransport = new TransportSuggestionDto(Mode: null, Note: "Carpool with friends"),
        };
        (await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, token);
        var dto = await get.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);

        dto!.SuggestedTransport.Should().NotBeNull();
        dto.SuggestedTransport!.Mode.Should().BeNull();
        dto.SuggestedTransport.Note.Should().Be("Carpool with friends");
    }

    [Fact]
    public async Task Put_AccommodationWithNullTypeAndNote_PersistsNoteWithClearedType()
    {
        var token = await RegisterAndGetTokenAsync("accom-partial");
        SeedVectorResults();

        var body = EmptyReplace() with
        {
            SuggestedAccommodation = new AccommodationSuggestionDto(Type: null, Note: "Friend's place"),
        };
        (await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, token);
        var dto = await get.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);

        dto!.SuggestedAccommodation.Should().NotBeNull();
        dto.SuggestedAccommodation!.Type.Should().BeNull();
        dto.SuggestedAccommodation.Note.Should().Be("Friend's place");
    }

    private void SeedVectorResults()
    {
        var day = new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero);
        _vector.DocumentResults =
        [
            new RetrievedChunk(Guid.NewGuid(), "Stage", 0, "x", 0.9f),
        ];
        _vector.EventResults =
        [
            new RetrievedEvent(Guid.NewGuid(), "Headliner", "x", day, 0.95f),
        ];
        _lookup.Results = [];
    }

    private static PreferencesPatchRequest EmptyPatch() => new(
        Name: null,
        Origin: null,
        Crew: null,
        VibeTags: null,
        MusicGenres: null,
        MustSeeArtists: null,
        FoodRestrictions: null,
        Cuisines: null,
        ActivityInterests: null,
        SuggestedTransport: null,
        SuggestedAccommodation: null,
        TicketType: null,
        AgeGroup: null);

    private static PreferencesReplaceRequest EmptyReplace() => new(
        Name: null,
        Origin: null,
        Crew: null,
        VibeTags: null,
        MusicGenres: null,
        MustSeeArtists: null,
        FoodRestrictions: null,
        Cuisines: null,
        ActivityInterests: null,
        SuggestedTransport: null,
        SuggestedAccommodation: null,
        TicketType: null,
        AgeGroup: null);

private async Task<string> RegisterAndGetTokenAsync(string slug)
    {
        var email = $"prefs-{slug}-{Guid.NewGuid():N}@example.com";
        var body = new RegisterRequest(email, ValidPassword);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", body, JsonOptions);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, string? token)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }
        return await _client.SendAsync(request);
    }
}
