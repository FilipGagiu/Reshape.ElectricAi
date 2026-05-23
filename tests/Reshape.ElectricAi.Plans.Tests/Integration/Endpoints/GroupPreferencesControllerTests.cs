using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Dtos.Groups;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class GroupPreferencesControllerTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Get_NewGroup_ReturnsEmptyDto()
    {
        var token = await RegisterAndGetTokenAsync("gprefs-empty");
        var group = await CreateGroupAsync("Empty", token);

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/groups/{group.Id}/preferences", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GroupPreferencesDto>(JsonOptions);
        dto!.CompletionPercent.Should().Be(0);
        dto.MusicGenres.Should().BeEmpty();
        dto.Cuisines.Should().BeEmpty();
    }

    [Fact]
    public async Task Put_AsOwner_Replaces100Percent()
    {
        var token = await RegisterAndGetTokenAsync("gprefs-put");
        var group = await CreateGroupAsync("Full", token);
        var body = new GroupPreferencesReplaceRequest(
            TicketType.Vip,
            Accommodation.Camping,
            TransportMode.EcBus,
            AgeGroup.Adult25To34,
            [MusicGenre.Techno],
            [FoodRestriction.Vegan],
            [ActivityType.Relax],
            ["Artist X"],
            [Cuisine.Italian]);

        var response = await SendAsync(HttpMethod.Put, $"/api/v1/groups/{group.Id}/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GroupPreferencesDto>(JsonOptions);
        dto!.TicketType.Should().Be(TicketType.Vip);
        dto.Cuisines.Should().BeEquivalentTo([Cuisine.Italian]);
        dto.CompletionPercent.Should().Be(100);
    }

    [Fact]
    public async Task Put_AsNonOwnerMember_Returns403()
    {
        var ownerToken = await RegisterAndGetTokenAsync("gprefs-owner-put");
        var group = await CreateGroupAsync("OwnerOnly", ownerToken);
        var (memberToken, memberEmail) = await RegisterAndGetTokenAndEmailAsync("gprefs-member-put");
        await SendAsync(HttpMethod.Post, $"/api/v1/groups/{group.Id}/members",
            new AddGroupMemberRequest(memberEmail), ownerToken);

        var body = new GroupPreferencesReplaceRequest(TicketType.Standard, null, null, null, null, null, null, null, null);
        var response = await SendAsync(HttpMethod.Put, $"/api/v1/groups/{group.Id}/preferences", body, memberToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Patch_AsOwner_PartialUpdate()
    {
        var token = await RegisterAndGetTokenAsync("gprefs-patch");
        var group = await CreateGroupAsync("Patchable", token);
        await SendAsync(HttpMethod.Put, $"/api/v1/groups/{group.Id}/preferences",
            new GroupPreferencesReplaceRequest(
                TicketType.Standard, Accommodation.Camping, null, null,
                [MusicGenre.Rock], null, null, ["Persisted"], null), token);

        var response = await SendAsync(HttpMethod.Patch, $"/api/v1/groups/{group.Id}/preferences",
            new GroupPreferencesPatchRequest(TicketType.Vip, null, null, null, null, null, null, null, null), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GroupPreferencesDto>(JsonOptions);
        dto!.TicketType.Should().Be(TicketType.Vip);
        dto.Accommodation.Should().Be(Accommodation.Camping);
        dto.Artists.Should().BeEquivalentTo(["Persisted"]);
    }

    [Fact]
    public async Task Patch_AsNonOwner_Returns403()
    {
        var ownerToken = await RegisterAndGetTokenAsync("gprefs-patch-owner");
        var group = await CreateGroupAsync("PatchProt", ownerToken);
        var (memberToken, memberEmail) = await RegisterAndGetTokenAndEmailAsync("gprefs-patch-member");
        await SendAsync(HttpMethod.Post, $"/api/v1/groups/{group.Id}/members",
            new AddGroupMemberRequest(memberEmail), ownerToken);

        var response = await SendAsync(HttpMethod.Patch, $"/api/v1/groups/{group.Id}/preferences",
            new GroupPreferencesPatchRequest(TicketType.Black, null, null, null, null, null, null, null, null), memberToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_AsAnyMember_Allowed()
    {
        var ownerToken = await RegisterAndGetTokenAsync("gprefs-get-owner");
        var group = await CreateGroupAsync("Visible", ownerToken);
        var (memberToken, memberEmail) = await RegisterAndGetTokenAndEmailAsync("gprefs-get-member");
        await SendAsync(HttpMethod.Post, $"/api/v1/groups/{group.Id}/members",
            new AddGroupMemberRequest(memberEmail), ownerToken);
        await SendAsync(HttpMethod.Put, $"/api/v1/groups/{group.Id}/preferences",
            new GroupPreferencesReplaceRequest(TicketType.Standard, null, null, null, null, null, null, null, null), ownerToken);

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/groups/{group.Id}/preferences", null, memberToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GroupPreferencesDto>(JsonOptions);
        dto!.TicketType.Should().Be(TicketType.Standard);
    }

    [Fact]
    public async Task Get_AsNonMember_Returns404()
    {
        var ownerToken = await RegisterAndGetTokenAsync("gprefs-get-owner-stranger");
        var group = await CreateGroupAsync("Hidden", ownerToken);
        var strangerToken = await RegisterAndGetTokenAsync("gprefs-stranger");

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/groups/{group.Id}/preferences", null, strangerToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("group-not-found");
    }

    [Fact]
    public async Task Put_OverArtistCap_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("gprefs-cap");
        var group = await CreateGroupAsync("CapTest", token);
        var artists = Enumerable.Range(0, 21).Select(i => $"A{i}").ToArray();
        var body = new GroupPreferencesReplaceRequest(
            null, null, null, null, null, null, null, artists, null);

        var response = await SendAsync(HttpMethod.Put, $"/api/v1/groups/{group.Id}/preferences", body, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_WithCuisines_RoundTrip()
    {
        var token = await RegisterAndGetTokenAsync("gprefs-cuisines");
        var group = await CreateGroupAsync("CuisinesGroup", token);
        var body = new GroupPreferencesReplaceRequest(
            null, null, null, null, null, null, null, null,
            [Cuisine.Greek, Cuisine.Mediterranean, Cuisine.Bbq]);

        var response = await SendAsync(HttpMethod.Put, $"/api/v1/groups/{group.Id}/preferences", body, token);

        var dto = await response.Content.ReadFromJsonAsync<GroupPreferencesDto>(JsonOptions);
        dto!.Cuisines.Should().BeEquivalentTo([Cuisine.Greek, Cuisine.Mediterranean, Cuisine.Bbq]);
    }

    private async Task<GroupDto> CreateGroupAsync(string name, string token)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/groups", new CreateGroupRequest(name), token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GroupDto>(JsonOptions))!;
    }

    private async Task<string> RegisterAndGetTokenAsync(string prefix)
    {
        var (token, _) = await RegisterAndGetTokenAndEmailAsync(prefix);
        return token;
    }

    private async Task<(string Token, string Email)> RegisterAndGetTokenAndEmailAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return (payload!.AccessToken, email);
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
