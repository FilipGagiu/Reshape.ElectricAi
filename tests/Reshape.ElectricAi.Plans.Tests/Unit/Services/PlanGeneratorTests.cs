using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos.Plans;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Configuration;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Services;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Reshape.ElectricAi.Plans.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services;

[Collection(PostgresCollection.Name)]
public sealed class PlanGeneratorTests(PostgresFixture postgres) : IAsyncLifetime
{
    private PlansDbContext _db = null!;
    private FakeOpenAiClient _fakeOpenAi = null!;
    private InMemorySlidingWindowRateLimiter _limiter = null!;

    public async Task InitializeAsync()
    {
        var dbOptions = new DbContextOptionsBuilder<PlansDbContext>()
            .UseNpgsql(postgres.ConnectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "plans"))
            .Options;
        _db = new PlansDbContext(dbOptions);
        await _db.Database.MigrateAsync();
        _fakeOpenAi = new FakeOpenAiClient();
        _limiter = new InMemorySlidingWindowRateLimiter(NullLogger<InMemorySlidingWindowRateLimiter>.Instance);
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    private async Task<User> SeedUserAsync()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = $"u{id:N}@example.com",
            PasswordHash = "x",
            PasswordSalt = new byte[16],
            Role = UserRole.User,
            CreatedUtc = DateTime.UtcNow
        };
        _db.Set<User>().Add(user);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return user;
    }

    private PlanGenerator BuildSut() =>
        new(
            _fakeOpenAi,
            new PlansRepository<User>(_db),
            new PlansRepository<UserPreferences>(_db),
            new PlansRepository<Plan>(_db),
            _limiter,
            _db,
            Options.Create(new PlanGenerationOptions()),
            NullLogger<PlanGenerator>.Instance);

    private static PlanGenerationRequest BuildRequest() =>
        new(
            Answers: new[]
            {
                new WizardAnswer("vibe", "What's your vibe?", "chill")
            },
            FreeText: "first time at EC");

    private static object BuildEnvelope(
        string tip = "Bring sunscreen and good shoes.",
        string artist = "Justin Timberlake",
        string[]? artistsOverride = null,
        bool emptyDays = false,
        bool nullFood = false,
        bool zeroBudget = false)
    {
        var days = emptyDays ? Array.Empty<object>() : new object[]
        {
            new
            {
                date = "2025-07-17",
                transport = new
                {
                    outbound = new { mode = "EcBus", from = "Iulius Mall", departLocal = "17:55", note = (string?)null },
                    @return = new { mode = "EcBus", from = (string?)null, departLocal = (string?)null, note = "Non-stop" }
                },
                concerts = new[] { new { stage = "Main Stage", artist, startLocal = "22:30", endLocal = "23:45" } },
                activities = new[] { new { name = "Castle Market", note = "Explore" } },
                weatherNotes = new[] { "Bring poncho." }
            }
        };

        return new
        {
            preferences = new
            {
                ticketType = "Standard",
                accommodation = "Glamping",
                transport = "EcBus",
                ageGroup = "Adult25To34",
                musicGenres = new[] { "Rock" },
                foodRestrictions = new[] { "Vegan" },
                activities = new[] { "Relax" },
                artists = artistsOverride ?? new[] { artist },
                cuisines = new[] { "Italian" }
            },
            plan = new
            {
                scope = "individual",
                ticketType = "Standard",
                days,
                food = nullFood ? null : (object?)Array.Empty<object>(),
                budget = zeroBudget
                    ? new { ticket = 0, transport = 0, accommodation = 0, food = 0, drinks = 0, chaosFund = 0, total = 0, currency = "RON-cents" }
                    : new { ticket = 12000, transport = 800, accommodation = 5000, food = 2500, drinks = 1500, chaosFund = 1500, total = 23300, currency = "RON-cents" }
            },
            tip
        };
    }

    [Fact]
    public async Task GenerateAsync_ValidRequest_UpsertsPreferences()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope());

        await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var prefs = await _db.Set<UserPreferences>()
            .Include(p => p.Genres)
            .Include(p => p.Cuisines)
            .FirstAsync(p => p.UserId == user.Id);
        prefs.Genres.Select(g => g.Genre).Should().Contain(MusicGenre.Rock);
        prefs.Accommodation.Should().Be(Accommodation.Glamping);
        prefs.Cuisines.Select(c => c.Cuisine).Should().Contain(Cuisine.Italian);
    }

    [Fact]
    public async Task GenerateAsync_ValidRequest_InsertsPlanRowWithTip()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope(tip: "Pack layers for the evening chill."));

        var result = await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var planRow = await _db.Set<Plan>().FirstAsync(p => p.OwnerUserId == user.Id);
        planRow.OwnerUserId.Should().Be(user.Id);
        planRow.Tip.Should().Be("Pack layers for the evening chill.");
        planRow.ContentJson.Should().NotBeNullOrEmpty();
        result.Tip.Should().Be("Pack layers for the evening chill.");
        result.Plan.Id.Should().Be(planRow.Id);
    }

    private static readonly string[] DuplicateArtists = ["Foo", "foo", "Foo"];

    [Fact]
    public async Task GenerateAsync_DuplicateArtistsInEnvelope_DedupedOnPersist()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope(artistsOverride: DuplicateArtists));

        await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var prefs = await _db.Set<UserPreferences>().Include(p => p.Artists).FirstAsync(p => p.UserId == user.Id);
        prefs.Artists.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateAsync_ExistingPrefs_ReplacesListContents()
    {
        var user = await SeedUserAsync();
        _db.Set<UserPreferences>().Add(new UserPreferences
        {
            UserId = user.Id,
            UpdatedUtc = DateTime.UtcNow,
            Genres = { new UserPreferenceGenre { UserId = user.Id, Genre = MusicGenre.Techno } }
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        _fakeOpenAi.WithEnvelope(BuildEnvelope());
        await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var prefs = await _db.Set<UserPreferences>().Include(p => p.Genres).FirstAsync(p => p.UserId == user.Id);
        prefs.Genres.Select(g => g.Genre).Should().ContainSingle().Which.Should().Be(MusicGenre.Rock);
    }

    [Fact]
    public async Task GenerateAsync_TipContainsEmDash_Sanitizes()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope(tip: "Be ready — bring boots and snacks."));

        var result = await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        result.Tip.Should().NotContain("—");
        result.Tip.Should().Contain("Be ready - bring boots and snacks.");
    }

    [Fact]
    public async Task GenerateAsync_TipContainsEnDash_Sanitizes()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope(tip: "Plan for 5 – 7 hours of fun."));

        var result = await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        result.Tip.Should().NotContain("–");
        result.Tip.Should().Contain("Plan for 5 - 7 hours of fun.");
    }

    [Fact]
    public async Task GenerateAsync_LlmThrowsSchemaException_TerminatesImmediately()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithException(new LlmSchemaException("days empty"));

        var act = () => BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<LlmSchemaException>();
        _fakeOpenAi.CallCount.Should().Be(1);
        (await _db.Set<Plan>().CountAsync(p => p.OwnerUserId == user.Id)).Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_EnvelopeMissingFood_ThrowsLlmSchemaException()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope(nullFood: true));

        var act = () => BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<LlmSchemaException>();
        ex.Which.MissingOrInvalidField.Should().Be("plan.food");
        (await _db.Set<Plan>().CountAsync(p => p.OwnerUserId == user.Id)).Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_EnvelopeBudgetZero_ThrowsLlmSchemaException()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope(zeroBudget: true));

        var act = () => BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<LlmSchemaException>();
        ex.Which.MissingOrInvalidField.Should().Be("plan.budget");
    }

    [Fact]
    public async Task GenerateAsync_LlmThrowsRepeatedly_BubblesAndPersistsNothing()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithException(new LlmException("llm-unavailable", "fail-1"));

        var act = () => BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<LlmException>();
        (await _db.Set<Plan>().CountAsync(p => p.OwnerUserId == user.Id)).Should().Be(0);
        (await _db.Set<UserPreferences>().CountAsync(p => p.UserId == user.Id)).Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_EnvelopeMissingDays_ThrowsLlmSchemaException()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope(emptyDays: true));

        var act = () => BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<LlmSchemaException>();
        (await _db.Set<Plan>().CountAsync(p => p.OwnerUserId == user.Id)).Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_UserNotFound_ThrowsNotFound()
    {
        _fakeOpenAi.WithEnvelope(BuildEnvelope());

        var act = () => BuildSut().GenerateAsync(Guid.NewGuid(), BuildRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GenerateAsync_RateLimitExceeded_ThrowsTooManyRequests()
    {
        var user = await SeedUserAsync();
        for (var i = 0; i < 6; i++)
        {
            _fakeOpenAi.WithEnvelope(BuildEnvelope());
        }
        var sut = BuildSut();
        for (var i = 0; i < 5; i++)
        {
            await sut.GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);
        }

        var act = () => sut.GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<TooManyRequestsException>();
        ex.Which.RetryAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateAsync_CancellationRequested_Propagates()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => BuildSut().GenerateAsync(user.Id, BuildRequest(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        (await _db.Set<Plan>().CountAsync(p => p.OwnerUserId == user.Id)).Should().Be(0);
    }

    [Fact]
    public async Task GenerateAsync_PromptsFakeWithExpectedShape()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope());

        await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        _fakeOpenAi.Calls.Should().HaveCount(1);
        var call = _fakeOpenAi.Calls[0];
        call.SystemPrompt.Should().Contain("Electric Castle");
        call.UserPrompt.Should().Contain("What's your vibe?");
        call.UserPrompt.Should().Contain("chill");
        call.UserPrompt.Should().Contain("first time at EC");
    }

    [Fact]
    public async Task GenerateAsync_PlanInsertFails_PrefsNotPersisted()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope());
        var failingPlanRepo = new ThrowingPlanRepository(new PlansRepository<Plan>(_db));
        var sut = new PlanGenerator(
            _fakeOpenAi,
            new PlansRepository<User>(_db),
            new PlansRepository<UserPreferences>(_db),
            failingPlanRepo,
            _limiter,
            _db,
            Options.Create(new PlanGenerationOptions()),
            NullLogger<PlanGenerator>.Instance);

        var act = () => sut.GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("simulated plan-insert failure");
        (await _db.Set<Plan>().CountAsync(p => p.OwnerUserId == user.Id)).Should().Be(0);
        (await _db.Set<UserPreferences>().CountAsync(p => p.UserId == user.Id)).Should().Be(0);
    }

    private sealed class ThrowingPlanRepository(IRepository<Plan> inner) : IRepository<Plan>
    {
        public ValueTask<Plan?> GetByIdAsync(object id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);
        public Task<Plan?> FirstOrDefaultAsync(ISpecification<Plan> spec, CancellationToken ct = default) => inner.FirstOrDefaultAsync(spec, ct);
        public Task<IReadOnlyList<Plan>> ListAsync(ISpecification<Plan> spec, CancellationToken ct = default) => inner.ListAsync(spec, ct);
        public Task<IReadOnlyList<Plan>> ListAsync(CancellationToken ct = default) => inner.ListAsync(ct);
        public Task<int> CountAsync(ISpecification<Plan> spec, CancellationToken ct = default) => inner.CountAsync(spec, ct);
        public Task<bool> AnyAsync(ISpecification<Plan> spec, CancellationToken ct = default) => inner.AnyAsync(spec, ct);
        public Task AddAsync(Plan entity, CancellationToken ct = default) => throw new InvalidOperationException("simulated plan-insert failure");
        public void Update(Plan entity) => inner.Update(entity);
        public void Remove(Plan entity) => inner.Remove(entity);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => inner.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task GenerateAsync_HonorsPlanGenerationMaxTokensAndTemperature()
    {
        var user = await SeedUserAsync();
        _fakeOpenAi.WithEnvelope(BuildEnvelope());

        await BuildSut().GenerateAsync(user.Id, BuildRequest(), CancellationToken.None);

        var call = _fakeOpenAi.Calls[0];
        call.MaxCompletionTokens.Should().Be(2048);
        call.Temperature.Should().BeApproximately(0.7, 0.001);
    }
}
