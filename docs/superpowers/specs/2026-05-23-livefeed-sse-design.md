# LiveFeed SSE Infrastructure — Design Spec (rev 3, post-master-merge)

**Date:** 2026-05-23
**Branch:** `feature/live-feed`
**Owner:** Dev 3 (LiveFeed lib)
**Status:** rev 3 — rebased on master after auth + FluentValidation 12 + persistence-pattern landings
**Revision history:**
- rev 1 — initial design
- rev 2 — Code Reviewer pass: `IFeedService` moved to Core, broadcast-after-commit, `SemaphoreSlim`/`PeriodicTimer` lifecycle hardened
- rev 2.1 — tie-breaker reconciliation (Postgres uuid native order)
- **rev 3 — master-merge alignment:** JWT live, FluentValidation 12.1.1 + hand-rolled global filter, Repository+Specification pattern, new `Infrastructure` project, no header-based identity, integration test coverage expanded per user ask

## 0. Non-negotiable phase list (CLAUDE.md)

Every plan derived from this spec MUST restate verbatim:

> 1. Invoke task-specific superpowers skill(s)
> 2. Enter plan mode
> 3. Inventory / explore
> 4. Design — propose specific custom agents for review/exploration/design feedback (NOT implementation)
> 5. Write the plan to `.claude/plans/<slug>.md`
> 6. `ExitPlanMode` — single approval gate
> 7. Execute — main loop edits files; re-read CODE.md before each code edit
> 8. Verify — build + tests + visible evidence
> 9. Promote learnings to memory
> 10. Delete the plan file

## 1. Problem statement

Organizers push live updates to attendees. Delays, weather alerts, stage moves reach the **right** subset of users immediately. Each user has preferences (favorite artists, genres). General announcements reach everyone; personalized announcements reach matching users.

This spec covers SSE infrastructure + CRUD + targeting predicate. Vector indexing, group targeting, location targeting out of scope.

## 2. Scope

### In scope
- New project: **`Reshape.ElectricAi.Infrastructure`** — promotes `EfRepository<TContext, T>` + `SpecificationEvaluator` out of Plans. Plans + LiveFeed both reference it. Triggered by PROJECT.md follow-up #4.
- `Reshape.ElectricAi.LiveFeed`: `FeedEntry` entity + `FeedDbContext` + `feed` schema + migration + service impl + broadcaster + targeting predicate + DI module + validators + `FeedRepository<T>` closing class.
- `Reshape.ElectricAi.Core`: `IFeedService` + `IFeedBroadcaster` + `IUserPrefsProvider` + `UserFeedPrefs` DTO + LiveFeed DTOs + `FeedEventKind` enum. **No new exception types** — reuse existing `Core.Domain.Exceptions.NotFoundException` etc.
- `Reshape.ElectricAi.Plans`: minor migration only — `PlansRepository<T>` becomes `: EfRepository<PlansDbContext, T>` from `Infrastructure` (namespace change, otherwise unchanged); `PlansModule` swap of using statement. **No business-logic changes to Plans.**
- `Reshape.ElectricAi.Presentation`: `FeedController` + SSE writer + `LiveFeedModule` wiring in `Program.cs`. **No auth wiring changes** — JWT bearer already live in master.
- `tests/Reshape.ElectricAi.LiveFeed.Tests`: unit + integration (Testcontainers Postgres + `WebApplicationFactory<Program>`). Mirrors `Plans.Tests` layout.

### Out of scope
- **`SseQueryStringTokenMiddleware`** (CODE.md `## Auth` line 184). User said "don't bother securing the endpoint yet" → SSE stream stays anonymous (no `[Authorize]`). Middleware deferred to future plan when EventSource auth becomes a real requirement.
- Vector indexing (Dev 2, VectorDb lib).
- Location targeting.
- `feed_deliveries` table.
- Group-level feed targeting.
- Rate limiting.
- Horizontal scaling.

## 3. Architecture

```
Presentation                               LiveFeed                            Infrastructure
─────────────                              ─────────────────                   ──────────────
FeedController ─── IFeedService (Core) ───► FeedService ─► IRepository<T> ─► EfRepository<FeedDbContext, T>
                                  └───────► IFeedBroadcaster                  ▲
                                            └► FeedBroadcaster (singleton)    │
                                                                              │ closing class
SSE endpoint ◄── IFeedBroadcaster.SubscribeUserToStreamAsync(...)             FeedRepository<T> (in LiveFeed)
                                                                              :
                                                                              :
                                                                            EfRepository<TContext, T>
                                                                            SpecificationEvaluator
```

- LiveFeed impls: `FeedService`, `FeedBroadcaster`, `FeedEntry` entity, `FeedDbContext`, `FeedRepository<T>`, `LiveFeedModule`.
- Core: `IFeedService`, `IFeedBroadcaster`, `IUserPrefsProvider`, `UserFeedPrefs`, `FeedEntryDto`, `FeedEventEnvelope`, `FeedEventKind`, `PublishFeedEntryCommand`, `UpdateFeedEntryCommand`.
- Infrastructure (new): `EfRepository<TContext, T>`, `SpecificationEvaluator`. Moved from `Plans/Persistence/`.
- Presentation: `FeedController` + SSE writer. Identity from JWT claim `JwtRegisteredClaimNames.Sub`.
- Singleton broadcaster holds `ConcurrentDictionary<Guid (subId), FeedSubscription>` + captures `IServiceScopeFactory` to safely resolve scoped `IFeedService` during replay-on-connect.

