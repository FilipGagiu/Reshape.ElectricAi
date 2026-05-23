using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Dtos.Groups;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class GroupsControllerTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Create_ReturnsGroupWithCallerAsOwner()
    {
        var (token, email) = await RegisterAndGetTokenAndEmailAsync("owner-create");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/groups", new CreateGroupRequest("Festival Crew"), token);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<GroupDto>(JsonOptions);
        dto!.Name.Should().Be("Festival Crew");
        dto.Members.Should().HaveCount(1);
        dto.Members[0].Email.Should().Be(email);
        dto.OwnerUserId.Should().Be(dto.Members[0].UserId);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400()
    {
        var token = await RegisterAndGetTokenAsync("empty-name");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/groups", new CreateGroupRequest(""), token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_AsOwner_ReturnsGroupWithMembers()
    {
        var token = await RegisterAndGetTokenAsync("get-owner");
        var created = await CreateGroupAsync("My Group", token);

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/groups/{created.Id}", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GroupDto>(JsonOptions);
        dto!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Get_AsNonMember_Returns404()
    {
        var ownerToken = await RegisterAndGetTokenAsync("owner-isolated");
        var created = await CreateGroupAsync("Private", ownerToken);

        var strangerToken = await RegisterAndGetTokenAsync("stranger");
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/groups/{created.Id}", null, strangerToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("group-not-found");
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var token = await RegisterAndGetTokenAsync("unknown-group");

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/groups/{Guid.NewGuid()}", null, token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMember_AsOwner_Adds()
    {
        var ownerToken = await RegisterAndGetTokenAsync("add-member-owner");
        var created = await CreateGroupAsync("Crew", ownerToken);
        var (_, inviteeEmail) = await RegisterAndGetTokenAndEmailAsync("invitee");

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest(inviteeEmail), ownerToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var member = await response.Content.ReadFromJsonAsync<GroupMemberDto>(JsonOptions);
        member!.Email.Should().Be(inviteeEmail);
    }

    [Fact]
    public async Task AddMember_UnknownEmail_Returns404()
    {
        var ownerToken = await RegisterAndGetTokenAsync("unknown-invitee");
        var created = await CreateGroupAsync("Crew", ownerToken);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest("ghost@example.com"), ownerToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("user-not-found");
    }

    [Fact]
    public async Task AddMember_AlreadyMember_Returns409()
    {
        var ownerToken = await RegisterAndGetTokenAsync("dup-owner");
        var created = await CreateGroupAsync("Crew", ownerToken);
        var (_, inviteeEmail) = await RegisterAndGetTokenAndEmailAsync("dup-invitee");
        await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest(inviteeEmail), ownerToken);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest(inviteeEmail), ownerToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("already-member");
    }

    [Fact]
    public async Task AddMember_AsNonOwner_Returns403()
    {
        var ownerToken = await RegisterAndGetTokenAsync("noowner-owner");
        var created = await CreateGroupAsync("Crew", ownerToken);
        var (memberToken, memberEmail) = await RegisterAndGetTokenAndEmailAsync("member-only");
        await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest(memberEmail), ownerToken);

        var (_, otherEmail) = await RegisterAndGetTokenAndEmailAsync("another");
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest(otherEmail), memberToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_AsOwner_Removes()
    {
        var ownerToken = await RegisterAndGetTokenAsync("rm-owner");
        var created = await CreateGroupAsync("Crew", ownerToken);
        var (_, inviteeEmail) = await RegisterAndGetTokenAndEmailAsync("rm-invitee");
        var addResp = await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest(inviteeEmail), ownerToken);
        var added = await addResp.Content.ReadFromJsonAsync<GroupMemberDto>(JsonOptions);

        var response = await SendAsync(HttpMethod.Delete, $"/api/v1/groups/{created.Id}/members/{added!.UserId}", null, ownerToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await SendAsync(HttpMethod.Get, $"/api/v1/groups/{created.Id}", null, ownerToken);
        var dto = await getResp.Content.ReadFromJsonAsync<GroupDto>(JsonOptions);
        dto!.Members.Should().NotContain(m => m.UserId == added.UserId);
    }

    [Fact]
    public async Task RemoveMember_Owner_Returns409()
    {
        var ownerToken = await RegisterAndGetTokenAsync("rm-self");
        var created = await CreateGroupAsync("Crew", ownerToken);

        var response = await SendAsync(HttpMethod.Delete, $"/api/v1/groups/{created.Id}/members/{created.OwnerUserId}", null, ownerToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions);
        envelope!.Error.Code.Should().Be("cannot-remove-owner");
    }

    [Fact]
    public async Task RemoveMember_AsNonOwner_Returns403()
    {
        var ownerToken = await RegisterAndGetTokenAsync("rm-nonowner");
        var created = await CreateGroupAsync("Crew", ownerToken);
        var (memberToken, memberEmail) = await RegisterAndGetTokenAndEmailAsync("rm-victim");
        var addResp = await SendAsync(HttpMethod.Post, $"/api/v1/groups/{created.Id}/members",
            new AddGroupMemberRequest(memberEmail), ownerToken);
        var added = await addResp.Content.ReadFromJsonAsync<GroupMemberDto>(JsonOptions);

        var response = await SendAsync(HttpMethod.Delete, $"/api/v1/groups/{created.Id}/members/{added!.UserId}", null, memberToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
