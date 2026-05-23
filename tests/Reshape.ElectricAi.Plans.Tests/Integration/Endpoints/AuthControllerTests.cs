using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class AuthControllerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ValidPassword = "ValidPass1!";

    private readonly PostgresFixture _postgres = postgres;
    private AuthApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new AuthApiFactory(_postgres);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_NewEmail_Returns200WithTokens()
    {
        var email = UniqueEmail("register-new");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrEmpty();
        payload.RefreshToken.Should().NotBeNullOrEmpty();
        payload.ExpiresIn.Should().BeGreaterThan(0);
        payload.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercase()
    {
        var mixedCaseEmail = $"Alice-{Guid.NewGuid():N}@EXAMPLE.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(mixedCaseEmail, ValidPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        payload!.User.Email.Should().Be(mixedCaseEmail.ToLowerInvariant());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409WithCode()
    {
        var email = UniqueEmail("register-dup");
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        envelope!.Error.Code.Should().Be("email-in-use");
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(UniqueEmail("weak-pw"), "weakshort"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var email = UniqueEmail("login-ok");
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, ValidPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithStableCode()
    {
        var email = UniqueEmail("login-wrong");
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        envelope!.Error.Code.Should().Be("invalid-credentials");
    }

    [Fact]
    public async Task Login_MissingUser_Returns401WithSameCodeAsWrongPassword()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(UniqueEmail("login-missing"), ValidPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var envelope = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        envelope!.Error.Code.Should().Be("invalid-credentials");
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewPair()
    {
        var email = UniqueEmail("refresh-ok");
        var registered = await RegisterAndReadAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(registered.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        payload!.RefreshToken.Should().NotBe(registered.RefreshToken);
        payload.AccessToken.Should().NotBe(registered.AccessToken);
    }

    [Fact]
    public async Task Refresh_AlreadyRotatedToken_Returns401()
    {
        var email = UniqueEmail("refresh-replay");
        var registered = await RegisterAndReadAsync(email);
        var firstRotate = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(registered.RefreshToken));
        firstRotate.EnsureSuccessStatusCode();

        var replay = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(registered.RefreshToken));

        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var envelope = await replay.Content.ReadFromJsonAsync<ErrorEnvelope>();
        envelope!.Error.Code.Should().Be("invalid-refresh-token");
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest("not-a-real-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidJwt_Returns200()
    {
        var email = UniqueEmail("me-ok");
        var registered = await RegisterAndReadAsync(email);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<UserDto>();
        dto!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Me_NoToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshTokenRow_StoresHashNotPlain_AndRotationMarksReplacedBy()
    {
        var email = UniqueEmail("hash-check");
        var registered = await RegisterAndReadAsync(email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();

        var rowBeforeRotate = await db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(rt => rt.UserId != Guid.Empty && rt.RevokedUtc == null);
        rowBeforeRotate.Should().NotBeNull();
        rowBeforeRotate!.TokenHash.Should().NotBe(registered.RefreshToken);

        var rotate = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(registered.RefreshToken));
        rotate.EnsureSuccessStatusCode();

        var rowsAfter = await db.RefreshTokens.AsNoTracking().Where(rt => rt.UserId == rowBeforeRotate.UserId).ToListAsync();
        var rotated = rowsAfter.Single(rt => rt.Id == rowBeforeRotate.Id);
        rotated.RevokedUtc.Should().NotBeNull();
        rotated.ReplacedByHash.Should().NotBeNullOrEmpty();
    }

    private async Task<AuthResponse> RegisterAndReadAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return payload!;
    }

    private static string UniqueEmail(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@example.com";

    private sealed record ErrorEnvelope(ErrorPayload Error);
    private sealed record ErrorPayload(string Code, string Message);
}
