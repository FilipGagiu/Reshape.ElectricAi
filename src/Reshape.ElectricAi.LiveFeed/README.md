# Reshape.ElectricAi.LiveFeed

> Real-time organizer-to-attendee push channel for the Electric Castle backend. Server-Sent Events (SSE) hub with personalized targeting based on each user's preferences.

This document explains the feature end-to-end: what it does, how a message gets from an organizer to the right phones, where each piece of code lives, and which constraints govern the design. **Status:** shipped on `feature/live-feed-v2` — 70/70 tests passing (32 Plans regression + 38 LiveFeed), 0 build warnings in scope, controllers carry XML doc comments ready for Swagger.

---

## 1. What it does

Organizers publish short feed entries ("30 min delay on Main Stage", "Rain after 21:00, stages stay open"). Each entry can be:

- **General** — broadcast to every connected user.
- **Targeted** — delivered only to users whose preferences intersect the entry's tags. Targeting uses two axes:
  - **Artist names** (free-text, exact-string, case-sensitive).
  - **Music genres** (`MusicGenre` enum: HipHop, House, Techno, Rock, etc.).

Attendees connect to a long-lived HTTP stream. As organizers publish, matching entries appear on their device within ~50 ms (in-process channel hop, no broker).

**Out of scope (v1):**
- Location-based targeting (no geo field anywhere).
- Group-level targeting (`GroupPreferences` ignored).
- Vector indexing of feed entries (Dev 2 — VectorDb lib).
- `feed_deliveries` per-user delivery log (replay relies on the client-supplied `Last-Event-ID` cursor).
- SSE authentication — stream endpoint is `[AllowAnonymous]` with a `?userId={guid}` query placeholder. Securing it (CODE.md `## Auth` line 184's query-string-token middleware) is deferred to a follow-up plan.

---

## 2. Architecture

```
┌──────────────┐                       ┌─────────────────────────┐
│ Organizer    │  POST /api/v1/feed    │ Presentation            │
│ (Bearer JWT, │ ───────────────────▶  │  FeedController         │
│  role=Organ.)│                       │   [Authorize(Roles=...)]│
└──────────────┘                       └──────────┬──────────────┘
                                                  │ IFeedService
                                                  ▼
                                       ┌─────────────────────────┐       ┌────────────────┐
                                       │ LiveFeed                │       │ Infrastructure │
                                       │  FeedService            │       │  EfRepository  │
                                       │   ├─ IRepository<Entry> ┼──────►│  Specification │
                                       │   │     (Add/Save)      │       │  Evaluator     │
                                       │   └─ IFeedBroadcaster   │       └────────┬───────┘
                                       │         (Broadcast AFTER│                │
                                       │          SaveChanges)   │                │
                                       └──────────┬──────────────┘                ▼
                                                  │                       Postgres `feed` schema
                                                  │ TryWrite to matching channels
                                                  ▼
┌──────────────┐                       ┌─────────────────────────┐
│ Attendee     │  GET  /api/v1/feed/   │ FeedController          │
│ (EventSource │   stream?userId=...   │  StreamFeedToCurrentUser│
│  in browser, │ ────────────────────▶ │   [AllowAnonymous]      │
│  no auth on  │   (SSE response       │   reads Channel<Env>,   │
│   stream v1) │    streams forever)   │   writes feed.* frames  │
└──────────────┘ ◀────────────────────  └─────────────────────────┘
   event: feed.created
   id:    2026-05-23T18:42Z-<guid>
   data:  { "id": ..., "title": "...", ... }

   : keepalive   (every 25 s)
```

**Two execution paths share one process:**

1. **Write path (POST/PUT/DELETE):** Controller → `FeedService` → `IRepository<FeedEntry>` (closed-generic, EF Core) → Postgres commit → `IFeedBroadcaster.BroadcastEventToMatchingSubscribers(kind, dto)`. Broadcast fires **only after** `SaveChangesAsync` returns — never before. A rollback never leaks an envelope.
2. **Read/stream path (GET /feed, GET /feed/stream):** Controller resolves the caller (JWT `sub` claim for CRUD, `?userId=` query for SSE), asks `IUserPrefsProvider` for that user's targeting preferences, then either lists DTOs (GET /feed) or subscribes to the broadcaster (GET /feed/stream) and forwards events to the client.

---

## 3. Component map

```
src/Reshape.ElectricAi.LiveFeed/
├── Entities/                    EF Core entities (POCOs).
│   ├── FeedEntry.cs             Aggregate root.
│   ├── FeedEntryArtist.cs       Owned collection (one row per target artist).
│   └── FeedEntryGenre.cs        Owned collection (one row per target genre).
│
├── Persistence/
│   ├── FeedDbContext.cs         DbContext for the `feed` Postgres schema.
│   ├── FeedDbContextFactory.cs  Design-time factory (used by `dotnet ef`).
│   ├── FeedRepository.cs        Closing class: FeedRepository<T> : EfRepository<FeedDbContext, T>.
│   ├── Configurations/          IEntityTypeConfiguration<T> per entity.
│   ├── Specifications/          ISpecification<FeedEntry> implementations.
│   │   ├── RecentFeedEntriesSpec.cs
│   │   ├── FeedEntriesSinceCursorSpec.cs
│   │   └── FeedEntryByIdSpec.cs   (asNoTracking flag — true for read-only Get path)
│   └── Migrations/              EF Core migration (`InitialFeedSchema`).
│
├── Broadcasting/                In-process SSE hub.
│   ├── FeedBroadcaster.cs       Singleton. ConcurrentDictionary<Guid, FeedSubscription>.
│   ├── FeedSubscription.cs      Per-connection bounded Channel<FeedEventEnvelope>.
│   ├── FeedTargeting.cs         Pure predicate (entry × prefs → bool).
│   └── FeedEventId.cs           Format + parse helpers for the SSE `id:` line.
│
├── Services/
│   ├── FeedService.cs           Application service. Implements Core.Services.IFeedService.
│   └── EmptyUserPrefsProvider.cs Default IUserPrefsProvider — returns empty prefs.
│                                 Plans dev replaces with a Postgres-backed impl later.
│
├── Dtos/
│   ├── PublishFeedEntryRequest.cs   HTTP input record (controller-side).
│   ├── UpdateFeedEntryRequest.cs    HTTP input record.
│   └── Mapping/
│       └── FeedEntryMapping.cs      ToDto / ToCommand / ToNewEntity / ApplyUpdateTo.
│
├── Validators/                  FluentValidation 12.1.1 validators auto-discovered by the module.
│   ├── PublishFeedEntryRequestValidator.cs
│   └── UpdateFeedEntryRequestValidator.cs
│
└── LiveFeedModule.cs            DI entry-point: AddLiveFeedModule(this IServiceCollection, IConfiguration).
                                 Registers DbContext, FeedRepository (closed-generic per entity:
                                 IRepository<FeedEntry>, <FeedEntryArtist>, <FeedEntryGenre>),
                                 FeedService (scoped), FeedBroadcaster (singleton),
                                 EmptyUserPrefsProvider (TryAdd), and validators (reflection scan).
```

**Shared abstractions in Core:**

```
src/Reshape.ElectricAi.Core/
├── Dtos/
│   ├── UserFeedPrefs.cs               Read-only set of artists + genres.
│   ├── FeedEntryDto.cs                The response record (also serialized in SSE data:).
│   ├── FeedEventEnvelope.cs           Kind + EventId + Entry.
│   ├── PublishFeedEntryCommand.cs     Service-layer input (no HTTP coupling).
│   └── UpdateFeedEntryCommand.cs
├── Enums/
│   └── FeedEventKind.cs               Created | Updated | Deleted.
└── Services/
    ├── IFeedService.cs                Service contract (CODE.md mandates interfaces in Core).
    ├── IFeedBroadcaster.cs            Broadcaster contract.
    └── IUserPrefsProvider.cs          Pluggable prefs source. Plans dev will swap impl.
```

**EF base in Infrastructure (shared by Plans + LiveFeed):**

```
src/Reshape.ElectricAi.Infrastructure/Persistence/
├── EfRepository.cs                    Generic IRepository<T> impl over any DbContext.
└── SpecificationEvaluator.cs          Applies ISpecification<T> to an IQueryable<T>.
```

**Controller (Presentation):**

```
src/Reshape.ElectricAi.Presentation/Controllers/
└── FeedController.cs                  Thin controller. XML doc comments on every action.
                                       [Produces("application/json")] class default;
                                       [Produces("text/event-stream")] override on /stream.
                                       [ProducesResponseType(...)] for every status code.
```

---

## 4. Domain model

### `FeedEntry`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uuid` PK | Server-generated. |
| `Title` | `varchar(200)` | Required. |
| `Body` | `varchar(4000)` | Required. |
| `PrimaryCategory` | `varchar(32)` enum string | `General`, `Music`, `Weather`, etc. (display only, NOT used for targeting in v1). |
| `IsGeneral` | `boolean` | When `true`, matches every subscriber. |
| `PublishedByUserId` | `uuid` | Loose FK to `plans.Users.Id`. No EF navigation across DbContexts. |
| `PublishedUtc` | `timestamptz` | Set at publish time. Drives ordering. |
| `UpdatedUtc` | `timestamptz?` | Set on update. |
| `DeletedUtc` | `timestamptz?` | **Soft delete.** Non-null entries are hidden from queries but stay in the table for audit. |

### `FeedEntryArtist` (one-to-many, owned)

| Column | Type | Notes |
|---|---|---|
| `FeedEntryId` | `uuid` PK part | Cascade delete from `FeedEntry`. |
| `ArtistName` | `varchar(100)` PK part | Exact-string match against `UserPreferenceArtist.ArtistName`. **Case-sensitive** by design — documented + unit-tested. Future plan may normalize. |

### `FeedEntryGenre` (one-to-many, owned)

| Column | Type | Notes |
|---|---|---|
| `FeedEntryId` | `uuid` PK part | Cascade. |
| `Genre` | `varchar(32)` enum string | Stored as enum name for forward compatibility. |

### Indexes

- `(PublishedUtc DESC)` — single-column DESC for full-history scans.
- `(DeletedUtc ASC, PublishedUtc DESC) WHERE DeletedUtc IS NULL` — **partial index** for the not-deleted hot path. Emitted via `HasFilter(...)` + `IsDescending(false, true)` so Postgres can scan forward for newest-first reads. The descending direction on `PublishedUtc` is mandatory; without it, `RecentFeedEntriesSpec` falls into a sub-optimal backward scan.

### Schema

All three tables live in the `feed` schema (`HasDefaultSchema("feed")`). Migrations history goes into `feed.__EFMigrationsHistory`. One schema per lib (CODE.md mandate).

---

## 5. Targeting rules

The pure predicate is one function:

```csharp
public static bool EntryMatchesUserPrefs(FeedEntryDto entry, UserFeedPrefs prefs) =>
    entry.IsGeneral
 || entry.TargetArtists.Any(prefs.Artists.Contains)
 || entry.TargetGenres.Any(prefs.Genres.Contains);
```

**Read:** an entry matches a user if it's general, OR any of its target artists overlaps the user's artist preferences, OR any of its target genres overlaps the user's genre preferences. A user with empty prefs only sees `IsGeneral` entries.

`PrimaryCategory` is NOT part of targeting — it's display metadata (icon, badge color in the UI). Could become a targeting axis in a future revision.

The predicate is exercised in two places:

1. **At broadcast time** (`FeedBroadcaster.BroadcastEventToMatchingSubscribers`) — every connected user's prefs snapshot is in memory; predicate runs in microseconds per subscriber.
2. **At read time** (`FeedService.ListRecentEntriesMatchingPrefsAsync`, `ListEntriesSinceEventIdMatchingPrefsAsync`) — the SQL spec fetches not-deleted entries by ordering, then the predicate filters in memory.

The targeting predicate is fully unit-tested (`FeedTargetingTests`, 5 cases).

---

## 6. The broadcaster — `FeedBroadcaster`

A **singleton** registered in DI. Owns the SSE hub state.

```csharp
public sealed class FeedBroadcaster(IServiceScopeFactory scopeFactory) : IFeedBroadcaster
{
    private readonly ConcurrentDictionary<Guid, FeedSubscription> _subs = new();
    ...
}
```

### Why singleton

In-memory subscription state is **shared global state across all HTTP requests**. Publish from request A must reach subscribers in requests B..Z. Scoped instance = each request gets its own empty dictionary = no fan-out. Singleton = one shared `ConcurrentDictionary` = pub/sub works. Channel lifetime exceeds any request scope (SSE connections live minutes to hours), so subscription bookkeeping must outlive request lifetimes.

### Why `IServiceScopeFactory`?

`FeedBroadcaster` is singleton; `IFeedService` is scoped (it depends on the scoped `IRepository<FeedEntry>` which holds the scoped `FeedDbContext`). On every SSE connect the broadcaster needs to call `IFeedService` to fetch the replay batch. A singleton can't capture a scoped service directly — that's the classic captive-dependency bug (ASP.NET Core's `ValidateScopes=true` throws at build OR runtime serves a stale instance forever). The broadcaster instead injects `IServiceScopeFactory` (itself a singleton, safe to capture) and opens a **per-subscribe scope**:

```csharp
using (var scope = scopeFactory.CreateScope())
{
    var feed = scope.ServiceProvider.GetRequiredService<IFeedService>();
    replay = ... await feed.ListRecentEntriesMatchingPrefsAsync(...);
}
```

The scope (and its DbContext) is disposed before the long-running `await foreach` loop begins, so the connection's lifetime doesn't pin a DbContext.

### Per-connection channel

Each subscriber gets one bounded `Channel<FeedEventEnvelope>`:

```csharp
Channel.CreateBounded<FeedEventEnvelope>(
    new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
```

- **Capacity 100** — bounded so a slow client can't grow memory.
- **DropOldest** — if a subscriber falls behind, the oldest queued envelopes drop. The freshest 100 are always available. Matches CODE.md's `## SSE (LiveFeed)` rules.
- **SingleReader** — exactly one reader (the SSE writer in the controller). Asserts in the channel; protects against accidental double-consumer wiring later.
- **SingleWriter = false** — multiple publishers (any number of concurrent POST handlers) may `TryWrite`.

`TryWrite` is non-blocking. The broadcaster never awaits on a slow subscriber; it returns immediately and moves to the next.

### Subscription lifetime

```csharp
public async IAsyncEnumerable<FeedEventEnvelope> SubscribeUserToStreamAsync(
    Guid userId, UserFeedPrefs prefs, string? lastEventId,
    [EnumeratorCancellation] CancellationToken ct)
{
    var sub = CreateSubscriptionForUser(userId, prefs);
    RegisterSubscription(sub);
    try
    {
        ... // yield replay batch
        await foreach (var env in sub.Channel.Reader.ReadAllAsync(ct)) yield return env;
    }
    finally
    {
        RemoveSubscriptionById(sub.SubscriptionId);
        sub.Channel.Writer.TryComplete();
    }
}
```

`try/finally` guarantees the dictionary entry is removed on **any** exit path: client disconnect (ct cancels), iterator dispose, exception, normal completion. No subscription leak across restarts of the same client.

The `SubscriptionId` is a per-connection `Guid` — meaning a single user can have multiple tabs open, each with its own channel and prefs snapshot. 50 unique users + 50 tabs each = 2500 entries in the dictionary, all independent.

---

## 7. Persistence layer — Repository + Specification

LiveFeed does **not** inject `FeedDbContext` directly into the service. It goes through Core abstractions:

```csharp
internal sealed class FeedService(
    IRepository<FeedEntry> repository,
    IFeedBroadcaster broadcaster) : IFeedService { ... }
```

`IRepository<T>` is registered in `LiveFeedModule` **per entity (closed generic)** to avoid shadowing Plans's open-generic registration:

```csharp
services.AddScoped<IRepository<FeedEntry>, FeedRepository<FeedEntry>>();
services.AddScoped<IRepository<FeedEntryArtist>, FeedRepository<FeedEntryArtist>>();
services.AddScoped<IRepository<FeedEntryGenre>, FeedRepository<FeedEntryGenre>>();
```

**Why closed-generic and not open?** Plans registers open-generic `IRepository<>` → `PlansRepository<>` in `PlansModule`. If LiveFeed also registered open-generic, the later registration would replace the earlier one — Plans's services resolving `IRepository<User>` would land in `FeedRepository<User>`, which calls `FeedDbContext.Set<User>()` and throws `Cannot create a DbSet for 'User' because this type is not included in the model for the context`. CODE.md `## Persistence layer` documents the rule: the FIRST lib in registration order may use open-generic; every later lib must close per-entity. MS.DI prefers the closed-specific registration over open-generic when both match a closed type, so per-entity closes from LiveFeed win for their three entities without disturbing Plans.

`FeedRepository<T>` (in LiveFeed) is just:

```csharp
public sealed class FeedRepository<T>(FeedDbContext context)
    : EfRepository<FeedDbContext, T>(context)
    where T : class;
```

`EfRepository<TContext, T>` lives in `Reshape.ElectricAi.Infrastructure`. Plans uses the same base via `PlansRepository<T>`. Both libs depend on Infrastructure → Core; no cycle.

### Why Specifications?

Plans introduced this pattern; LiveFeed reuses it for consistency. A spec encodes a query (where + includes + ordering + paging) as a value object:

```csharp
public sealed class RecentFeedEntriesSpec : Specification<FeedEntry>
{
    public RecentFeedEntriesSpec(Category? categoryFilter, int take)
    {
        Where(e => e.DeletedUtc == null /* + optional category filter */);
        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        ApplyOrderByDescending(e => e.PublishedUtc);
        ApplyPaging(0, take);
        EnableNoTracking();
        EnableSplitQuery();
    }
}
```

`repository.ListAsync(new RecentFeedEntriesSpec(category, 100), ct)` runs the spec via `SpecificationEvaluator.Apply(...)`. The service never builds queryables inline.

Three specs exist:
- `RecentFeedEntriesSpec(Category?, int take)` — recent not-deleted.
- `FeedEntriesSinceCursorSpec(DateTime, Guid, int)` — replay-on-connect via decomposed cursor predicate (`PublishedUtc > p OR (PublishedUtc == p AND Id > g)`). Translates cleanly under Npgsql.
- `FeedEntryByIdSpec(Guid, bool asNoTracking = false)` — by-id lookup with target collections included. `asNoTracking=true` only on the read-only `GetEntryByIdAsync` path; Update/Delete keep tracking on so EF can mutate the loaded graph.

---

## 8. SSE wire protocol

### Event-Id format

```
{PublishedUtc:O}-{Guid:D}
```

Example:

```
2026-05-23T18:42:11.0123456Z-3f5b0a7c-9f7d-4a01-9c10-aab47c8f5c19
```

The prefix is ISO 8601 round-trip; the suffix is a hex Guid. Lexicographic comparison on the prefix equals chronological order. `FeedEventId.TryParseEntryIdFromEventId(...)` decomposes the string into `(DateTime, Guid)` for the cursor predicate.

**Tie-breaker on same millisecond:** Postgres `uuid` native byte-order comparison (the natural semantic of `e.Id > cursorEntryId` in the SQL predicate). NOT `Guid.ToString()` lexicographic — the two orderings differ for .NET because Guid string format is mixed-endian. As long as both write-time format and read-time cursor comparison use the same ordering, replay is deterministic and lossless.

### Frame format

```
event: feed.created
id: 2026-05-23T18:42:11.0123456Z-3f5b0a7c-9f7d-4a01-9c10-aab47c8f5c19
data: {"id":"3f5b...","title":"Rain","body":"Light shower after 21:00","primaryCategory":"weather","isGeneral":true,"targetArtists":[],"targetGenres":[],"publishedUtc":"2026-05-23T18:42:11.0123456Z","updatedUtc":null}

```

(Two trailing newlines terminate the frame, per the SSE spec.)

Event types:

| Wire | When |
|---|---|
| `event: feed.created` | New entry just committed. |
| `event: feed.updated` | Existing entry mutated (title/body/targeting). |
| `event: feed.deleted` | Soft-deleted. Clients should remove the entry from the rendered list. |

### Heartbeat

Every 25 seconds the writer emits a comment line:

```
: keepalive

```

Comments start with `:` and are ignored by the EventSource API. They serve two purposes:

1. Keep proxies (nginx, Cloudflare, etc.) from closing idle connections.
2. Let the client realize the server is alive even with no events flowing.

The heartbeat lives in the controller (`RunHeartbeatLoopAsync`), driven by `PeriodicTimer(TimeSpan.FromSeconds(25))`. The timer is `using`-bound inside the loop method, so disposal is local — no timer leak across reconnects.

### Concurrent writes

Two things can write to `Response.Body` at the same instant:

1. The event loop (when a published entry matches and reaches the channel reader).
2. The heartbeat tick (every 25 s).

If they raced, frames would interleave mid-line and corrupt the SSE stream. A per-connection `SemaphoreSlim(1, 1)` serializes the two writers:

```csharp
using var writeLock = new SemaphoreSlim(1, 1);
var heartbeatTask = RunHeartbeatLoopAsync(writeLock, ct);
try { await foreach (var env in ...) await WriteSseEventFrameAsync(env, writeLock, ct); }
catch (OperationCanceledException) { }
catch (ObjectDisposedException) { }
catch (IOException) { }
finally { try { await heartbeatTask; } catch (...) { } }
```

The `using var` disposes the semaphore on connection close. Cancellation is the normal exit for an SSE consumer — the outer try/catch swallows `OperationCanceledException` / `ObjectDisposedException` / `IOException` (Kestrel teardown mid-write) so the request doesn't bubble up to the global exception middleware (which would clear the response body and write a JSON envelope, breaking content type).

### Reconnect / replay (`Last-Event-ID`)

EventSource auto-reconnects on disconnect. Browsers send the last successfully delivered id as the `Last-Event-ID` header on the reconnect request. The stream handler reads it:

```csharp
var lastEventId = Request.Headers["Last-Event-ID"].FirstOrDefault();
```

If present, `IFeedService.ListEntriesSinceEventIdMatchingPrefsAsync(lastEventId, prefs, 10, ct)` parses it via `FeedEventId.TryParseEntryIdFromEventId(...)` and yields the next batch. If the parse fails (malformed cursor, missing header), the call falls through to `ListRecentEntriesMatchingPrefsAsync(...)` — the client doesn't lose stream.

The cap is **10 entries on replay**, intentional: clients shouldn't be flooded after a brief network hiccup. A long-disconnected client can paginate through `GET /feed` to backfill more history.

### Response headers

```
Content-Type: text/event-stream
Cache-Control: no-cache, no-transform
Connection: keep-alive
X-Accel-Buffering: no
```

`no-transform` defeats proxies that try to compress; `X-Accel-Buffering: no` defeats nginx's default response buffering.

---

## 9. Endpoints

All routes are under `/api/v1/feed` (CODE.md `## Controllers`). Controllers carry XML doc comments + `[ProducesResponseType]` attributes; once the host opts in to `swagger.IncludeXmlComments(...)`, the descriptions surface in Scalar UI automatically.

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/v1/feed?category=<Category>` | `[Authorize]` | List recent (last 100) not-deleted entries filtered by category + the caller's prefs. |
| `POST` | `/api/v1/feed` | `[Authorize(Roles = "Organizer")]` | Publish new entry. Returns 201 + DTO. `Location` header points at entry URL. |
| `PUT` | `/api/v1/feed/{id}` | `[Authorize(Roles = "Organizer")]` | Update entry. 404 if missing or deleted. |
| `DELETE` | `/api/v1/feed/{id}` | `[Authorize(Roles = "Organizer")]` | Soft-delete. 204. **Idempotent**: re-deleting an already-deleted entry is a no-op — no exception, no second broadcast. |
| `GET` | `/api/v1/feed/stream?userId={guid}` | `[AllowAnonymous]` | SSE stream. `userId` is the targeting placeholder (see §10). |

### Validation

`FluentValidationFilter` is wired globally in Presentation. Any 400 response uses the project's error-envelope shape:

```json
{ "error": { "code": "validation-failed", "message": "...", "details": { "Field": ["..."] } } }
```

Specific rules on publish/update requests:

- `Title` 1..200 chars.
- `Body` 1..4000 chars.
- `PrimaryCategory` must be a valid enum value.
- `TargetArtists` ≤ 25 entries, each 1..100 chars, no case-insensitive duplicates.
- `TargetGenres` ≤ 12 entries, valid enum, no duplicates.
- **`IsGeneral == false` AND empty artists AND empty genres → fail with `code: "no-targeting-and-not-general"`.** An entry that targets no one is rejected at publish time.

---

## 10. Authentication model

### CRUD endpoints (POST/PUT/DELETE + GET list)

Standard JWT bearer. The master branch already wires:

- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer()`
- `TokenValidationParameters` with `ValidAlgorithms = [HmacSha256]`, issuer/audience pins, signing key from `Auth:JwtSigningKey`.
- `JwtBearerEvents.OnChallenge` / `OnForbidden` writing the standard error envelope.
- `MapInboundClaims = false` (no legacy SAML mapping).
- `app.UseAuthentication() + app.UseAuthorization()`.

Inside FeedController, the user id is read directly from the JWT `sub` claim:

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

Update/Delete actions defensively call `GetCurrentUserId(User)` and discard the result — `[Authorize(Roles="Organizer")]` already gates the role, but the discard provides a belt-and-suspenders assertion that the JWT carries a valid `sub` claim. If audit logging arrives, this userId can be passed through to the service.

Roles use the standard ASP.NET Core `Roles = "Organizer"` mechanism; the JWT carries a `role` claim with the `UserRole` enum value.

### Stream endpoint (intentionally anonymous in v1)

`GET /api/v1/feed/stream` is decorated `[AllowAnonymous]`. The browser EventSource API **cannot attach an `Authorization` header**, and the secure workaround (`?access_token=` middleware described in CODE.md `## Auth` line 184) is deferred per current product direction.

Until that middleware lands, the stream accepts an opaque `?userId={guid}` query parameter purely so the broadcaster has a key to look up the caller's targeting prefs. Without it the connection still works but the caller only receives `IsGeneral` entries.

**Limitations of the current model (acknowledged):**

- Anyone who knows a user id can subscribe to their personalized feed.
- No connection-count throttling on the stream — DoS surface for an anonymous attacker opening many simultaneous SSE connections (security audit flagged as MAJOR; deferred to v2 with rate-limit middleware).
- A future plan will add `SseQueryStringTokenMiddleware` (per CODE.md) — extracts `?access_token=` only on the stream route, rewrites the `Authorization` header before JWT middleware runs, rejects the query token on every other route.

---

## 11. DI registration

`LiveFeedModule.AddLiveFeedModule(services, configuration)` is the single DI entry-point (one per lib, per CODE.md). It registers:

```csharp
services.AddDbContext<FeedDbContext>(...);

// Closed-generic per entity (NOT open-generic) — see § 7 for the why.
services.AddScoped<IRepository<FeedEntry>,        FeedRepository<FeedEntry>>();
services.AddScoped<IRepository<FeedEntryArtist>,  FeedRepository<FeedEntryArtist>>();
services.AddScoped<IRepository<FeedEntryGenre>,   FeedRepository<FeedEntryGenre>>();

services.AddScoped<IFeedService, FeedService>();
services.AddSingleton<IFeedBroadcaster, FeedBroadcaster>();
services.TryAddScoped<IUserPrefsProvider, EmptyUserPrefsProvider>();

RegisterValidators(services);  // reflection scan, same pattern as PlansModule
```

`Program.cs` calls it after `AddPlansModule` and `AddVectorDbModule`. The development-only DB migration block runs `MigrateAsync()` for `PlansDbContext`, `VectorDbContext`, and `FeedDbContext` in that order on app startup.

`TryAddScoped<IUserPrefsProvider, EmptyUserPrefsProvider>()` is intentional: when the Plans dev wires the real preferences provider (reading from `plans.UserPreferences`), their registration takes precedence as long as `AddPlansModule` runs before `AddLiveFeedModule` in `Program.cs` (currently the case). Until then, every user looks like "empty prefs" → only `IsGeneral` entries reach them.

---

## 12. Configuration + local dev setup

The only required key (beyond Plans's existing keys) is the standard:

```
ConnectionStrings:Postgres = Host=localhost;Port=5432;Database=electric_ai;Username=postgres;Password=postgres
```

There are no LiveFeed-specific knobs in `appsettings.json` for v1. The channel capacity (100), heartbeat interval (25 s), and replay cap (10 entries) are hard-coded constants — appropriate for hackathon-scale. If they ever need to vary by environment, promote to `appsettings.json` keys.

### Postgres requirement

The `VectorDbContext` migration runs `CREATE EXTENSION vector` on startup. If the local Postgres lacks pgvector, app boot fails with `0A000: extension "vector" is not available`. Two options on a developer workstation:

1. **Run Postgres in Docker with pgvector pre-installed** (recommended):

   ```powershell
   docker run -d --name electric-ai-dev `
     -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=admin `
     -e POSTGRES_DB=electric_ai `
     -p 5433:5432 `
     -v electric-ai-data:/var/lib/postgresql/data `
     pgvector/pgvector:pg16

   dotnet user-secrets set "ConnectionStrings:Postgres" `
     "Host=localhost;Port=5433;Database=electric_ai;Username=postgres;Password=admin" `
     --project src/Reshape.ElectricAi.Presentation
   ```

   (Port 5433 to avoid colliding with a native Postgres service on 5432.)

2. **Install pgvector on the native Postgres**. Build from source via `nmake /F Makefile.win` against the local Postgres install, or use the Windows pre-built binaries from [pgvector releases](https://github.com/pgvector/pgvector/releases).

---

## 13. Tests

`tests/Reshape.ElectricAi.LiveFeed.Tests` mirrors `Plans.Tests` in structure and package versions.

### Unit tests (no Docker required) — 16 passing

| File | Coverage |
|---|---|
| `FeedTargetingTests` | 5 cases over the targeting truth table including the case-sensitive artist documentation test. |
| `FeedEventIdTests` | Format round-trip + theory over malformed inputs. |
| `FeedBroadcasterTests` | Match/no-match delivery, empty replay, scope-per-subscribe contract. |

A small `RecordingScopeFactory` test fixture stubs `IFeedService` without touching EF, so these tests run in milliseconds.

### Integration tests (Docker required) — 22 passing

`FeedApiFactory : WebApplicationFactory<Program>` boots the real `Program.cs` against a Testcontainers PostgreSQL container (`pgvector/pgvector:pg16`), overrides the connection string + JWT signing key, and exposes:

- `CreateClientForUser(Guid userId, UserRole role)` — mints a real JWT via the host's `ITokenService` and sets the `Authorization: Bearer ...` header.
- `CreateAnonymousClient()` — no token.
- `FakePrefs` — a swap-in `IUserPrefsProvider` allowing tests to declare per-user prefs.
- `ResetDatabaseAsync()` — drop + re-migrate the `feed` schema between tests.

| File | What's covered |
|---|---|
| `FeedCrudTests` (8) | Auth gate (401), role gate (403), happy publish, ordering, soft delete removal, 404 on missing entry, validation failure code. |
| `FeedSseTests` (10) | Matched delivery, unmatched silence, 25 s heartbeat, `Last-Event-ID` replay (happy + malformed), multi-subscriber isolation, heartbeat-vs-event interleave correctness, disconnect cleanup, response header compliance, anonymous-stream behavior. |
| `FeedServiceBroadcastOrderingTests` (2) | Broadcast happens **after** `SaveChangesAsync`. Idempotent re-delete does not broadcast. |
| `FeedRepositorySpecificationTests` (2) | `RecentFeedEntriesSpec` orders + excludes deleted. `FeedEntryByIdSpec` includes target collections. |

The Plans test suite (32 tests) continues to pass — the only LiveFeed-induced change inside Plans is a single using-statement update in `PlansRepository.cs` after `EfRepository` moved to Infrastructure.

---

## 14. Adding to the feature later

Where to plug in common future changes, without re-reading the whole codebase:

- **A real `IUserPrefsProvider`.** Plans dev creates `PostgresUserPrefsProvider : IUserPrefsProvider` reading from `plans.UserPreferenceArtists` + `plans.UserPreferenceGenres`, registers it in `PlansModule` with `services.AddScoped<IUserPrefsProvider, PostgresUserPrefsProvider>()`. Because `LiveFeedModule` uses `TryAddScoped`, the Plans registration wins automatically as long as `AddPlansModule` runs before `AddLiveFeedModule` in `Program.cs` (currently the case).

- **Add a new targeting axis** (e.g. activity preferences).
  1. Add `Target<X>` collection on `FeedEntry` + its configuration + a migration.
  2. Extend `UserFeedPrefs` with a new set property.
  3. Extend `FeedTargeting.EntryMatchesUserPrefs` with a third clause.
  4. Update validators + add a unit test row.
  5. Update both mapping methods (`ToNewEntity`, `ApplyUpdateTo`).
  6. Add a new closed-generic IRepository registration in `LiveFeedModule` if the new collection becomes a separate entity.

- **Secure the stream.** Add `Presentation/Middleware/SseQueryStringTokenMiddleware.cs` per CODE.md `## Auth` line 184. Remove `[AllowAnonymous]` from `StreamFeedToCurrentUserAsync`. Delete the `?userId=` query parameter — `GetCurrentUserId(User)` reads the claim. Add an integration test that the middleware rejects `?access_token=` on every route other than `/api/v1/feed/stream`.

- **Surface XML doc comments in Swagger / Scalar.** Add one line in Program.cs's existing `AddSwaggerGen` call:

  ```csharp
  builder.Services.AddSwaggerGen(swagger =>
  {
      var xmlPath = Path.Combine(AppContext.BaseDirectory,
          $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
      if (File.Exists(xmlPath)) swagger.IncludeXmlComments(xmlPath);
  });
  ```

  The XML file is already generated (`Presentation.csproj` has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`). One opt-in line and Scalar UI renders the descriptions.

- **Horizontal scale.** The `FeedBroadcaster.ConcurrentDictionary<...>` is in-process. For multi-instance deployment, swap to a fan-out backplane (Redis pub/sub, SignalR backplane, or NATS). Service interfaces (`IFeedBroadcaster`) stay; only the broadcaster impl changes. `LiveFeedModule` picks the impl based on configuration.

- **Vector-index feed entries.** Dev 2's VectorDb lib will publish a new chunk per published entry. Likely a thin call appended after the broadcast inside `FeedService.PublishEntryAsync` (or via a hosted background queue if synchronous embed-on-publish becomes too slow).

---

## 15. Known limitations (v1)

- **In-memory channel state.** Restart = all subscribers reconnect from scratch (their EventSource clients auto-retry). Single-instance only — see horizontal scale note above.
- **No `feed_deliveries` log.** Replay relies on the client-supplied `Last-Event-ID` cursor. If the cursor is older than the entries currently in DB (e.g., a client offline for a year), they get the most recent batch, not strict "everything since X". For v1 this is acceptable: feed entries are inherently short-lived.
- **SSE stream is `[AllowAnonymous]`.** `?userId=` is trivially spoofable. Documented above; future plan adds the query-string token middleware.
- **No rate limiting on the stream.** A single anonymous attacker can open many SSE connections. Security audit flagged as MAJOR; deferred to v2 with ASP.NET Core rate-limiting middleware per-IP.
- **Artist match is case-sensitive.** Documented + unit-tested. If organizers report misses, normalize at write time (lowercase + trim) — additive change.
- **Synchronous fan-out on the publish request thread.** At 1000+ subscribers, every publish adds noticeable latency. Future: post the fan-out to a background `ChannelWriter<>` consumed by an `IHostedService`.

---

## 16. Recent fixes worth knowing about

These five issues were found and fixed during the LiveFeed slice's verification cycle. Documented here so future devs (and you in two months) don't re-hit them.

1. **Open-generic `IRepository<>` shadowing.** Plans registers `IRepository<>` open-generic; the original LiveFeed module did the same and silently replaced Plans's registration (last-wins). Plans's `AuthService` then resolved `FeedRepository<User>` and failed at `FeedDbContext.Set<User>()`. Fixed by closing the generic per-entity in `LiveFeedModule` for `FeedEntry`, `FeedEntryArtist`, `FeedEntryGenre`. CODE.md `## Persistence layer` now documents the rule.

2. **`CreatedAtAction(nameof(...Async))` route resolution failure.** MVC strips the `Async` suffix from action names when building routes; `nameof()` returns the full name, so the lookup misses every time. Replaced with `Created($"/api/v1/feed/{dto.Id}", dto)` which builds the Location URL directly.

3. **`OperationCanceledException` escape from SSE writer.** Client disconnect cancels the request → `await foreach` over the channel throws OCE → escapes to `ExceptionHandlerMiddleware` → clears response + writes JSON envelope, replacing the `text/event-stream` content type. Fixed by wrapping the loop in `try { ... } catch (OperationCanceledException) { } catch (ObjectDisposedException) { } catch (IOException) { }`. Same trio also wraps the `await heartbeatTask` in `finally`.

4. **Test JSON enum deserialization.** Server emits camelCase enum strings via the global `JsonStringEnumConverter`; tests using bare `ReadFromJsonAsync<FeedEntryDto>()` fail to deserialize because default options expect integer enums. Added `TestJson.Options` (a shared `JsonSerializerOptions` with `JsonStringEnumConverter`) to test fixtures and pass it to every `ReadFromJsonAsync` call.

5. **Partial index missing `DESC` modifier.** The composite partial index `(DeletedUtc, PublishedUtc) WHERE DeletedUtc IS NULL` originally indexed `PublishedUtc` ASC, which Postgres can scan backward but loses some optimizations. Fixed via `.IsDescending(false, true)` in the EF configuration; migration regenerated to produce `descending: new[] { false, true }` in the DDL.

---

## 17. Quick reference — running the feature

Once Docker Desktop is installed:

```bash
# 1. Build + verify
dotnet build
dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests
dotnet test tests/Reshape.ElectricAi.Plans.Tests   # regression check

# 2. Start a local Postgres with pgvector (or apply migrations to a native install — see §12)
docker run -d --name electric-ai-dev -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=admin `
  -e POSTGRES_DB=electric_ai -p 5433:5432 -v electric-ai-data:/var/lib/postgresql/data `
  pgvector/pgvector:pg16

# 3. Run the API (Development env auto-migrates all four contexts)
dotnet run --project src/Reshape.ElectricAi.Presentation
# default port 5217, Scalar UI at http://localhost:5217/scalar/v1
```

Manual smoke (PowerShell):

```powershell
# Terminal 1 — subscribe anonymously, target a user id
curl.exe -N "http://localhost:5217/api/v1/feed/stream?userId=00000000-0000-0000-0000-000000000001"

# Terminal 2 — register an Organizer (one-time)
# (use Plans auth endpoints: POST /api/v1/auth/register, then UPDATE plans."Users" SET "Role"='Organizer'
# WHERE "Email"='organizer@example.com' from psql; then POST /api/v1/auth/login to get an access token)

# Terminal 2 — publish
curl.exe -X POST `
         -H "Authorization: Bearer $organizerToken" `
         -H "Content-Type: application/json" `
         -d '{\"title\":\"Rain\",\"body\":\"Light shower\",\"primaryCategory\":\"Weather\",\"isGeneral\":true,\"targetArtists\":[],\"targetGenres\":[]}' `
         http://localhost:5217/api/v1/feed
```

Terminal 1 should receive `event: feed.created` within ~50 ms and a `: keepalive` line every 25 seconds thereafter.
