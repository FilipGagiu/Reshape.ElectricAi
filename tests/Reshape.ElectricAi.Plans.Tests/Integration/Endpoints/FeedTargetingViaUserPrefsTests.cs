using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class FeedTargetingViaUserPrefsTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task TargetedByGenre_VisibleToUserWithMatchingGenrePref()
    {
        var (userToken, userId, _) = await RegisterUserAsync("genre-match");
        await SeedPreferencesAsync(userId, genres: [MusicGenre.HipHop], artists: []);

        var organizerToken = MintOrganizerToken();
        var title = $"Rock stage delay {Guid.NewGuid()}";
        await PublishEntryAsync(organizerToken,
            title: title, body: "30 min late",
            primaryCategory: Category.Music, isGeneral: false,
            targetArtists: [], targetGenres: [MusicGenre.HipHop]);

        var feed = await GetFeedAsync(userToken);
        feed.Should().Contain(e => e.Title == title);
    }

    [Fact]
    public async Task TargetedByArtist_VisibleToUserWithMatchingArtistPref()
    {
        var (userToken, userId, _) = await RegisterUserAsync("artist-match");
        await SeedPreferencesAsync(userId, genres: [], artists: ["Nicolae Guta"]);

        var organizerToken = MintOrganizerToken();
        var title = $"Set change {Guid.NewGuid()}";
        await PublishEntryAsync(organizerToken,
            title: title, body: "Nicolae Guta moved to main stage",
            primaryCategory: Category.Music, isGeneral: false,
            targetArtists: ["Nicolae Guta"], targetGenres: []);

        var feed = await GetFeedAsync(userToken);
        feed.Should().Contain(e => e.Title == title);
    }

    [Fact]
    public async Task TargetedByArtist_CaseInsensitiveMatchAgainstUserPref()
    {
        var (userToken, userId, _) = await RegisterUserAsync("artist-case");
        // Pref stored mixed case; entry target uppercase. Provider's OrdinalIgnoreCase
        // HashSet folds both to a single equivalence class -> overlap fires.
        await SeedPreferencesAsync(userId, genres: [], artists: ["nicolae guta"]);

        var organizerToken = MintOrganizerToken();
        var title = $"Casing check {Guid.NewGuid()}";
        await PublishEntryAsync(organizerToken,
            title: title, body: "casing test",
            primaryCategory: Category.Music, isGeneral: false,
            targetArtists: ["NICOLAE GUTA"], targetGenres: []);

        var feed = await GetFeedAsync(userToken);
        feed.Should().Contain(e => e.Title == title);
    }

    [Fact]
    public async Task TargetedToOtherGenre_NotVisibleWhenPrefsDoNotMatch()
    {
        var (userToken, userId, _) = await RegisterUserAsync("genre-miss");
        await SeedPreferencesAsync(userId, genres: [MusicGenre.Rock], artists: []);

        var organizerToken = MintOrganizerToken();
        var title = $"HipHop only {Guid.NewGuid()}";
        await PublishEntryAsync(organizerToken,
            title: title, body: "irrelevant for Rock fans",
            primaryCategory: Category.Music, isGeneral: false,
            targetArtists: [], targetGenres: [MusicGenre.HipHop]);

        var feed = await GetFeedAsync(userToken);
        feed.Should().NotContain(e => e.Title == title);
    }

    [Fact]
    public async Task UserWithoutPreferences_OnlySeesGeneralEntries()
    {
        var (userToken, _, _) = await RegisterUserAsync("no-prefs");
        // No SeedPreferencesAsync call -- user has zero saved prefs.

        var organizerToken = MintOrganizerToken();
        var generalTitle = $"General announcement {Guid.NewGuid()}";
        var targetedTitle = $"Targeted only {Guid.NewGuid()}";
        await PublishEntryAsync(organizerToken,
            title: generalTitle, body: "broadcast",
            primaryCategory: Category.General, isGeneral: true,
            targetArtists: [], targetGenres: []);
        await PublishEntryAsync(organizerToken,
            title: targetedTitle, body: "rockers only",
            primaryCategory: Category.Music, isGeneral: false,
            targetArtists: [], targetGenres: [MusicGenre.Rock]);

        var feed = await GetFeedAsync(userToken);
        feed.Should().Contain(e => e.Title == generalTitle);
        feed.Should().NotContain(e => e.Title == targetedTitle);
    }

    private async Task<(string Token, Guid UserId, string Email)> RegisterUserAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest(email, ValidPassword));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        // The JWT sub claim carries the userId. Decode without verification because the
        // signature was just minted by the test host -- we already trust it.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(payload!.AccessToken);
        var sub = jwt.Subject ?? throw new InvalidOperationException("sub claim missing on freshly issued token");
        return (payload.AccessToken, Guid.Parse(sub), email);
    }

    private async Task SeedPreferencesAsync(Guid userId, IReadOnlyList<MusicGenre> genres, IReadOnlyList<string> artists)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        var entity = new UserPreferences
        {
            UserId = userId,
            UpdatedUtc = DateTime.UtcNow,
            Genres = [.. genres.Select(g => new UserPreferenceGenre { UserId = userId, Genre = g })],
            Artists = [.. artists.Select(a => new UserPreferenceArtist { UserId = userId, ArtistName = a })]
        };
        db.UserPreferences.Add(entity);
        await db.SaveChangesAsync();
    }

    private string MintOrganizerToken()
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var token = tokenService.IssueAccessToken(
            new TokenSubject(Guid.NewGuid(), "organizer@example.com", UserRole.Organizer));
        return token.Token;
    }

    private async Task PublishEntryAsync(
        string organizerToken,
        string title,
        string body,
        Category primaryCategory,
        bool isGeneral,
        IReadOnlyList<string> targetArtists,
        IReadOnlyList<MusicGenre> targetGenres)
    {
        var payload = new
        {
            title,
            body,
            primaryCategory,
            isGeneral,
            targetArtists,
            targetGenres
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", organizerToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<List<FeedEntryDto>> GetFeedAsync(string userToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/feed");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var entries = await response.Content.ReadFromJsonAsync<List<FeedEntryDto>>(JsonOptions);
        return entries ?? [];
    }
}