## 4. Domain model

### 4.1. Entities

```csharp
public class FeedEntry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public Category PrimaryCategory { get; set; }
    public bool IsGeneral { get; set; }
    public Guid PublishedByUserId { get; set; }
    public DateTime PublishedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public DateTime? DeletedUtc { get; set; }

    public List<FeedEntryArtist> TargetArtists { get; set; } = [];
    public List<FeedEntryGenre> TargetGenres { get; set; } = [];
}
public class FeedEntryArtist { public Guid FeedEntryId; public string ArtistName = ""; public FeedEntry? FeedEntry { get; set; } }
public class FeedEntryGenre  { public Guid FeedEntryId; public MusicGenre Genre; public FeedEntry? FeedEntry { get; set; } }
```

**No location anywhere.**

### 4.2. DTOs

Core (cross-lib):
- `FeedEntryDto(Guid Id, string Title, string Body, Category PrimaryCategory, bool IsGeneral, IReadOnlyList<string> TargetArtists, IReadOnlyList<MusicGenre> TargetGenres, DateTime PublishedUtc, DateTime? UpdatedUtc)`
- `FeedEventEnvelope(FeedEventKind Kind, string EventId, FeedEntryDto Entry)`
- `UserFeedPrefs(IReadOnlySet<string> Artists, IReadOnlySet<MusicGenre> Genres)`
- `PublishFeedEntryCommand(...)`, `UpdateFeedEntryCommand(...)`
- `enum FeedEventKind { Created, Updated, Deleted }` in `Core/Enums/`

LiveFeed (request types, controller-facing):
- `PublishFeedEntryRequest(string Title, string Body, Category PrimaryCategory, bool IsGeneral, IReadOnlyList<string> TargetArtists, IReadOnlyList<MusicGenre> TargetGenres)`
- `UpdateFeedEntryRequest(...)` same shape

### 4.3. EventId format

`{PublishedUtc:O}-{Guid:D}`. ISO-8601 round-trip + Guid hex. Lexicographic on timestamp prefix = chronological.

**Tie-breaker (rev 2.1 — kept):** same-millisecond entries ordered by **Postgres `uuid` native byte-order comparison** (matches `e.Id > cursorEntryId` SQL predicate semantics). NOT `Guid.ToString()` lexicographic. Replay correctness holds: no skipped/duplicate events. Test asserts agreement with Postgres ordering.

### 4.4. Targeting predicate (pure)

```csharp
public static class FeedTargeting
{
    public static bool EntryMatchesUserPrefs(FeedEntryDto entry, UserFeedPrefs prefs) =>
        entry.IsGeneral
     || entry.TargetArtists.Any(prefs.Artists.Contains)
     || entry.TargetGenres.Any(prefs.Genres.Contains);
}
```

Artist match exact-string, case-sensitive. Documented + unit-tested.

## 5. Persistence

### 5.1. `Reshape.ElectricAi.Infrastructure` (new project, PROJECT.md follow-up #4)

Moved from `Plans/Persistence/`:
- `EfRepository<TContext, T>` — open generic, EF-aware impl of `IRepository<T>`. Namespace: `Reshape.ElectricAi.Infrastructure.Persistence`.
- `SpecificationEvaluator` — static, applies `ISpecification<T>` to `IQueryable<T>`. Namespace: `Reshape.ElectricAi.Infrastructure.Persistence`.

Project refs: `Reshape.ElectricAi.Core` only. Package refs: `Microsoft.EntityFrameworkCore` (`10.0.*`).

Dependency direction update (CODE.md will need a docs-edit commit):
```
Presentation  →  Plans, VectorDb, LiveFeed, AiChat, Core, Infrastructure
Plans         →  Core, Infrastructure
LiveFeed      →  Core, Infrastructure, VectorDb
AiChat        →  Core, VectorDb
VectorDb      →  Core
Infrastructure → Core
Core          → (nothing)
```

`Plans/Persistence/PlansRepository.cs` updates to:
```csharp
using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.Plans.Persistence;

public sealed class PlansRepository<T>(PlansDbContext context)
    : EfRepository<PlansDbContext, T>(context)
    where T : class;
```

`Plans/Persistence/EfRepository.cs` and `Plans/Persistence/SpecificationEvaluator.cs` are **deleted** from Plans (now live in Infrastructure).

### 5.2. `FeedDbContext` (LiveFeed)

- `HasDefaultSchema("feed")`
- `MigrationsHistoryTable("__EFMigrationsHistory", "feed")`
- DbSets: `FeedEntries`, `FeedEntryArtists`, `FeedEntryGenres`
- Configurations in `Persistence/Configurations/` via `IEntityTypeConfiguration<T>`
- Indexes: `(PublishedUtc DESC)` + partial `(DeletedUtc, PublishedUtc DESC) WHERE DeletedUtc IS NULL` (via `HasFilter` or `migrationBuilder.Sql` fallback)
- `FeedEntryArtists` PK `(FeedEntryId, ArtistName)`, cascade
- `FeedEntryGenres` PK `(FeedEntryId, Genre)`, cascade
- Design-time factory `FeedDbContextFactory` mirroring `PlansDbContextFactory`

### 5.3. `FeedRepository<T>` (LiveFeed, closing class)

```csharp
using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Persistence;

public sealed class FeedRepository<T>(FeedDbContext context)
    : EfRepository<FeedDbContext, T>(context)
    where T : class;
```

