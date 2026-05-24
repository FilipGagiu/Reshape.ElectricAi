using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class ItineraryControllerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ValidPassword = "ValidPass1!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private AuthApiFactory _factory = null!;
    private HttpClient _client = null!;
    private FakeOpenAiClient _openAi = null!;
    private FakeVectorSearchService _vector = null!;
    private FakeEventLookupService _lookup = null!;

    public Task InitializeAsync()
    {
        _factory = new AuthApiFactory(postgres);
        _openAi = _factory.WithFakeOpenAi();
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
    public async Task Generate_HappyPath_Returns200WithSixSections()
    {
        var token = await RegisterAndGetTokenAsync("happy-path");
        var userId = await GetUserIdByEmailAsync("happy-path");
        _openAi.WithEnvelope(SampleExtractedPrefs());
        SeedVectorResults();

        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        dto!.Preferences.Name.Should().Be("Paul");
        dto.Preferences.Origin.Should().Be("Cluj");
        dto.Preferences.Crew!.Kind.Should().Be(CrewKind.WithGroup);
        dto.Itinerary.Sections.Should().HaveCount(6);
        dto.Itinerary.Sections.Select(s => s.Key).Should().BeEquivalentTo(
            ["greeting", "transport", "vibeActivities", "food", "topArtists", "accommodation"]);
        dto.Itinerary.Id.Should().NotBeEmpty();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        var row = await db.Plans.AsNoTracking().FirstAsync(p => p.OwnerUserId == userId);
        row.Id.Should().Be(dto.Itinerary.Id);
    }

    [Fact]
    public async Task Generate_EmptyAnswersAndFreeText_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("empty-input");
        var body = new ItineraryGenerationRequest(1, "en", DateTimeOffset.UtcNow, [], null);

        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Generate_LlmSchemaFail_Returns502AndNoRowWritten()
    {
        var token = await RegisterAndGetTokenAsync("schema-fail");
        var userId = await GetUserIdByEmailAsync("schema-fail");
        _openAi.WithException(new LlmSchemaException("preferences"));
        SeedVectorResults();

        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        (await db.Plans.CountAsync(p => p.OwnerUserId == userId)).Should().Be(0);
        (await db.UserPreferences.CountAsync(p => p.UserId == userId)).Should().Be(0);
    }

    [Fact]
    public async Task Generate_Unauth_Returns401()
    {
        var body = SampleRequest();
        var response = await _client.PostAsJsonAsync("/api/v1/itinerary/generate", body, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_Twice_OverwritesSnapshotNoDuplicateRow()
    {
        var token = await RegisterAndGetTokenAsync("overwrite");
        var userId = await GetUserIdByEmailAsync("overwrite");
        SeedVectorResults();

        _openAi.WithEnvelope(SampleExtractedPrefs(name: "Paul"));
        var first = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDto = await first.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        _openAi.WithEnvelope(SampleExtractedPrefs(name: "Filip"));
        var second = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondDto = await second.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        // Id stays stable across re-generation — upsert reuses existing Plan.Id.
        secondDto!.Itinerary.Id.Should().Be(firstDto!.Itinerary.Id).And.NotBeEmpty();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        (await db.Plans.CountAsync(p => p.OwnerUserId == userId)).Should().Be(1);
        var prefs = await db.UserPreferences.FirstAsync(p => p.UserId == userId);
        prefs.Name.Should().Be("Filip");
    }

    [Fact]
    public async Task Get_NoSnapshot_Returns404()
    {
        var token = await RegisterAndGetTokenAsync("no-snapshot");

        var response = await SendAsync(HttpMethod.Get, "/api/v1/itinerary", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_WithSnapshot_Returns200()
    {
        var token = await RegisterAndGetTokenAsync("with-snapshot");
        _openAi.WithEnvelope(SampleExtractedPrefs());
        SeedVectorResults();
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var genDto = await gen.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/itinerary", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        dto!.Itinerary.Sections.Should().HaveCount(6);
        dto.Itinerary.Id.Should().Be(genDto!.Itinerary.Id);
    }

    [Fact]
    public async Task PutPreferences_TriggersSnapshotRebuildWithoutLlmCall()
    {
        var token = await RegisterAndGetTokenAsync("prefs-rebuild");
        SeedVectorResults();

        var putBody = new PreferencesReplaceRequest(
            Name: "Direct",
            Origin: "Cluj",
            Crew: new CrewDto(CrewKind.Solo, null),
            VibeTags: ["chill"],
            MusicGenres: null,
            MustSeeArtists: null,
            FoodRestrictions: null,
            Cuisines: null,
            ActivityInterests: null,
            SuggestedTransport: null,
            SuggestedAccommodation: null,
            TicketType: null,
            AgeGroup: null);

        var put = await SendAsync(HttpMethod.Put, "/api/v1/preferences", putBody, token);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        _openAi.CallCount.Should().Be(0);

        var get = await SendAsync(HttpMethod.Get, "/api/v1/itinerary", null, token);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await get.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        dto!.Itinerary.Sections.Should().HaveCount(6);
        dto.Itinerary.Id.Should().NotBeEmpty();
        var greeting = dto.Itinerary.Sections.First(s => s.Key == "greeting").Data.AsObject();
        ((string?)greeting["name"]).Should().Be("Direct");
    }

    [Fact]
    public async Task LatestId_NoSnapshot_Returns200WithNullId()
    {
        var token = await RegisterAndGetTokenAsync("latest-id-none");

        var response = await SendAsync(HttpMethod.Get, "/api/v1/itinerary/latest-id", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<LatestItineraryIdResponse>(JsonOptions);
        dto!.Id.Should().BeNull();
    }

    [Fact]
    public async Task LatestId_WithSnapshot_Returns200WithId()
    {
        var token = await RegisterAndGetTokenAsync("latest-id-with");
        _openAi.WithEnvelope(SampleExtractedPrefs());
        SeedVectorResults();
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var genDto = await gen.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/itinerary/latest-id", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<LatestItineraryIdResponse>(JsonOptions);
        dto!.Id.Should().Be(genDto!.Itinerary.Id);
    }

    [Fact]
    public async Task LatestId_Unauth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/itinerary/latest-id");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_Existing_Returns200()
    {
        var token = await RegisterAndGetTokenAsync("by-id-existing");
        _openAi.WithEnvelope(SampleExtractedPrefs());
        SeedVectorResults();
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var genDto = await gen.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/itinerary/{genDto!.Itinerary.Id}", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        dto!.Itinerary.Id.Should().Be(genDto.Itinerary.Id);
        dto.Itinerary.Sections.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetById_OtherUserIdAllowed_Returns200()
    {
        var tokenA = await RegisterAndGetTokenAsync("by-id-owner");
        _openAi.WithEnvelope(SampleExtractedPrefs(name: "Paul"));
        SeedVectorResults();
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), tokenA);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var genDto = await gen.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        var tokenB = await RegisterAndGetTokenAsync("by-id-stranger");

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/itinerary/{genDto!.Itinerary.Id}", null, tokenB);

        // No owner check — userB sees userA's itinerary by Id. Deliberate v1 decision.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        dto!.Itinerary.Id.Should().Be(genDto.Itinerary.Id);
        dto.Preferences.Name.Should().Be("Paul");
    }

    [Fact]
    public async Task GetById_Nonexistent_Returns404()
    {
        var token = await RegisterAndGetTokenAsync("by-id-missing");
        var randomId = Guid.NewGuid();

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/itinerary/{randomId}", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Unauth_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/itinerary/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_LegacyRowWithoutIdInJson_Returns200WithRowId()
    {
        var token = await RegisterAndGetTokenAsync("by-id-legacy");
        var userId = await GetUserIdByEmailAsync("by-id-legacy");
        var legacyRowId = Guid.NewGuid();
        // Craft ContentJson exactly as pre-Id rows looked: { generatedUtc, sections }, no "id" key.
        // Section schema must still match ItinerarySectionDto so deserialization succeeds.
        var legacyJson = """
            {"generatedUtc":"2025-01-01T00:00:00Z","sections":[{"key":"greeting","data":{"name":"Legacy"},"diagnostic":null}]}
            """;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
            db.Plans.Add(new Plan
            {
                Id = legacyRowId,
                OwnerUserId = userId,
                ContentJson = legacyJson,
                GeneratedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await db.SaveChangesAsync();
        }

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/itinerary/{legacyRowId}", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        dto!.Itinerary.Id.Should().Be(legacyRowId);
        dto.Itinerary.Sections.Should().ContainSingle().Which.Key.Should().Be("greeting");
    }

    [Fact]
    public async Task Refine_HappyPath_Returns200WithMutatedSnapshot()
    {
        var token = await RegisterAndGetTokenAsync("refine-happy");
        var userId = await GetUserIdByEmailAsync("refine-happy");
        SeedVectorResults();

        // Initial generate.
        _openAi.WithEnvelope(SampleExtractedPrefs(name: "Paul"));
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var genDto = await gen.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        var itineraryId = genDto!.Itinerary.Id;

        // Refine — refiner returns a new snapshot with a different name.
        _openAi.WithEnvelope(SampleExtractedPrefs(name: "Filip"));
        var refineBody = new ItineraryRefineRequest(itineraryId, "Call me Filip", null);
        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/refine", refineBody, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        dto!.Preferences.Name.Should().Be("Filip");
        dto.Itinerary.Id.Should().Be(itineraryId);
        dto.Itinerary.Sections.Should().HaveCount(6);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        (await db.Plans.CountAsync(p => p.OwnerUserId == userId)).Should().Be(1);
        var prefs = await db.UserPreferences.FirstAsync(p => p.UserId == userId);
        prefs.Name.Should().Be("Filip");
    }

    [Fact]
    public async Task Refine_WrongItineraryId_Returns404()
    {
        var token = await RegisterAndGetTokenAsync("refine-wrong-id");
        SeedVectorResults();
        _openAi.WithEnvelope(SampleExtractedPrefs());
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var callCountBefore = _openAi.CallCount;

        var refineBody = new ItineraryRefineRequest(Guid.NewGuid(), "Change something", null);
        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/refine", refineBody, token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        // Refiner must not be invoked when the id mismatch trips early.
        _openAi.CallCount.Should().Be(callCountBefore);
    }

    [Fact]
    public async Task Refine_NoExistingPlan_Returns404()
    {
        var token = await RegisterAndGetTokenAsync("refine-no-plan");

        var refineBody = new ItineraryRefineRequest(Guid.NewGuid(), "Change something", null);
        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/refine", refineBody, token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _openAi.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Refine_EmptyFreeText_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("refine-empty");
        SeedVectorResults();
        _openAi.WithEnvelope(SampleExtractedPrefs());
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var genDto = await gen.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        var refineBody = new ItineraryRefineRequest(genDto!.Itinerary.Id, "", null);
        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/refine", refineBody, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refine_LlmSchemaFail_Returns502_AndPrefsUnchanged()
    {
        var token = await RegisterAndGetTokenAsync("refine-schema-fail");
        var userId = await GetUserIdByEmailAsync("refine-schema-fail");
        SeedVectorResults();
        _openAi.WithEnvelope(SampleExtractedPrefs(name: "Paul"));
        var gen = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/generate", SampleRequest(), token);
        gen.StatusCode.Should().Be(HttpStatusCode.OK);
        var genDto = await gen.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);

        _openAi.WithException(new LlmSchemaException("preferences"));
        var refineBody = new ItineraryRefineRequest(genDto!.Itinerary.Id, "Make my day chiller", null);
        var response = await SendAsync(HttpMethod.Post, "/api/v1/itinerary/refine", refineBody, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        var prefs = await db.UserPreferences.FirstAsync(p => p.UserId == userId);
        // Schema fail aborts BEFORE prefsEntity.ApplyExtracted — name must stay as the generate set it.
        prefs.Name.Should().Be("Paul");
    }

    private void SeedVectorResults()
    {
        var day = new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero);
        _vector.DocumentResults =
        [
            new RetrievedChunk(Guid.NewGuid(), "Stage Alpha", 0, "Loud and bright", 0.91f),
            new RetrievedChunk(Guid.NewGuid(), "Veggie Truck", 0, "All vegetarian", 0.84f),
        ];
        _vector.EventResults =
        [
            new RetrievedEvent(Guid.NewGuid(), "Headliner Act", "Big show", day, 0.95f),
        ];
        _lookup.Results = [];
    }

    private static ItineraryGenerationRequest SampleRequest() => new(
        Version: 1,
        Locale: "en",
        SubmittedAt: DateTimeOffset.UtcNow,
        Answers: [new WizardAnswer("What should we call you?", "Paul", null)],
        FreeText: null);

    private static AiExtractedPreferences SampleExtractedPrefs(string? name = "Paul") => new(
        Name: name,
        Origin: "Cluj",
        Crew: new AiExtractedCrew(CrewKind.WithGroup, 4),
        VibeTags: ["party"],
        MusicGenres: null,
        MustSeeArtists: null,
        FoodRestrictions: [FoodRestriction.Vegetarian],
        Cuisines: [Cuisine.Italian],
        ActivityInterests: null,
        SuggestedTransport: new AiExtractedTransportSuggestion(TransportMode.Car, null),
        SuggestedAccommodation: new AiExtractedAccommodationSuggestion(Accommodation.Camping, null),
        TicketType: null,
        AgeGroup: null);

    private async Task<string> RegisterAndGetTokenAsync(string slug)
    {
        var email = $"itinerary-{slug}-{Guid.NewGuid():N}@example.com";
        var body = new RegisterRequest(email, ValidPassword);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", body, JsonOptions);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    private async Task<Guid> GetUserIdByEmailAsync(string slug)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email.StartsWith($"itinerary-{slug}-"));
        return user.Id;
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
