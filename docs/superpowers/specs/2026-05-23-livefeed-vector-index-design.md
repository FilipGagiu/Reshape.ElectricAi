# LiveFeed → VectorDb integration (publish-only) — design spec

**Date:** 2026-05-23
**Branch:** `feature/live-feed-vector-integration`
**Status:** approved (brainstorm complete, awaiting user spec review)
**Author:** Claude (with adimi)

---

## 1. Motivation

`Reshape.ElectricAi.LiveFeed` shipped on `feature/live-feed-v2` without any vector-index integration. The README explicitly marks "Vector indexing of feed entries" as out of scope for v1 (§1 line 23) and points at §14 for the future plug-in. This spec defines the v1 wiring: every newly published feed entry is embedded and persisted as an `EventEntry` row in `vector.event_entries` so the existing `IVectorSearchService.SearchEventsAsync` surface returns LiveFeed entries.

## 2. Scope

In:
- LiveFeed `PublishEntryAsync` calls `IIngestService.IngestEventAsync` after the SSE broadcast.
- New mapping helper that builds `IngestEventRequest` from a `FeedEntry`.
- Integration tests that prove the row lands in VectorDb and that publish stays green when the embedding service throws.

Out:
- `UpdateEntryByIdAsync` does NOT re-embed (stale vector accepted as v1 trade-off; documented in §10).
- `SoftDeleteEntryByIdAsync` does NOT remove or tombstone the `EventEntry` row (same trade-off).
- No new HTTP endpoint (no semantic-search route on FeedController).
- No config toggle / feature flag.
- No changes to `IIngestService`, VectorDb source, Presentation source, Core source.

## 3. Constraints

Hard:
- Files modifiable: only under `src/Reshape.ElectricAi.LiveFeed/` and `tests/Reshape.ElectricAi.LiveFeed.Tests/`. Confirmed via brainstorm Q5.
- VectorDb and Core source are off-limits without explicit user permission.
- No new NuGet packages (CLAUDE.md §6a, hook-enforced).

Soft:
- CODE.md compliance: every code edit honors the rulebook (re-read before each write, per CLAUDE.md Phase 7).
- All existing tests (70/70) must still pass after the change.

## 4. Architecture

```
FeedController.PublishEntryAsync
        │
        ▼
FeedService.PublishEntryAsync
   1. repository.AddAsync(entry)
   2. repository.SaveChangesAsync         ← FeedEntry committed
   3. broadcaster.BroadcastEvent(Created, dto)
   4. await SafeIngestEventAsync(entry)   ← NEW
        │
        ▼
IIngestService.IngestEventAsync          (Core abstraction, impl in VectorDb)
        │
        ▼
IngestService (VectorDb, unchanged)
   - idempotent EventByFeedEntryIdSpec gate
   - IEmbeddingService.GenerateEmbeddingAsync
   - eventRepository.AddAsync + SaveChanges
        │
        ▼
vector.event_entries row keyed on FeedEntryId
```

Single new arrow. `UpdateEntryByIdAsync` and `SoftDeleteEntryByIdAsync` paths untouched.

## 5. Components touched

| Layer | File | Change |
|---|---|---|
| LiveFeed | `Services/FeedService.cs` | Add `IIngestService ingestService` and `ILogger<FeedService> logger` ctor params. Add private `SafeIngestEventAsync(FeedEntry, CancellationToken)`. Call it last in `PublishEntryAsync` |
| LiveFeed | `Dtos/Mapping/FeedEntryMapping.cs` | Add `ToIngestEventRequest(this FeedEntry)` extension |
| LiveFeed | `LiveFeedModule.cs` | No change (`IIngestService` resolved from upstream `AddVectorDbModule`) |
| Core | — | No change |
| Presentation | — | No change |
| Tests | `Integration/Fixtures/FeedApiFactory.cs` | Override `IEmbeddingService` registration with `FakeEmbeddingService`. Add `ResetVectorEventsAsync()` companion to existing `ResetDatabaseAsync()` |
| Tests | `Integration/Fixtures/FakeEmbeddingService.cs` | New — duplicates the existing implementation in `VectorDb.Tests` (32 dims, deterministic hash-seeded, unit-normalized) |
| Tests | `Integration/Fixtures/ThrowingEmbeddingService.cs` | New — `IEmbeddingService` that throws `InvalidOperationException` on every call (used by the swallow-error test) |
| Tests | `Integration/FeedVectorIndexTests.cs` | New — two tests (see §8) |

