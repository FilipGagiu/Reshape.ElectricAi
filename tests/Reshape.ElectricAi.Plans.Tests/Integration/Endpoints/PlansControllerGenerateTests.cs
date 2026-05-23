using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Core.Dtos.Plans;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class PlansControllerGenerateTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ValidPassword = "ValidPass1!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private AuthApiFactory _factory = null!;
    private HttpClient _client = null!;
    private FakeOpenAiClient _fakeOpenAi = null!;

    public Task InitializeAsync()
    {
        _factory = new AuthApiFactory(postgres);
        _fakeOpenAi = _factory.WithFakeOpenAi();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<string> RegisterAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, ValidPassword), JsonOptions);
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return payload!.AccessToken;
    }

    private static PlanGenerationRequest BuildRequest() =>
        new(
            Answers: [new WizardAnswer("vibe", "What's your vibe?", "chill")],
            FreeText: "first time at EC");

    private void QueueValidEnvelope(string tip = "Pack layers for the chill evenings.") =>
        _fakeOpenAi.WithEnvelope(new
        {
            preferences = new
            {
                ticketType = "Standard",
                accommodation = "Glamping",
                transport = "EcBus",
                ageGroup = "Adult25To34",
                musicGenres = new[] { "Rock" },
                foodRestrictions = Array.Empty<string>(),
                activities = new[] { "Relax" },
                artists = new[] { "Foo Fighters" },
                cuisines = new[] { "Italian" }
            },
            plan = new
            {
                scope = "individual",
                ticketType = "Standard",
                days = new[]
                {
                    new
                    {
                        date = "2025-07-17",
                        transport = new
                        {
                            outbound = new { mode = "EcBus", from = "Iulius Mall", departLocal = "17:55", note = (string?)null },
                            @return = new { mode = "EcBus", from = (string?)null, departLocal = (string?)null, note = "Non-stop" }
                        },
                        concerts = new[] { new { stage = "Main Stage", artist = "Foo Fighters", startLocal = "22:30", endLocal = "23:45" } },
                        activities = new[] { new { name = "Castle Market", note = "Explore" } },
                        weatherNotes = new[] { "Bring poncho." }
                    }
                },
                food = Array.Empty<object>(),
                budget = new { ticket = 12000, transport = 800, accommodation = 5000, food = 2500, drinks = 1500, chaosFund = 1500, total = 23300, currency = "RON-cents" }
            },
            tip
        });

    private async Task<HttpResponseMessage> PostGenerateAsync(string token, PlanGenerationRequest request)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/plans/generate")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(msg);
    }

    [Fact]
    public async Task Generate_NoToken_Returns401WithEnvelope()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/plans/generate", BuildRequest(), JsonOptions);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_EmptyAnswers_Returns400()
    {
        var token = await RegisterAsync("empty");
        var resp = await PostGenerateAsync(token, new PlanGenerationRequest(Array.Empty<WizardAnswer>(), null));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Generate_FreeTextTooLong_Returns400()
    {
        var token = await RegisterAsync("free-too-long");
        var resp = await PostGenerateAsync(
            token,
            new PlanGenerationRequest(
                [new WizardAnswer("vibe", "Q", "A")],
                new string('x', 2001)));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Generate_HappyPath_Returns200WithFullResult()
    {
        var token = await RegisterAsync("happy");
        QueueValidEnvelope();

        var resp = await PostGenerateAsync(token, BuildRequest());

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PlanGenerationResult>(JsonOptions);
        body!.Tip.Should().Be("Pack layers for the chill evenings.");
        body.Plan.Days.Should().HaveCount(1);
        body.Preferences.MusicGenres.Should().Contain(Core.Enums.MusicGenre.Rock);
    }

    [Fact]
    public async Task Generate_LlmThrows_Returns502()
    {
        var token = await RegisterAsync("llm-throw");
        _fakeOpenAi.WithException(new LlmException("llm-unavailable", "boom"));

        var resp = await PostGenerateAsync(token, BuildRequest());

        resp.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Generate_LlmSchemaException_Returns502()
    {
        var token = await RegisterAsync("llm-schema");
        _fakeOpenAi.WithException(new LlmSchemaException("plan.days"));

        var resp = await PostGenerateAsync(token, BuildRequest());

        resp.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Generate_RateLimitHit_Returns429()
    {
        var token = await RegisterAsync("rate");
        for (var i = 0; i < 6; i++)
        {
            QueueValidEnvelope();
        }
        for (var i = 0; i < 5; i++)
        {
            (await PostGenerateAsync(token, BuildRequest())).EnsureSuccessStatusCode();
        }

        var resp = await PostGenerateAsync(token, BuildRequest());

        resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Generate_AfterSuccess_GetPreferencesReflectsAiFilled()
    {
        var token = await RegisterAsync("after-success");
        QueueValidEnvelope();

        (await PostGenerateAsync(token, BuildRequest())).EnsureSuccessStatusCode();

        var prefsResp = await SendGetAsync(token, "/api/v1/preferences");
        prefsResp.EnsureSuccessStatusCode();
        var prefs = await prefsResp.Content.ReadFromJsonAsync<PreferencesDto>(JsonOptions);
        prefs!.MusicGenres.Should().Contain(Core.Enums.MusicGenre.Rock);
        prefs.Cuisines.Should().Contain(Core.Enums.Cuisine.Italian);
    }

    [Fact]
    public async Task Generate_HappyPath_PersistsTipColumnSeparately()
    {
        var token = await RegisterAsync("tip-column");
        QueueValidEnvelope("Have fun and stay hydrated, friend.");
        (await PostGenerateAsync(token, BuildRequest())).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        var plan = await db.Set<Plan>().OrderByDescending(p => p.GeneratedUtc).FirstAsync();
        plan.Tip.Should().Be("Have fun and stay hydrated, friend.");
        plan.ContentJson.Should().NotContain("\"tip\"");
    }

    private async Task<HttpResponseMessage> SendGetAsync(string token, string path)
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, path);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(msg);
    }
}