DI registration in `LiveFeedModule`: `services.AddScoped(typeof(IRepository<>), typeof(FeedRepository<>))`.

### 5.4. Specifications (LiveFeed/Persistence/Specifications/)

- `RecentFeedEntriesSpec(Category? categoryFilter, int take)` — `WHERE DeletedUtc IS NULL [AND PrimaryCategory == cat]`, `ORDER BY PublishedUtc DESC, Id DESC`, `Take(take)`, `AsNoTracking`, `AsSplitQuery`, includes `TargetArtists` + `TargetGenres`.
- `FeedEntriesSinceCursorSpec(DateTime cursorUtc, Guid cursorId, int take)` — `WHERE DeletedUtc IS NULL AND (PublishedUtc > p OR (PublishedUtc == p AND Id > g))`, `ORDER BY PublishedUtc, Id`, `Take(take)`, includes.
- `FeedEntryByIdSpec(Guid id)` — `WHERE Id == id`, includes; used by Update.

### 5.5. Migration

- Single migration `feed_initial` via `dotnet ef migrations add` on `FeedDbContext`.
- Inspect partial-index DDL; `HasFilter("\"DeletedUtc\" IS NULL")` preferred, `migrationBuilder.Sql` fallback.

### 5.6. Startup migration

`Program.cs` already does `await db.Database.MigrateAsync()` for `PlansDbContext` in Development. Extend the same block for `FeedDbContext`. Pattern matches existing line.

## 6. Service layer

### 6.1. `IFeedService` (Core)

```csharp
namespace Reshape.ElectricAi.Core.Services;

public interface IFeedService
{
    Task<FeedEntryDto> PublishEntryAsync(Guid organizerId, PublishFeedEntryCommand command, CancellationToken ct);
    Task<FeedEntryDto> UpdateEntryByIdAsync(Guid entryId, UpdateFeedEntryCommand command, CancellationToken ct);
    Task SoftDeleteEntryByIdAsync(Guid entryId, CancellationToken ct);
    Task<FeedEntryDto?> GetEntryByIdAsync(Guid entryId, CancellationToken ct);
    Task<IReadOnlyList<FeedEntryDto>> ListRecentEntriesMatchingPrefsAsync(UserFeedPrefs prefs, Category? categoryFilter, int take, CancellationToken ct);
    Task<IReadOnlyList<FeedEntryDto>> ListEntriesSinceEventIdMatchingPrefsAsync(string lastEventId, UserFeedPrefs prefs, int take, CancellationToken ct);
}
```

### 6.2. `FeedService` (LiveFeed)

**Uses `IRepository<FeedEntry>` injection, not `FeedDbContext` directly** — matches Plans pattern.