## 6. Mapping rules — `FeedEntry → IngestEventRequest`

```csharp
public static IngestEventRequest ToIngestEventRequest(this FeedEntry entry)
{
    var textRepresentation = $"{entry.Title}\n\n{entry.Body}";

    var tags = new Dictionary<Category, IReadOnlyList<string>>();

    if (entry.TargetGenres.Count > 0)
    {
        tags[Category.Music] = entry.TargetGenres
            .Select(g => g.Genre.ToString())
            .Distinct()
            .ToList();
    }

    // Always add the primary category — unless genres already populated the same key.
    if (!tags.ContainsKey(entry.PrimaryCategory))
    {
        tags[entry.PrimaryCategory] = new[] { entry.PrimaryCategory.ToString() };
    }

    return new IngestEventRequest(
        FeedEntryId: entry.Id,
        Title: entry.Title,
        TextRepresentation: textRepresentation,
        EventUtc: entry.PublishedUtc,
        CategoryValues: tags);
}
```

Tag-string format is `{Category}.{Value}` (see `VectorDb/CategoryTagsHelper.cs`). Resulting `CategoryTags[]` examples:

| PrimaryCategory | TargetGenres | Tags emitted |
|---|---|---|
| `Weather` | (empty) | `["Weather.Weather"]` |
| `Music` | `[Rock, Techno]` | `["Music.Rock", "Music.Techno"]` |
| `Lineup` | `[Rock]` | `["Music.Rock", "Lineup.Lineup"]` |
| `Music` | (empty) | `["Music.Music"]` |

Notes:
- Artist names deliberately excluded from `TextRepresentation` (brainstorm Q3: cleanest signal). They remain in the FeedEntry row for SSE targeting.
- `EventUtc` maps from `FeedEntry.PublishedUtc` (cast `DateTime` → `DateTimeOffset`; `PublishedUtc` is already in UTC per LiveFeed contract).
- `tags` dict always contains at least the `PrimaryCategory` key, so `CategoryValues` is never null at the call site.

## 7. Error handling

```csharp
private async Task SafeIngestEventAsync(FeedEntry entry, CancellationToken ct)
{
    try
    {
        await ingestService.IngestEventAsync(entry.ToIngestEventRequest(), ct);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        throw; // honor caller cancellation
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "Vector indexing failed for FeedEntry {FeedEntryId} after publish; entry is committed and broadcast, vector index will be stale until a future re-ingest.",
            entry.Id);
    }
}
```

| Failure | Behavior |
|---|---|
| OpenAI 429 / 5xx | Caught, warning logged, publish returns 201 |
| OpenAI auth failure | Caught, warning logged, publish returns 201 |
| `VectorDbContext.SaveChangesAsync` failure | Caught, warning logged, publish returns 201 |
| `OperationCanceledException` from caller-supplied `ct` | Re-thrown — request is already cancelling |
| Idempotent dedupe (re-call same FeedEntryId) | Silent no-op (handled inside `IngestService`, no log noise) |

Rationale: per brainstorm Q2, publish availability must not be coupled to OpenAI availability. The broadcast has already shipped to subscribers when the embed call begins; an exception here would only corrupt the response, never undo the SSE event.

## 8. Tests — `FeedVectorIndexTests.cs`

Both tests reuse the existing `FeedApiFactory` collection fixture (`[Collection("Postgres")]`).

**Test 1 — `Publishing_an_entry_persists_an_EventEntry_in_VectorDb`**

1. `await Factory.ResetDatabaseAsync()` + `await Factory.ResetVectorEventsAsync()`.
2. POST `/api/v1/feed` as `UserRole.Organizer` with a known payload (title="Stage delay", body="Main Stage delayed 30 min", primaryCategory=`Music`, targetGenres=[`Rock`, `Techno`]).
3. Assert 201 + extract `dto.Id`.
4. Open a `VectorDbContext` scope, query `EventEntries.SingleAsync(e => e.FeedEntryId == dto.Id)`.
5. Assert: `Title == "Stage delay"`, `TextRepresentation == "Stage delay\n\nMain Stage delayed 30 min"`, `Embedding.Memory.Length == 32` (FakeEmbeddingService dim), `EventUtc == dto.PublishedUtc`, `CategoryTags` equals `["Music.Rock", "Music.Techno"]` (order-insensitive — assert via `Should().BeEquivalentTo(...)` or `HashSet` comparison). With `PrimaryCategory=Music` plus those genres, the Music key is populated from genres and the primary key is skipped per §6 mapping.

