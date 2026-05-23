using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

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

    public Task InitializeAsync()
    {
        _factory = new AuthApiFactory(postgres);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Get_NewUser_ReturnsEmptyDtoWithZeroCompletion()
    {
        var token = await RegisterAndGetTokenAsync("get-empty");

        var response = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto.Should().NotBeNull();
        dto!.TicketType.Should().BeNull();
        dto.Accommodation.Should().BeNull();
        dto.Transport.Should().BeNull();
        dto.AgeGroup.Should().BeNull();
        dto.MusicGenres.Should().BeEmpty();
        dto.FoodRestrictions.Should().BeEmpty();
        dto.Activities.Should().BeEmpty();
        dto.Artists.Should().BeEmpty();
        dto.Cuisines.Should().BeEmpty();
        dto.CompletionPercent.Should().Be(0);
    }

    [Fact]
    public async Task Put_ReplacesAllFields_ReturnsCompletionPercent100()
    {
        var token = await RegisterAndGetTokenAsync("put-full");
        var body = new PreferencesReplaceRequest(
            TicketType.Standard,
            Accommodation.Glamping,
            TransportMode.EcBus,
            AgeGroup.Adult25To34,
            [MusicGenre.Techno, MusicGenre.House],
            [FoodRestriction.Vegan],
            [ActivityType.Relax, ActivityType.Social],
            ["Justin Timberlake", "Queens of the Stone Age"],
            [Cuisine.Italian, Cuisine.Japanese]);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.TicketType.Should().Be(TicketType.Standard);
        dto.Accommodation.Should().Be(Accommodation.Glamping);
        dto.Transport.Should().Be(TransportMode.EcBus);
        dto.AgeGroup.Should().Be(AgeGroup.Adult25To34);
        dto.MusicGenres.Should().BeEquivalentTo([MusicGenre.Techno, MusicGenre.House]);
        dto.FoodRestrictions.Should().BeEquivalentTo([FoodRestriction.Vegan]);
        dto.Activities.Should().BeEquivalentTo([ActivityType.Relax, ActivityType.Social]);
        dto.Artists.Should().BeEquivalentTo(["Justin Timberlake", "Queens of the Stone Age"]);
        dto.Cuisines.Should().BeEquivalentTo([Cuisine.Italian, Cuisine.Japanese]);
        dto.CompletionPercent.Should().Be(100);
    }

    [Fact]
    public async Task Put_NullListField_ClearsThatList()
    {
        var token = await RegisterAndGetTokenAsync("put-clear-list");
        await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null,
            ["X", "Y"],
            null), token);

        var second = await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null,
            null,
            null), token);

        var dto = await second.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.Artists.Should().BeEmpty();
    }

    [Fact]
    public async Task Patch_TicketTypeOnly_PreservesOtherFields()
    {
        var token = await RegisterAndGetTokenAsync("patch-scalar");
        await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            TicketType.Standard,
            Accommodation.Camping,
            TransportMode.Car,
            AgeGroup.Adult18To24,
            [MusicGenre.Rock],
            null,
            [ActivityType.Energetic],
            ["Alice"],
            [Cuisine.Romanian]), token);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/preferences", new PreferencesPatchRequest(
            TicketType.Vip, null, null, null, null, null, null, null, null), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.TicketType.Should().Be(TicketType.Vip);
        dto.Accommodation.Should().Be(Accommodation.Camping);
        dto.MusicGenres.Should().BeEquivalentTo([MusicGenre.Rock]);
        dto.Artists.Should().BeEquivalentTo(["Alice"]);
        dto.Cuisines.Should().BeEquivalentTo([Cuisine.Romanian]);
    }

    [Fact]
    public async Task Patch_NullListField_NoChange()
    {
        var token = await RegisterAndGetTokenAsync("patch-null-list");
        await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null,
            ["Persistent"],
            null), token);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/preferences", new PreferencesPatchRequest(
            TicketType.Standard, null, null, null, null, null, null, null, null), token);

        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.Artists.Should().BeEquivalentTo(["Persistent"]);
    }

    [Fact]
    public async Task Patch_EmptyListField_Clears()
    {
        var token = await RegisterAndGetTokenAsync("patch-empty-clear");
        await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null,
            ["ToRemove"],
            null), token);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/preferences", new PreferencesPatchRequest(
            null, null, null, null, null, null, null, [], null), token);

        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.Artists.Should().BeEmpty();
    }

    [Fact]
    public async Task Put_OverArtistCap_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("put-cap-artists");
        var artists = Enumerable.Range(0, 21).Select(i => $"Artist{i}").ToArray();
        var body = new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null,
            artists,
            null);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_DuplicateGenres_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("put-dup-genres");
        var body = new PreferencesReplaceRequest(
            null, null, null, null,
            [MusicGenre.Techno, MusicGenre.Techno],
            null, null, null,
            null);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_EmptyArtistString_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("put-empty-artist");
        var body = new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null,
            ["", "Real Artist"],
            null);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_NoAuth_Returns401WithEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("missing-token");
    }

    [Fact]
    public async Task Put_DifferentUsers_DoNotOverlap()
    {
        var tokenA = await RegisterAndGetTokenAsync("isolate-a");
        var tokenB = await RegisterAndGetTokenAsync("isolate-b");

        await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            TicketType.Standard, null, null, null,
            null, null, null, ["UserAArtist"], null), tokenA);

        await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            TicketType.Vip, null, null, null,
            null, null, null, ["UserBArtist"], null), tokenB);

        var getA = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, tokenA);
        var dtoA = await getA.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dtoA!.TicketType.Should().Be(TicketType.Standard);
        dtoA.Artists.Should().BeEquivalentTo(["UserAArtist"]);

        var getB = await SendAsync(HttpMethod.Get, "/api/v1/preferences", null, tokenB);
        var dtoB = await getB.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dtoB!.TicketType.Should().Be(TicketType.Vip);
        dtoB.Artists.Should().BeEquivalentTo(["UserBArtist"]);
    }

    [Fact]
    public async Task Put_CompletionPercent_PartialFill()
    {
        var token = await RegisterAndGetTokenAsync("completion-partial");
        var body = new PreferencesReplaceRequest(
            TicketType.Standard,
            null, null, null,
            [MusicGenre.House],
            null, null, null,
            null);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.CompletionPercent.Should().Be(22);
    }

    [Fact]
    public async Task Put_PersistsToDatabase()
    {
        var token = await RegisterAndGetTokenAsync("put-persist");
        var body = new PreferencesReplaceRequest(
            TicketType.Black,
            Accommodation.Glamping,
            null, null,
            [MusicGenre.Metal],
            null,
            null,
            ["Persisted"],
            [Cuisine.Mediterranean]);

        await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        var row = await db.UserPreferences.AsNoTracking()
            .Include(p => p.Genres)
            .Include(p => p.Artists)
            .Include(p => p.Cuisines)
            .SingleAsync();
        row.TicketType.Should().Be(TicketType.Black);
        row.Accommodation.Should().Be(Accommodation.Glamping);
        row.Genres.Select(g => g.Genre).Should().BeEquivalentTo([MusicGenre.Metal]);
        row.Artists.Select(a => a.ArtistName).Should().BeEquivalentTo(["Persisted"]);
        row.Cuisines.Select(c => c.Cuisine).Should().BeEquivalentTo([Cuisine.Mediterranean]);
    }

    [Fact]
    public async Task Put_WithCuisines_ReturnsCuisinesInDto()
    {
        var token = await RegisterAndGetTokenAsync("put-cuisines");
        var body = new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null, null,
            [Cuisine.American, Cuisine.Italian, Cuisine.Bbq]);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.Cuisines.Should().BeEquivalentTo([Cuisine.American, Cuisine.Italian, Cuisine.Bbq]);
    }

    [Fact]
    public async Task Patch_AddCuisines_PreservesOtherFields()
    {
        var token = await RegisterAndGetTokenAsync("patch-cuisines");
        await SendAsync(HttpMethod.Put, "/api/v1/preferences", new PreferencesReplaceRequest(
            TicketType.Vip,
            null, null, null,
            [MusicGenre.Rock],
            null,
            null,
            ["Existing"],
            null), token);

        var response = await SendAsync(HttpMethod.Patch, "/api/v1/preferences", new PreferencesPatchRequest(
            null, null, null, null, null, null, null, null,
            [Cuisine.Greek, Cuisine.Thai]), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.TicketType.Should().Be(TicketType.Vip);
        dto.MusicGenres.Should().BeEquivalentTo([MusicGenre.Rock]);
        dto.Artists.Should().BeEquivalentTo(["Existing"]);
        dto.Cuisines.Should().BeEquivalentTo([Cuisine.Greek, Cuisine.Thai]);
    }

    [Fact]
    public async Task Put_OverCuisineCap_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("put-cap-cuisines");
        var cuisines = Enumerable.Repeat(Cuisine.Other, 16).ToArray();
        var body = new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null, null,
            cuisines);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_DuplicateCuisine_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("put-dup-cuisines");
        var body = new PreferencesReplaceRequest(
            null, null, null, null,
            null, null, null, null,
            [Cuisine.Italian, Cuisine.Italian]);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompletionPercent_NineDimensions_Capped100()
    {
        var token = await RegisterAndGetTokenAsync("completion-nine");
        var body = new PreferencesReplaceRequest(
            TicketType.UltraVip,
            Accommodation.Camping,
            TransportMode.EcTrain,
            AgeGroup.Adult35To44,
            [MusicGenre.Pop],
            [FoodRestriction.Vegetarian],
            [ActivityType.Discovery],
            ["A1"],
            [Cuisine.French]);

        var response = await SendAsync(HttpMethod.Put, "/api/v1/preferences", body, token);

        var dto = await response.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        dto!.CompletionPercent.Should().Be(100);
    }

    private async Task<string> RegisterAndGetTokenAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return payload!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }
        return await _client.SendAsync(request);
    }

    private sealed record ErrorEnvelope(ErrorPayload Error);
    private sealed record ErrorPayload(string Code, string Message);
}