- `PublishEntryAsync`: build entity → `repo.AddAsync` → `repo.SaveChangesAsync` → map to DTO → `broadcaster.BroadcastEventToMatchingSubscribers(Created, dto)`. **Broadcast AFTER `SaveChangesAsync` returns** (rev 2 fix #4 preserved).
- `UpdateEntryByIdAsync`: `repo.FirstOrDefaultAsync(new FeedEntryByIdSpec(id))` → throw `NotFoundException("feed-entry-not-found", ...)` if null or `DeletedUtc != null` → mutate → `repo.SaveChangesAsync` → broadcast `Updated`.
- `SoftDeleteEntryByIdAsync`: idempotent — if missing/already-deleted, no-op + no broadcast. Else set `DeletedUtc = UtcNow`, save, broadcast `Deleted`.
- `GetEntryByIdAsync`: spec lookup; null if soft-deleted.
- `ListRecentEntriesMatchingPrefsAsync`: `repo.ListAsync(new RecentFeedEntriesSpec(category, take))` → map to DTO → filter via `FeedTargeting`.
- `ListEntriesSinceEventIdMatchingPrefsAsync`: parse cursor via `FeedEventId.TryParseEntryIdFromEventId`. Parse fail → fall through to `ListRecentEntriesMatchingPrefsAsync`. Parse success → `repo.ListAsync(new FeedEntriesSinceCursorSpec(cursorUtc, cursorId, take))` → map → filter.

### 6.3. `IUserPrefsProvider` (Core)

```csharp
namespace Reshape.ElectricAi.Core.Services;
public interface IUserPrefsProvider
{
    Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct);
}
```

### 6.4. `EmptyUserPrefsProvider` (LiveFeed) — default until Plans dev wires real

Returns cached static empty `UserFeedPrefs`. Registered via `services.TryAddScoped<IUserPrefsProvider, EmptyUserPrefsProvider>()` — Plans dev later overrides by registering real provider before `AddLiveFeedModule()`. Effect today: every subscriber sees `IsGeneral` entries only.

## 7. Broadcaster + SSE hub

### 7.1. `IFeedBroadcaster` (Core)

```csharp
namespace Reshape.ElectricAi.Core.Services;
public interface IFeedBroadcaster
{
    IAsyncEnumerable<FeedEventEnvelope> SubscribeUserToStreamAsync(
        Guid userId, UserFeedPrefs prefs, string? lastEventId, CancellationToken ct);

    void BroadcastEventToMatchingSubscribers(FeedEventKind kind, FeedEntryDto entry);
}
```

### 7.2. `FeedBroadcaster` (LiveFeed, singleton)

Singleton. `IServiceScopeFactory` injection for resolving scoped `IFeedService` during replay-on-connect (rev 2 fix #3 preserved). Per-subscription `Channel<FeedEventEnvelope>` capacity 100 `DropOldest` + `SingleReader = true`. `BroadcastEventToMatchingSubscribers` iterates subs, runs `FeedTargeting.EntryMatchesUserPrefs`, `TryWrite`. `SubscribeUserToStreamAsync` yields replay batch then loops `Channel.Reader.ReadAllAsync`. `try/finally` removes subscription + completes writer on cancellation.

(Same code as rev 2.1 §7.2 — unchanged.)

## 8. Identity — JWT claims (no placeholder)

JWT live in master. Controllers read claims directly per CODE.md `## Auth` line 181.

```csharp
private static Guid GetCurrentUserId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
           ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(sub, out var id)
        ? id
        : throw new UnauthorizedException("missing-sub-claim", "Subject claim missing or invalid");
}
```

Helper lives as `private static` in `FeedController`. Tests bypass JWT via `AuthApiFactory`-style fixture (test-only `JwtBearerOptions` override OR token generation via real `ITokenService`).

**No more `ICurrentUserAccessor` / `HeaderCurrentUserAccessor` / `X-User-Id` header.** Whole layer removed.

## 9. Controller + SSE writer

### 9.1. `FeedController`

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class FeedController(
    IFeedService feed,
    IFeedBroadcaster broadcaster,
    IUserPrefsProvider prefsProvider) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public Task<ActionResult<IReadOnlyList<FeedEntryDto>>> ListRecentEntriesForCurrentUserAsync(
        [FromQuery] Category? category, CancellationToken ct);

    [HttpPost]
    [Authorize(Roles = "Organizer")]
    public Task<ActionResult<FeedEntryDto>> PublishEntryAsOrganizerAsync(
        [FromBody] PublishFeedEntryRequest request, CancellationToken ct);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    public Task<ActionResult<FeedEntryDto>> UpdateEntryByIdAsOrganizerAsync(
        [FromRoute] Guid id, [FromBody] UpdateFeedEntryRequest request, CancellationToken ct);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    public Task<IActionResult> SoftDeleteEntryByIdAsOrganizerAsync(
        [FromRoute] Guid id, CancellationToken ct);

    [HttpGet("stream")]
    [AllowAnonymous]   // intentional — SSE auth deferred per user direction; no SseQueryStringTokenMiddleware
    [Produces("text/event-stream")]
    public Task StreamFeedToCurrentUserAsync([FromQuery] Guid? userId, CancellationToken ct);
}
```

- CRUD endpoints honor CODE.md `## Auth`: `[Authorize]` reads, `[Authorize(Roles = "Organizer")]` writes.
- SSE stream: `[AllowAnonymous]` per user direction. Accepts `?userId={guid}` query as identity placeholder for the stream — without claims, broadcaster needs a userId; SSE auth is the only thing deferred. Anonymous if `userId` omitted (then `UserFeedPrefs` empty → IsGeneral only).
- `[AllowAnonymous]` written out, not aliased (CODE.md style rule).

### 9.2. `StreamFeedToCurrentUserAsync` body

```csharp
public async Task StreamFeedToCurrentUserAsync([FromQuery] Guid? userId, CancellationToken ct)
{
    WriteSseResponseHeaders();

    var effectiveUserId = userId ?? Guid.Empty;
    var prefs = effectiveUserId == Guid.Empty
        ? new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>())
        : await prefsProvider.GetPrefsByUserIdAsync(effectiveUserId, ct);

    var lastEventId = Request.Headers["Last-Event-ID"].FirstOrDefault();

    using var writeLock = new SemaphoreSlim(1, 1);
    var heartbeatTask = RunHeartbeatLoopAsync(writeLock, ct);
    try
    {
        await foreach (var env in broadcaster.SubscribeUserToStreamAsync(effectiveUserId, prefs, lastEventId, ct))
        {
            await WriteSseEventFrameAsync(env, writeLock, ct);
        }
    }
    finally
    {
        try { await heartbeatTask; }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
    }
}
```

Helpers `WriteSseResponseHeaders`, `WriteSseEventFrameAsync`, `RunHeartbeatLoopAsync`, `_jsonOpts` — unchanged from rev 2.1 §9.3.

### 9.3. CRUD action bodies — JWT claim reads

```csharp
[HttpPost]
[Authorize(Roles = "Organizer")]
public async Task<ActionResult<FeedEntryDto>> PublishEntryAsOrganizerAsync(
    [FromBody] PublishFeedEntryRequest request, CancellationToken ct)
{
    var organizerId = GetCurrentUserId(User);
    var dto = await feed.PublishEntryAsync(organizerId, request.ToCommand(), ct);
    return CreatedAtAction(nameof(ListRecentEntriesForCurrentUserAsync), new { }, dto);
}
```

`GetCurrentUserId(User)` helper from §8. No more `currentUser.GetRequiredUserId()`.

## 10. Validation (FluentValidation 12.1.1)

### 10.1. Validators (LiveFeed)

`PublishFeedEntryRequestValidator` + `UpdateFeedEntryRequestValidator` — rules per rev 2.1 §10.1.

### 10.2. Registration (LiveFeed/PlansModule pattern)

`LiveFeedModule` uses **same reflection scan** as `PlansModule.RegisterValidators` (CODE.md line 23 — `FluentValidation.DependencyInjectionExtensions` lives only on Presentation; feature libs scan). Extract into shared helper or inline-copy.

### 10.3. Filter

Already-global `FluentValidationFilter` (Presentation/Filters/) auto-runs on every action. No additional wiring needed.

## 11. Module + DI

### 11.1. `LiveFeedModule`

```csharp
public static class LiveFeedModule
{
    public static IServiceCollection AddLiveFeedModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<FeedDbContext>(opts =>
            opts.UseNpgsql(connectionString, n =>
                n.MigrationsHistoryTable("__EFMigrationsHistory", "feed")));

        services.AddScoped(typeof(IRepository<>), typeof(FeedRepository<>));

        services.AddScoped<IFeedService, FeedService>();
        services.AddSingleton<IFeedBroadcaster, FeedBroadcaster>();
        services.TryAddScoped<IUserPrefsProvider, EmptyUserPrefsProvider>();

        RegisterValidators(services);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        var validatorInterface = typeof(IValidator<>);
        var registrations = typeof(LiveFeedModule).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsClass: true })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var r in registrations)
            services.TryAddScoped(r.Service, r.Implementation);
    }
}
```

### 11.2. `Program.cs` additions

Single line added near the existing `AddPlansModule` call:
```csharp
builder.Services.AddLiveFeedModule(builder.Configuration);
```

Add `FeedDbContext` to the Development startup migration block:
```csharp
using (var scope = app.Services.CreateScope())
{
    var plansDb = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
    await plansDb.Database.MigrateAsync();

    var feedDb = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
    await feedDb.Database.MigrateAsync();
}
```

**No auth wiring, no middleware, no filter changes.** Everything else already configured by master.

## 12. Exception envelope

Already wired via `ExceptionHandlerMiddleware` (`Reshape.ElectricAi.Presentation/Middleware/`). Uses `Reshape.ElectricAi.Core.Domain.Exceptions.*`. Maps:
- `UnauthorizedException` → 401
- `ForbiddenException` → 403
- `NotFoundException` → 404
- `ConflictException` → 409
- `PreconditionFailedException` → 422
- default `DomainException` → 400
- unhandled `Exception` → 500 `internal-error`

No `ValidationException` type exists in master — validation failures come from `FluentValidationFilter` writing 400 + envelope directly.

If LiveFeed needs to throw something semantic that doesn't match an existing type, **prefer reusing existing types** with appropriate `code`. E.g. `throw new NotFoundException("feed-entry-not-found", $"Feed entry {id} not found")`.

## 13. Tests — full wire-level coverage

Project: `tests/Reshape.ElectricAi.LiveFeed.Tests` — mirrors `Plans.Tests` layout exactly.

### 13.1. csproj (mirrors Plans.Tests pinned versions)

| Package | Version (matches Plans.Tests) |
|---|---|
| `coverlet.collector` | `6.0.4` |
| `FluentAssertions` | `6.12.2` (matches Plans.Tests — last permissive license, NOT 7.*) |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.8` |
| `Microsoft.NET.Test.Sdk` | `17.14.1` |
| `Testcontainers.PostgreSql` | `4.12.0` |
| `xunit` | `2.9.3` |
| `xunit.runner.visualstudio` | `3.1.4` |

Project refs: `Core`, `LiveFeed`, `Presentation`. Add `Plans` reference if test helpers need `ITokenService` to mint real JWTs.

### 13.2. Test fixtures

Mirror `Plans.Tests/Integration/Fixtures/`:
- `PostgresFixture.cs` — Testcontainers PostgreSQL container.
- `PostgresCollection.cs` — xUnit collection definition (shares container across test classes).
- `FeedApiFactory.cs` — `WebApplicationFactory<Program>` analogue of `AuthApiFactory`. Overrides `Postgres` connection string. Registers `FakeUserPrefsProvider`. Provides helper `CreateAuthenticatedClient(Guid userId, string[] roles)` that mints a real JWT via injected `ITokenService` from `Plans` and sets the `Authorization: Bearer <token>` header.

### 13.3. Unit tests

`Unit/FeedTargetingTests.cs`:
- `EntryMatchesUserPrefs_WhenIsGeneralTrue_ReturnsTrueForAnyUser`
- `EntryMatchesUserPrefs_WhenArtistOverlapsUserPrefs_ReturnsTrue`
- `EntryMatchesUserPrefs_WhenGenreOverlapsUserPrefs_ReturnsTrue`
- `EntryMatchesUserPrefs_WhenNoOverlapAndNotGeneral_ReturnsFalse`
- `EntryMatchesUserPrefs_WhenArtistMatchIsCaseSensitive_DocumentsBehavior`

`Unit/FeedEventIdTests.cs`:
- `FormatForEntry_ProducesIso8601WithGuidSuffix`
- `TryParseEntryIdFromEventId_RoundTripsCleanly`
- `TryParseEntryIdFromEventId_WhenInputIsMalformed_ReturnsFalse` (theory with garbage inputs)

`Unit/FeedBroadcasterTests.cs` (with `RecordingScopeFactory`):
- `BroadcastEventToMatchingSubscribers_WhenSubscriberMatches_WritesEnvelopeToChannel`
- `BroadcastEventToMatchingSubscribers_WhenSubscriberDoesNotMatch_DoesNotWrite`
- `SubscribeUserToStreamAsync_WhenChannelOverflowsCapacity_DropsOldestEnvelope`
- `SubscribeUserToStreamAsync_WhenCancellationRequested_RemovesSubscriptionFromRegistry`
- `SubscribeUserToStreamAsync_WhenLastEventIdNullAndNoHistory_YieldsZeroReplayEntries`
- `SubscribeUserToStreamAsync_OnReplay_ResolvesFreshIFeedServiceScopePerCall`

### 13.4. Integration tests — full SSE coverage (user-mandated)

`Integration/Endpoints/FeedCrudTests.cs`:
- `PublishEntryAsOrganizer_WhenAuthenticatedAsOrganizer_Returns201AndDtoMatchingInput`
- `PublishEntryAsOrganizer_WhenAuthenticatedAsUser_Returns403Envelope` (role gate)
- `PublishEntryAsOrganizer_WhenAnonymous_Returns401Envelope` (auth gate)
- `ListRecentEntries_WhenAuthenticated_ReturnsOrderedByPublishedDescending`
- `ListRecentEntries_WhenAnonymous_Returns401Envelope`
- `SoftDeleteEntryById_WhenEntryExistsAsOrganizer_RemovesFromList`
- `UpdateEntryById_WhenEntryMissing_Returns404Envelope`
- `PublishEntry_WhenNotGeneralAndNoTargeting_Returns400ValidationEnvelope`

`Integration/Endpoints/FeedSseTests.cs`:
- `StreamFeed_WhenOrganizerPublishesMatchingEntry_ClientReceivesCreatedFrame`
- `StreamFeed_WhenOrganizerPublishesUnmatchedEntry_ClientReceivesNoFrameWithinOneSecond`
- `StreamFeed_WhenIdleFor26Seconds_ClientReceivesKeepaliveComment`
- `StreamFeed_WhenLastEventIdHeaderPresent_ReplaysOnlyEntriesSinceCursor`
- `StreamFeed_WhenLastEventIdHeaderMalformed_FallsThroughToRecentBatch`
- `StreamFeed_WhenTwoUsersConnectedAndEntryTargetsOnlyOne_OnlyMatchingUserReceivesFrame`
- `StreamFeed_WhenHeartbeatAndEventInterleave_ProducesNoCorruptFrame` (high publish rate + heartbeat; assert each frame starts with `event:` or `:` and ends with `\n\n`)
- `StreamFeed_WhenClientDisconnects_HeartbeatTaskCompletesWithinShortWindow`
- `StreamFeed_WhenConnected_ResponseHeadersAreSseCompliant` (Content-Type, Cache-Control, Connection, X-Accel-Buffering)
- `StreamFeed_WhenAnonymousAndUserIdQuerySet_TargetingAppliesToThatUser` (anonymous SSE path with `?userId=`)
- `StreamFeed_WhenAnonymousAndNoUserIdQuery_OnlyReceivesGeneralEntries`

`Integration/Endpoints/FeedServiceBroadcastOrderingTests.cs` (broadcaster correctness inside DB context):
- `PublishEntry_AfterSaveChanges_BroadcastsCreatedEnvelope` (verifies broadcast happens after commit by checking entry exists in DB before envelope reaches subscriber)
- `PublishEntry_WhenSaveChangesThrows_DoesNotBroadcast` (force a unique-constraint trip — e.g. duplicate Guid via mocked `IRepository` substituting in `ConfigureTestServices`)
- `SoftDeleteEntryById_WhenAlreadyDeleted_DoesNotBroadcastAndDoesNotThrow`

`Integration/Persistence/FeedRepositorySpecificationTests.cs`:
- `ListAsync_WithRecentFeedEntriesSpec_ReturnsLatestFirstAndExcludesDeleted`
- `ListAsync_WithFeedEntriesSinceCursorSpec_ReturnsOnlyEntriesAfterCursor`
- `FirstOrDefaultAsync_WithFeedEntryByIdSpec_ReturnsEntryWithIncludedTargets`

### 13.5. Test auth strategy

Mirror `AuthApiFactory.cs` pattern in Plans.Tests. `FeedApiFactory` exposes `CreateClientForUser(Guid userId, params string[] roles)` which:
1. Resolves `ITokenService` from the host services.
2. Mints an access token with `sub=userId` + role claims.
3. Returns `HttpClient` with `Authorization: Bearer <token>` preset.

Anonymous tests use `CreateClient()` from the base factory (no token).

`FakeUserPrefsProvider` registered via `ConfigureTestServices`, replacing `EmptyUserPrefsProvider`.

## 14. Packages to install (user runs)

**Existing csprojs already pinned correctly** for Plans / Presentation / Core. New work:

| Project | Package | Version |
|---|---|---|
| `src/Reshape.ElectricAi.Infrastructure` (new) | `Microsoft.EntityFrameworkCore` | `10.0.*` |
| `src/Reshape.ElectricAi.LiveFeed` | `Microsoft.EntityFrameworkCore` | `10.0.*` (already present per current csproj) |
| `src/Reshape.ElectricAi.LiveFeed` | `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.*` (already present) |
| `src/Reshape.ElectricAi.LiveFeed` | `FluentValidation` | `12.1.1` |
| `src/Reshape.ElectricAi.LiveFeed` | project ref to `Infrastructure` | — |
| `src/Reshape.ElectricAi.Plans` | project ref to `Infrastructure` | — |
| `src/Reshape.ElectricAi.Presentation` | project ref to `LiveFeed` | — (verify; likely missing) |
| `tests/Reshape.ElectricAi.LiveFeed.Tests` | mirror Plans.Tests package set (§13.1) | (versions above) |

User installs all packages (CODE.md §6a). No `FluentValidation.DependencyInjectionExtensions` on LiveFeed (CODE.md line 23 forbids).

## 15. Files added / changed

### 15.1. New (Infrastructure project)
- `src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj`
- `src/Reshape.ElectricAi.Infrastructure/Persistence/EfRepository.cs` (moved from Plans)
- `src/Reshape.ElectricAi.Infrastructure/Persistence/SpecificationEvaluator.cs` (moved from Plans)

### 15.2. Changed (Plans)
- `src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj` — add `Infrastructure` project ref
- `src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs` — **DELETED**
- `src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs` — **DELETED**
- `src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs` — update `using` to `Reshape.ElectricAi.Infrastructure.Persistence`

### 15.3. Changed (Core)
- `src/Reshape.ElectricAi.Core/Services/IFeedService.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Services/IFeedBroadcaster.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Services/IUserPrefsProvider.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Dtos/UserFeedPrefs.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Dtos/FeedEntryDto.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Dtos/FeedEventEnvelope.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Dtos/PublishFeedEntryCommand.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Dtos/UpdateFeedEntryCommand.cs` (NEW)
- `src/Reshape.ElectricAi.Core/Enums/FeedEventKind.cs` (NEW)

### 15.4. New (LiveFeed)
- `Entities/FeedEntry.cs`, `FeedEntryArtist.cs`, `FeedEntryGenre.cs`
- `Persistence/FeedDbContext.cs`, `FeedDbContextFactory.cs`, `FeedRepository.cs`
- `Persistence/Configurations/FeedEntryConfiguration.cs`, `FeedEntryArtistConfiguration.cs`, `FeedEntryGenreConfiguration.cs`
- `Persistence/Specifications/RecentFeedEntriesSpec.cs`, `FeedEntriesSinceCursorSpec.cs`, `FeedEntryByIdSpec.cs`
- `Migrations/*_feed_initial.*`
- `Dtos/PublishFeedEntryRequest.cs`, `UpdateFeedEntryRequest.cs`
- `Dtos/Mapping/FeedEntryMapping.cs`
- `Services/FeedService.cs`, `EmptyUserPrefsProvider.cs`
- `Broadcasting/FeedBroadcaster.cs`, `FeedSubscription.cs`, `FeedTargeting.cs`, `FeedEventId.cs`
- `Validators/PublishFeedEntryRequestValidator.cs`, `UpdateFeedEntryRequestValidator.cs`
- `LiveFeedModule.cs`

### 15.5. Changed (LiveFeed csproj)
- Project refs: add `Infrastructure`. Keep existing refs to `Core` + `VectorDb`.
- Packages: add `FluentValidation 12.1.1`. EF Core + Npgsql already present.

### 15.6. New (Presentation)
- `src/Reshape.ElectricAi.Presentation/Controllers/FeedController.cs`

### 15.7. Changed (Presentation)
- `Program.cs` — add `builder.Services.AddLiveFeedModule(builder.Configuration);`. Extend Development migration block for `FeedDbContext`. **No other changes.**
- `Reshape.ElectricAi.Presentation.csproj` — add project ref to `LiveFeed` if missing.

### 15.8. Changed (CODE.md + PROJECT.md)
- CODE.md `## Persistence layer` — note Infrastructure project promotion (the conditional in line 134 fires).
- PROJECT.md dependency graph + follow-up #4 strikethrough.

### 15.9. New (tests)
- `tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj`
- `tests/.../Integration/Fixtures/PostgresFixture.cs`, `PostgresCollection.cs`, `FeedApiFactory.cs`, `FakeUserPrefsProvider.cs`
- `tests/.../Unit/FeedTargetingTests.cs`, `FeedEventIdTests.cs`, `FeedBroadcasterTests.cs`, `RecordingScopeFactory.cs`
- `tests/.../Integration/Endpoints/FeedCrudTests.cs`, `FeedSseTests.cs`, `FeedServiceBroadcastOrderingTests.cs`
- `tests/.../Integration/Persistence/FeedRepositorySpecificationTests.cs`

### 15.10. Untouched
- AuthController, AuthService, TokenService, RefreshTokenStore, PasswordHasher, all Plans business logic
- ExceptionHandlerMiddleware, FluentValidationFilter, JWT wiring in Program.cs
- VectorDb, AiChat
- `appsettings*.json`, `nuget.config`

## 16. Verification gates

1. `dotnet build` clean, 0 warnings.
2. `dotnet ef migrations add feed_initial -p src/Reshape.ElectricAi.LiveFeed -s src/Reshape.ElectricAi.Presentation -- --context FeedDbContext` succeeds; partial-index DDL inspected.
3. `dotnet test tests/Reshape.ElectricAi.Plans.Tests` — still green (Plans untouched logically).
4. `dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests` — all green (Docker required).
5. Manual SSE smoke:
   ```powershell
   # CRUD requires Organizer token — mint via Plans:
   # POST /api/v1/auth/login with an Organizer-role account, copy access token
   curl.exe -N "http://localhost:5217/api/v1/feed/stream?userId=00000000-0000-0000-0000-000000000001"

   # second terminal — publish as Organizer
   curl.exe -X POST -H "Authorization: Bearer <organizer-token>" `
            -H "Content-Type: application/json" `
            -d '{\"title\":\"Rain\",\"body\":\"Light shower\",\"primaryCategory\":\"Weather\",\"isGeneral\":true,\"targetArtists\":[],\"targetGenres\":[]}' `
            http://localhost:5217/api/v1/feed
   # first terminal receives event: feed.created
   ```
6. Custom-agent review: `Code Reviewer` + `Security Engineer` + `Backend Architect` with directive *"verify CODE.md compliance against the changed files. Focus: SSE channel discipline, Repository+Specification pattern usage (not direct DbContext), Infrastructure project bounds, JWT claim reading (not header placeholder), no auth wiring of any kind added (master state is authoritative), CRUD auth attributes per CODE.md ## Auth, SSE stream intentionally [AllowAnonymous], FluentValidation 12.1.1 + global filter pattern."*

## 17. Open assumptions

1. `Last-Event-Id` interpreted as client cursor; no `feed_deliveries` log.
2. `EmptyUserPrefsProvider` default until Plans dev wires real provider.
3. SSE stream `[AllowAnonymous]` is **intentional** — user direction. `SseQueryStringTokenMiddleware` deferred. `?userId=` query is placeholder identity for the stream only; never honored on CRUD routes (which require JWT).
4. Artist match exact-string, case-sensitive.
5. Tie-breaker = Postgres `uuid` native byte-order (rev 2.1 reconciliation).
6. `IFeedService` lives in Core per CODE.md `## Services`.
7. Validators register via reflection scan inside `LiveFeedModule` (CODE.md line 23 — no `FluentValidation.DependencyInjectionExtensions` on feature libs).
8. EfRepository promotion to Infrastructure is the trigger PROJECT.md follow-up #4 names; spec executes that promotion.

## 18. Risks + mitigations

| Risk | Mitigation | Test |
|---|---|---|
| Broadcast fires before `SaveChangesAsync` commits | §6.2 ordering rule | `PublishEntry_WhenSaveChangesThrows_DoesNotBroadcast` |
| Singleton broadcaster captures scoped `IFeedService` | `IServiceScopeFactory` per replay | `SubscribeUserToStreamAsync_OnReplay_ResolvesFreshIFeedServiceScopePerCall` |
| `Response.Body` interleaved writes | `SemaphoreSlim(1,1)` | `StreamFeed_WhenHeartbeatAndEventInterleave_ProducesNoCorruptFrame` |
| Heartbeat task leaks past disconnect | `using PeriodicTimer` inside loop + broad catch | `StreamFeed_WhenClientDisconnects_HeartbeatTaskCompletesWithinShortWindow` |
| `SemaphoreSlim` leaks | `using var` in §9.2 | covered by disconnect test |
| Malformed cursor crashes | `TryParseEntryIdFromEventId` returns false → fall through | `StreamFeed_WhenLastEventIdHeaderMalformed_FallsThroughToRecentBatch` |
| Anonymous SSE leaks data | targeting filter at broadcast time; no claims-derived prefs → empty prefs → IsGeneral only | `StreamFeed_WhenAnonymousAndNoUserIdQuery_OnlyReceivesGeneralEntries` |
| Plans tests regress after Infrastructure promotion | `dotnet test Plans.Tests` part of verification gate | n/a (process) |

## 19. Definition of done

- All §15 files present, build clean.
- Both `Plans.Tests` and `LiveFeed.Tests` green.
- Manual SSE smoke (§16.5) passes.
- Custom-agent review (§16.6) findings addressed.
- CODE.md + PROJECT.md docs updates committed (Infrastructure promotion).
- Spec file deleted (Phase 10).

## 20. Diff summary vs rev 2.1

- **JWT live:** drop `ICurrentUserAccessor` / `HeaderCurrentUserAccessor` / `X-User-Id` header. Read `JwtRegisteredClaimNames.Sub` directly.
- **Auth attributes:** `[Authorize]` on reads, `[Authorize(Roles = "Organizer")]` on writes per CODE.md `## Auth`. SSE stream `[AllowAnonymous]` per user direction. `?userId=` query is the only SSE-side identity input.
- **FluentValidation 12.1.1:** drop `FluentValidation.DependencyInjectionExtensions` from LiveFeed. Use reflection-scan in `LiveFeedModule` mirroring `PlansModule`. Existing `FluentValidationFilter` (global) auto-runs.
- **Repository pattern:** `FeedService` injects `IRepository<FeedEntry>`, NOT `FeedDbContext`. Add `FeedRepository<T>` closing class. Add specs (`RecentFeedEntriesSpec`, `FeedEntriesSinceCursorSpec`, `FeedEntryByIdSpec`).
- **Infrastructure project (new):** promote `EfRepository<TContext, T>` + `SpecificationEvaluator` out of Plans. Both Plans + LiveFeed reference Infrastructure. PROJECT.md follow-up #4 trigger fires.
- **Exception namespace:** `Reshape.ElectricAi.Core.Domain.Exceptions` (was `.Exceptions`). No `ValidationException` — reuse existing types.
- **`ExceptionHandlerMiddleware`:** existing master version, untouched. Delete the rev-2.1 new-middleware task entirely.
- **Integration tests:** expanded per user ask. 11 SSE-flavored integration tests + CRUD coverage + persistence-spec tests. Mirror `Plans.Tests` layout + package versions (FluentAssertions 6.12.2, not 7.*).
- **Test auth:** real JWTs minted via `ITokenService` in `FeedApiFactory.CreateClientForUser(userId, roles)`. No fake auth handler.

User-explicit constraints preserved:
- ✅ No SSE auth enforcement / `SseQueryStringTokenMiddleware`.
- ✅ No location targeting.
- ✅ No business-logic changes to Plans (only mechanical namespace/move edits for Infrastructure promotion).