**Test 2 — `Publishing_returns_201_and_broadcasts_even_when_embedding_throws`**

1. Create a dedicated `FeedApiFactory` subclass (or a per-test `WithWebHostBuilder(ConfigureTestServices(...))` override) that registers `ThrowingEmbeddingService` in place of the default.
2. `await Factory.ResetDatabaseAsync()` + `await Factory.ResetVectorEventsAsync()`.
3. Open an SSE subscriber on a parallel `HttpClient` (matches the pattern already used by `FeedSseTests`).
4. POST `/api/v1/feed` as Organizer.
5. Assert 201.
6. Assert the subscriber received a `feed.created` envelope (broadcast happens before the throw).
7. Assert `VectorDbContext.EventEntries.AnyAsync()` is `false`.

**Regression:** the existing 22 LiveFeed integration tests + 16 unit tests must still pass.

## 9. Idempotency + ordering

- `IngestService.IngestEventAsync` opens with `if (await eventRepository.AnyAsync(EventByFeedEntryIdSpec, ct)) return;` — safe to retry.
- `IIngestService` is registered scoped (VectorDb module); `FeedService` is scoped — no captive-dependency hazard. No `IServiceScopeFactory` needed (we are in a request scope, not in the singleton `FeedBroadcaster`).
- Two separate DbContexts: `FeedDbContext.SaveChangesAsync` commits FeedEntry; `VectorDbContext.SaveChangesAsync` commits EventEntry. No distributed transaction. Eventual consistency by design (window: ms-to-seconds depending on embed latency).
- Broadcast precedes embed → subscribers see the entry before the vector index converges. Acceptable: SSE is the user-visible surface; vector search is opportunistic.

## 10. Known limitations (documented)

- **Update propagation absent.** Editing a FeedEntry's title/body changes the FeedDbContext row but leaves the cached `Embedding` and `TextRepresentation` in `vector.event_entries` stale until a future re-ingest job. Documented in this spec and in LiveFeed README §14 (future plan slot).
- **Soft-delete leaves orphan vector rows.** Same root cause. A soft-deleted FeedEntry is hidden from `GET /api/v1/feed` but its `EventEntry` row continues to surface from `IVectorSearchService.SearchEventsAsync`. Mitigated only by a future cleanup pass.
- **Embed latency on publish.** `IngestEventAsync` is awaited synchronously after the broadcast. OpenAI `text-embedding-3-small` typically 100-400 ms; visible in `POST /api/v1/feed` p99. Acceptable for hackathon scale.
- **No retry on embed failure.** Failures log + swallow. A future hosted background reconciler can scan FeedEntries with no matching EventEntry row and re-ingest.

## 11. Acceptance criteria

- `PublishEntryAsync` writes one `vector.event_entries` row per non-duplicate FeedEntry.
- Embedding-service exceptions never fail `POST /api/v1/feed` and never suppress the SSE broadcast.
- Two new integration tests pass.
- All 70 existing tests still pass.
- `dotnet build` produces zero new warnings in `Reshape.ElectricAi.LiveFeed.*` projects.
- CODE.md compliance verified by review-agent dispatch (CLAUDE.md Phase 4 requirement: review agent receives explicit "verify CODE.md compliance" directive).

## 12. Out of band — VectorDb-side cleanup (future)

If/when the user approves modifying `IIngestService` + `IngestService`:
- Add `UpdateEventAsync(IngestEventRequest)` — re-embed in place, update row.
- Add `RemoveEventAsync(Guid feedEntryId)` — hard-delete the EventEntry row.
- Wire both from `FeedService.UpdateEntryByIdAsync` / `SoftDeleteEntryByIdAsync` behind the same `Safe*Async` swallow pattern.

Tracked as the natural follow-up. Not in this plan.
