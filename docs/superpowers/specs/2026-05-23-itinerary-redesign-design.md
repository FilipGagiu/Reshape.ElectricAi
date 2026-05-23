# Itinerary Redesign — Single-Roundtrip AI Extraction + Section Pipeline

**Date:** 2026-05-23
**Status:** Draft — awaiting user review
**Supersedes (partially):** `2026-05-23-ai-plan-generation-design.md`, `2026-05-23-plans-models-design.md` (the parts dealing with `Plan` content shape, `GroupPreferences`, and LLM-driven plan generation)

---

## 1. Context & motivation

The current flow ships free-text wizard answers + free-text notes to OpenAI, expecting a single envelope back containing **both** extracted user preferences **and** a fully-composed plan (days, food list, transport legs, budget). The plan envelope is large, expensive, and brittle, and it does not allow the user to review/edit the extracted preferences before the costly plan-generation pass.

The new model:

- The AI's only job is to extract structured user preferences from the wizard answers + free text. One LLM round-trip per generation.
- The backend deterministically enriches those preferences via the existing VectorDb (activities, food, artists) and assembles a composite **itinerary snapshot** through a section pipeline.
- The snapshot is persisted (overwrite on regenerate) so reads are fast.
- The `Group` entity stays for social/membership, but groups no longer drive plan generation. "Crew" context is extracted into `UserPreferences` scalars.
- The contract is built for extensibility: new sections (e.g. Weather, Safety, Rules) become single-class additions.

## 2. Scope

### In scope

- New `POST /api/v1/itinerary/generate` endpoint (replaces `POST /api/v1/plans/generate`).
- Reshaped `PUT/PATCH/GET /api/v1/preferences` endpoints (kept; behavior extended).
- New `GET /api/v1/itinerary` endpoint.
- `UserPreferences` schema extension (Name, Origin, Crew, vibe tags, transport/accommodation notes).
- `Plan` entity repurposed as an itinerary snapshot (jsonb holds `ItineraryDto`).
- Drop `GroupPreferences` + 5 group-preference child tables + 3 group-preference endpoints + `Plan.GroupId`.
- New `IItinerarySection` pipeline with 6 v1 sections: Greeting, Transport, VibeActivities, Food, TopArtists, Accommodation.
- New `IPreferencesExtractor` (single-purpose LLM call).
- New `IItineraryBuilder` orchestrator (parallel sections, fail-soft per section).
- Plans project takes a direct project reference on VectorDb (for `IVectorSearchService`).

### Out of scope

- Versioned plan history. One snapshot per user; overwrite on regen.
- Lazy snapshot refresh based on VectorDb data version.
- LLM-assisted slice editing (e.g. "change Friday night"). Refinement = re-`POST /itinerary/generate` with different free-text.
- Plan export functionality (the old `ExportedUtc` column is dropped).
- Anonymous itinerary generation (login required, status quo).
- Group-shared plans. Group entity stays social-only.

## 3. Architecture & data flow

```
┌──────────┐       ┌──────────────────────────────────────────┐       ┌─────────────┐
│   FE     │       │              Plans API                    │       │  OpenAI     │
└────┬─────┘       └────┬──────────────────────────────────┬───┘       └──────┬──────┘
     │                  │                                  │                  │
     │ POST /itinerary/generate { answers[], freeText? }   │                  │
     │─────────────────►│                                  │                  │
     │                  │ IPreferencesExtractor:           │                  │
     │                  │   build prompt + schema ─────────┼─────────────────►│
     │                  │                                  │◄─────────────────│
     │                  │ upsert UserPreferences           │                  │
     │                  │                                  │                  │
     │                  │ IItineraryBuilder:               │                  │
     │                  │   run all IItinerarySection      │                  │
     │                  │   in parallel:                   │                  │
     │                  │     GreetingSection (no I/O)     │                  │
     │                  │     TransportSection (no I/O)    │                  │
     │                  │     AccommodationSection (no I/O)│                  │
     │                  │     VibeActivitiesSection ───┐   │                  │
     │                  │     FoodSection ─────────────┼───┼──► VectorDb      │
     │                  │     TopArtistsSection ───────┘   │  (Documents +    │
     │                  │   assemble ItineraryDto          │   EventEntries)  │
     │                  │ upsert Plan snapshot (overwrite) │                  │
     │ 200 ItineraryDto │                                  │                  │
     │◄─────────────────│                                  │                  │
     │                                                     │                  │
     │ PUT /api/v1/preferences { knobs }                   │                  │
     │─────────────────►│ apply changes                    │                  │
     │                  │ IItineraryBuilder (no LLM)       │                  │
     │                  │ upsert Plan snapshot             │                  │
     │ 200 ItineraryDto │                                  │                  │
     │◄─────────────────│                                  │                  │
     │                                                     │                  │
     │ GET /api/v1/itinerary   ─►  returns persisted snapshot                 │
     │ GET /api/v1/preferences ─►  returns persisted prefs                    │
```

Key properties:

- **One LLM call per generate.** Extraction only. No plan generation by LLM.
- **Section enrichment is fast + deterministic.** Vector queries against existing `IVectorSearchService`.
- **Sections run in parallel** via `Task.WhenAll`. Independent. Any section can fail-soft (returns empty payload + diagnostic) without taking the whole snapshot down.
- **Snapshot = persisted `Plan` entity** (repurposed). `ContentJson` jsonb stores `ItineraryDto` (NOT the old `PlanDto` shape). Overwrite per user on each rebuild. Unique index on `Plan.OwnerUserId`.
- **Two write paths, both rebuild snapshot.** Generate (LLM + sections) and prefs PUT/PATCH (sections only).
- **Adding a section = adding one class + one DI line.** No existing-section touch.

## 4. API contract

### Endpoints

```
POST   /api/v1/itinerary/generate     [Authorize]    rate-limited per user
PUT    /api/v1/preferences            [Authorize]
PATCH  /api/v1/preferences            [Authorize]
GET    /api/v1/preferences            [Authorize]    200 (empty defaults if no row)
GET    /api/v1/itinerary              [Authorize]    200 snapshot or 404 itinerary-not-found
```

Dropped: `POST /api/v1/plans/generate`, group-preferences endpoints (`GET/PUT/PATCH /api/v1/groups/{id}/preferences`).
Kept untouched: existing group CRUD (`POST /groups`, `GET /groups/{id}`, member add/remove).

### Request DTOs

```jsonc
// POST /itinerary/generate body
{
  "version": 1,
  "locale": "en",
  "submittedAt": "2026-05-23T18:06:22.577Z",
  "answers": [
    { "question": "What should we call you?", "answer": "Paul", "answeredAt": "..." }
  ],
  "freeText": "optional extra notes or refinement feedback on regen"
}
```

- `version` is reserved for future schema migration. Must equal `1` in this release; otherwise 400 `unsupported-version`.
- `locale` is passed through to the LLM prompt (system prompt acknowledges it). Not persisted.
- `answeredAt` accepted but ignored on BE (not persisted, not surfaced).
- `answers` MAY be empty if `freeText` is non-empty. At least one source of input is required (validator rule).
- FE owns the question list; BE treats `question` strings as opaque.

```jsonc
// PUT /api/v1/preferences body (full replace)
{
  "name": "Paul",
  "origin": "Cluj",
  "crew": { "kind": "withGroup", "estimatedSize": 4 },
  "vibeTags": ["full row", "party"],
  "musicGenres": ["Pop", "Soul"],
  "mustSeeArtists": ["Teddy Swims"],
  "foodRestrictions": ["Vegetarian"],
  "activityInterests": ["FoodTour", "Workshop"],
  "suggestedTransport":   { "mode": "car",     "note": null },
  "suggestedAccommodation": { "type": "camping", "note": null },
  "ticketType": "Standard",
  "ageGroup": null
}
```

PATCH follows existing project STJ convention: `null` value = explicit clear in PUT, absent property in PATCH = no change. Validators mirror PUT shape.

### Response DTOs

```jsonc
// GET /api/v1/preferences AND PUT/PATCH /api/v1/preferences AND embedded in /itinerary/generate
{
  "preferences": { /* same shape as PUT body */ },
  "completionPercent": 80
}

// GET /api/v1/itinerary AND POST /api/v1/itinerary/generate
{
  "preferences": { /* PreferencesDto */ },
  "itinerary": {
    "generatedUtc": "2026-05-23T18:07:01Z",
    "sections": [
      { "key": "greeting",       "data": { "name": "Paul", "origin": "Cluj", "crew": { "kind": "withGroup", "size": 4 } } },
      { "key": "transport",      "data": { "mode": "car", "note": null } },
      { "key": "vibeActivities", "data": { "vibeTags": ["full row","party"], "topActivities": [ { "id": "...", "title": "...", "snippet": "...", "score": 0.83 } ] } },
      { "key": "food",           "data": { "restrictions": ["Vegetarian"], "topRestaurants": [ { "id": "...", "title": "...", "snippet": "...", "score": 0.78 } ] } },
      { "key": "topArtists",     "data": { "topOverall": [], "byDay": [ { "date": "2026-07-15", "artists": [] } ] } },
      { "key": "accommodation",  "data": { "type": "camping", "note": null } }
    ]
  }
}
```

- `sections[]` order is BE-controlled (display order).
- `key` is stable; FE switches renderer on `key`.
- `data` is polymorphic per `key`. FE knows the shape per key.
- A failed section still appears with `data: {}` and an optional `diagnostic` field (logged only; surfaced in dev/debug builds).

### `completionPercent`

The 9-dimension equal-weight integer-divide formula from current `IPreferencesService` is recomputed for the new field surface. New dimensions (count = 12):

1. `name` (scalar)
2. `origin` (scalar)
3. `crew.kind` (scalar)
4. `ticketType` (scalar)
5. `ageGroup` (scalar)
6. `suggestedTransport.mode` (scalar)
7. `suggestedAccommodation.type` (scalar)
8. `vibeTags` (list)
9. `musicGenres` (list)
10. `mustSeeArtists` (list)
11. `foodRestrictions` (list)
12. `activityInterests` (list)

Integer divide by 12. Existing helper renamed/adjusted.

### Error envelope

Standard project envelope `{ error: { code, message } }`. Codes:

| Code | HTTP | Trigger |
|---|---|---|
| `preferences-required` | 400 | empty `answers[]` AND empty `freeText` on `POST /itinerary/generate` |
| `unsupported-version` | 400 | `version != 1` |
| `validation-failed` | 400 | FluentValidation failure |
| `preferences-conflict` | 409 | `DbUpdateConcurrencyException` on `PUT/PATCH /preferences` |
| `itinerary-not-found` | 404 | `GET /api/v1/itinerary` when no snapshot exists |
| `llm-extraction-failed` | 502 | `LlmSchemaException` or `LlmException` during extraction |
| `rate-limit-exceeded` | 429 | rate limiter trip |

## 5. Persistence

### `UserPreferences` (Plans schema) — extended

| Column | Type | Source | Notes |
|---|---|---|---|
| UserId | uuid PK | existing | unchanged |
| UpdatedUtc | timestamptz | existing | unchanged |
| xmin | xid | existing | concurrency token, unchanged |
| TicketType | smallint (enum) NULL | existing | kept |
| Accommodation | smallint (enum) NULL | existing | kept; maps from `suggestedAccommodation.type` |
| **AccommodationNote** | text NULL (max 200) | NEW | `suggestedAccommodation.note` |
| Transport | smallint (enum) NULL | existing | kept; maps from `suggestedTransport.mode` |
| **TransportNote** | text NULL (max 200) | NEW | `suggestedTransport.note` |
| AgeGroup | smallint (enum) NULL | existing | kept |
| **Name** | text NULL (max 80) | NEW | wizard-extracted; takes precedence over `User.Name` for greeting |
| **Origin** | text NULL (max 120) | NEW | free-form city/region |
| **CrewKind** | smallint (enum: Solo, WithGroup) NULL | NEW | |
| **CrewEstimatedSize** | smallint NULL | NEW | only when `CrewKind = WithGroup` and LLM extracted a number |

Existing child tables kept (composite PK `(UserId, Value)` + cascade delete, per existing pattern):

- `UserPreferenceGenres` ← `musicGenres`
- `UserPreferenceFoodRestrictions` ← `foodRestrictions`
- `UserPreferenceActivities` ← `activityInterests`
- `UserPreferenceArtists` ← `mustSeeArtists`

New child table:

- `UserPreferenceVibeTags` (UserId, Value text, max 60 per tag, composite PK, FK cascade). Free-form LLM-extracted strings.

Dropped child table:

- `UserPreferenceCuisines` — not in new extraction surface; redundant with `vibeTags` + `foodRestrictions`.

### `Plan` (repurposed as itinerary snapshot)

| Column | Status | Notes |
|---|---|---|
| Id | kept | uuid PK |
| OwnerUserId | kept | uuid NOT NULL (was nullable). Add **unique index**. |
| ContentJson | kept | jsonb; now stores `ItineraryDto` (NOT old `PlanDto`) |
| GeneratedUtc | kept | timestamp of last snapshot rebuild |
| Scope | DROPPED | only one shape now |
| GroupId | DROPPED | + drop FK to Groups |
| TicketType | DROPPED | already on UserPreferences |
| Tip | DROPPED | not in new contract |
| ExportedUtc | DROPPED | export not in scope of this redesign |
| Owner (nav) | kept | |
| Group (nav) | DROPPED | |

### Dropped entirely

- `GroupPreferences` table
- `GroupPreferenceGenres`, `GroupPreferenceFoodRestrictions`, `GroupPreferenceActivities`, `GroupPreferenceArtists`, `GroupPreferenceCuisines` (5 child tables)
- `Group.Preferences` nav property
- `IGroupPreferencesService` + impl
- Group-preferences endpoints (3 endpoints in `GroupsController`)
- `PlanScope` enum (only one snapshot shape now)
- Old `PlanDayDto`, `PlanFoodDto`, `PlanBudgetDto`, `PlanConcertDto`, `PlanActivityDto`, `PlanTransportDto`, `PlanTransportLegDto`, `PlanDto`, `WizardAnswer.QuestionId`, `AiPlanEnvelope`, `AiPreferences`

### Migration plan

Single EF migration `RedesignItineraryModel`:

1. Drop FK `Plans.GroupId` → `Groups.Id`
2. **Wipe existing `Plans` rows**: `DELETE FROM plans."Plans"` in `Up()`. Old `ContentJson` shape is incompatible. Dev-only data; no prod yet.
3. Drop columns: `Plans.Scope`, `Plans.GroupId`, `Plans.TicketType`, `Plans.Tip`, `Plans.ExportedUtc`
4. Make `Plans.OwnerUserId` NOT NULL; create unique index `IX_Plans_OwnerUserId_Unique`
5. Add columns to `UserPreferences`: `AccommodationNote`, `TransportNote`, `Name`, `Origin`, `CrewKind`, `CrewEstimatedSize`
6. Create table `UserPreferenceVibeTags` (UserId uuid, Value text, composite PK, FK cascade)
7. Drop tables (in FK-safe order): `UserPreferenceCuisines`, `GroupPreferenceCuisines`, `GroupPreferenceArtists`, `GroupPreferenceActivities`, `GroupPreferenceFoodRestrictions`, `GroupPreferenceGenres`, `GroupPreferences`

No down-migration support; drops are destructive and dev DBs will be rebuilt.

Postgres identifiers stay PascalCase (per project convention). `xmin` concurrency token pattern unchanged.

## 6. Section pipeline

### Core contract

```csharp
// Reshape.ElectricAi.Core/Services/Itinerary/IItinerarySection.cs
public interface IItinerarySection
{
    string Key { get; }                  // stable identifier; FE switches on this
    int Order { get; }                   // BE-controlled display order
    Task<ItinerarySectionResult> BuildAsync(
        UserPreferencesSnapshot prefs,
        CancellationToken cancellationToken);
}

public sealed record UserPreferencesSnapshot(
    Guid UserId,
    string? Name,
    string? Origin,
    CrewKind? CrewKind,
    int? CrewEstimatedSize,
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<MusicGenre> MusicGenres,
    IReadOnlyList<string> MustSeeArtists,
    IReadOnlyList<FoodRestriction> FoodRestrictions,
    IReadOnlyList<ActivityInterest> ActivityInterests,
    TransportMode? TransportMode,
    string? TransportNote,
    AccommodationType? AccommodationType,
    string? AccommodationNote,
    TicketType? TicketType,
    AgeGroup? AgeGroup);

public sealed record ItinerarySectionResult(
    string Key,
    int Order,
    JsonNode Data,                       // section-specific payload
    string? Diagnostic);                 // null on success; set on fail-soft
```

The snapshot is immutable, built once per request from the EF entity. Sections do NOT see the DbContext — pure read of the snapshot + vector calls.

### Orchestrator

```csharp
// Reshape.ElectricAi.Plans/Services/Itinerary/ItineraryBuilder.cs
internal sealed partial class ItineraryBuilder(
    IEnumerable<IItinerarySection> sections,
    ILogger<ItineraryBuilder> logger) : IItineraryBuilder
{
    public async Task<ItineraryDto> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken ct)
    {
        var tasks = sections.Select(s => RunSafe(s, prefs, ct)).ToArray();
        var results = await Task.WhenAll(tasks);
        var ordered = results.OrderBy(r => r.Order).ToList();
        return new ItineraryDto(DateTime.UtcNow, ordered);
    }

    private async Task<ItinerarySectionResult> RunSafe(IItinerarySection s, UserPreferencesSnapshot p, CancellationToken ct)
    {
        try { return await s.BuildAsync(p, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            LogSectionFailed(logger, s.Key, ex);
            return new ItinerarySectionResult(s.Key, s.Order, JsonNode.Parse("{}")!, $"section-failed:{ex.GetType().Name}");
        }
    }
}
```

Parallel via `Task.WhenAll`. Per-section try/catch isolates failures. `OperationCanceledException` re-thrown so cancellation propagates. Order applied after collection. `LoggerMessage` source-gen (`partial` host) per existing project pattern.

### Six v1 sections

| Section | Key | Order | I/O | Behavior |
|---|---|---|---|---|
| `GreetingSection` | `greeting` | 10 | none | Emits `{ name, origin, crew: { kind, size? } }`. Pure read. |
| `TransportSection` | `transport` | 20 | none | Emits `{ mode, note }`. Pure read. |
| `VibeActivitiesSection` | `vibeActivities` | 30 | vector | Embed query = `string.Join(" ", vibeTags ++ activityInterests-as-text)`. `IVectorSearchService.SearchDocumentsAsync` with `UserContext = { Category.Activity: [...] }`, `TopK=5`. Emits `{ vibeTags, topActivities: [{id, title, snippet, score}] }`. |
| `FoodSection` | `food` | 40 | vector | Embed query = restrictions as natural-language predicate (e.g. `"vegetarian friendly restaurant"`). Filter `Category.Food`. `TopK=5`. Emits `{ restrictions, topRestaurants: [...] }`. |
| `TopArtistsSection` | `topArtists` | 50 | vector + db | Two passes: (a) must-see exact-match via `IEventLookupService.FindByTitlesAsync(IReadOnlyList<string> names, CancellationToken ct)` (case-insensitive `ILIKE` against `EventEntry.Title`); (b) `IVectorSearchService.SearchEventsAsync` with embed of genres + vibeTags, filter `Category.Music`, `TopK=30`. Merge: must-see first, dedup, group by `EventUtc.Date`, top-3 per day. `topOverall` = top-5 by score from merged set (must-see entries get an artificial top score so they always appear). Emits `{ topOverall, byDay: [{date, artists}] }`. |
| `AccommodationSection` | `accommodation` | 60 | none | Emits `{ type, note }`. Pure read. |

`ActivityInterest` and `FoodRestriction` enum values are converted to natural language via a small mapping helper for embedding (e.g. `FoodTour` → `"food tour"`). Helper lives in Plans next to the section classes.

### Extractor

```csharp
internal interface IPreferencesExtractor
{
    Task<AiExtractedPreferences> ExtractAsync(
        IReadOnlyList<WizardAnswer> answers,
        string? freeText,
        string locale,
        CancellationToken ct);
}
```

Single LLM call. `AiExtractedPreferences` mirrors the field surface in §4. Schema exported via `JsonSchemaExporter.GetJsonSchemaAsNode(LlmJsonOptions.Default, typeof(AiExtractedPreferences))` (per existing pattern; `DefaultJsonTypeInfoResolver` is part of `LlmJsonOptions.Default`). Result is merged into `UserPreferences` via `ApplyExtracted(...)` extension. Strict schema (`jsonSchemaIsStrict: true`); model = `gpt-4o-mini`; max tokens per `ItineraryGenerationOptions.MaxCompletionTokens` (default 1024).

System prompt embedded resource: `Reshape.ElectricAi.Plans.Services.Prompts.PreferencesExtractorSystemPrompt.md`.

### Top-level flow (controller endpoints)

```
POST /itinerary/generate:
  1. validate body (FluentValidation)
  2. rate-limit per user (IRateLimiter, key "itinerary-gen:{userId}")
  3. ai = await extractor.ExtractAsync(answers, freeText, locale, ct)
  4. tx:
     a. UserPreferences entity = upsertFromAi(ai, userId)
     b. snapshot = UserPreferencesSnapshot.From(entity)
     c. ItineraryDto = await itineraryBuilder.BuildAsync(snapshot, ct)
     d. upsert Plan entity (overwrite ContentJson)
     e. SaveChanges, Commit
  5. return { preferences, itinerary }

PUT/PATCH /preferences:
  1. validate body
  2. rate-limit per user (IRateLimiter, key "prefs-update:{userId}")
  3. tx:
     a. apply changes to UserPreferences entity (xmin checked by EF)
     b. snapshot = UserPreferencesSnapshot.From(entity)
     c. ItineraryDto = await itineraryBuilder.BuildAsync(snapshot, ct)
     d. upsert Plan entity
     e. SaveChanges, Commit
  3. return { preferences, itinerary }
```

Single transaction wraps prefs persistence + snapshot persistence (per existing `PlanGenerator` pattern: the paid LLM call has completed; never leave half on disk).

### DI wiring (PlansModule)

```csharp
services.AddScoped<IPreferencesExtractor, PreferencesExtractor>();
services.AddScoped<IItineraryBuilder, ItineraryBuilder>();
services.AddScoped<IItinerarySection, GreetingSection>();
services.AddScoped<IItinerarySection, TransportSection>();
services.AddScoped<IItinerarySection, AccommodationSection>();
services.AddScoped<IItinerarySection, VibeActivitiesSection>();
services.AddScoped<IItinerarySection, FoodSection>();
services.AddScoped<IItinerarySection, TopArtistsSection>();
```

`Reshape.ElectricAi.Plans.csproj` gains `<ProjectReference Include="..\Reshape.ElectricAi.VectorDb\Reshape.ElectricAi.VectorDb.csproj" />` (mirrors LiveFeed pattern; `IVectorSearchService` interface in Core, impl in VectorDb). `EventLookupService` is registered by `VectorDbModule` alongside the existing search service.

Adding a new section = one new class + one DI line. Removing a section = delete class + delete DI line.

## 7. Error handling & edge cases

### LLM extraction failures

| Failure | Handling | Code |
|---|---|---|
| OpenAI transient (timeout, 5xx, throttle) | Existing wrapper retry policy | n/a |
| Schema-invalid response | **Terminal** — throw `LlmSchemaException`. No retry (project rule: schema retries waste spend). | `llm-extraction-failed` |
| Empty payload (all fields null) | Accept — persist whatever extracted; sections fail-soft on missing data | n/a |
| `LlmException` | 502 | `llm-extraction-failed` |
| Cost guardrail trip | 429 via `IRateLimiter` | `rate-limit-exceeded` |

Transaction wraps prefs+snapshot AFTER the LLM call. If extraction throws, nothing persisted.

### Section failures (fail-soft)

`ItineraryBuilder.RunSafe` catches per-section exceptions. Section appears with `data: {}` and `diagnostic: "section-failed:<ExceptionType>"`. Logged via `LogSectionFailed`. Snapshot still committed.

### Empty/edge inputs

- Empty `answers[]` AND empty `freeText` → 400 `preferences-required`
- Empty `answers[]` but non-empty `freeText` → allowed; LLM extracts from free text
- User has no `UserPreferences` row yet → `GET /preferences` returns 200 empty-defaults (existing pattern)
- User has prefs but no snapshot → `GET /itinerary` returns 404 `itinerary-not-found`
- `mustSeeArtists` name with no `EventEntry` match → silently skipped by `TopArtistsSection`
- LLM emits unknown enum → strict schema prevents it; if slipped through, `ApplyExtracted` skips with warning log
- VectorDb has 0 results for a section → empty array, no diagnostic
- Lineup spans 1 day → `byDay` has 1 entry; `topOverall` still up to 5

### Rate limiting

- `POST /itinerary/generate`: `IRateLimiter`, key `itinerary-gen:{userId}`, default 10/hour (configurable: `ItineraryGeneration:RateLimit:PerHour`)
- `PUT/PATCH /preferences`: separate limiter, key `prefs-update:{userId}`, default 30/hour (configurable: `ItineraryGeneration:PrefsRateLimit:PerHour`). Prevents pref-edit spam from hammering VectorDb.
- `GET /preferences`, `GET /itinerary`: no rate limit

### Concurrency

- `UserPreferences.xmin` concurrency token in place. Concurrent `PUT /preferences` → 409 `preferences-conflict`. FE retries with fresh `GET`.
- `Plan` snapshot: no concurrency token. Last-write-wins is the contract.

### Auth + authorization

- All endpoints `[Authorize]`
- `userId` from `JwtRegisteredClaimNames.Sub` (with `ClaimTypes.NameIdentifier` fallback per existing pattern)
- No cross-user reads

### Cost / telemetry

`POST /itinerary/generate` logs (via `LoggerMessage` source-gen): `userId, answersCount, freeTextLength, llmPromptTokens, llmCompletionTokens, costCents, sectionsBuilt, sectionsFailed, totalDurationMs`. Section failures logged with section key + exception type.

### Group endpoint breaking change

Removing group-preferences endpoints is a FE-breaking change. Plans-side: remove `IGroupPreferencesService` + impl + 3 controller actions in same PR. FE coordination: callout in PR description; FE PR must land same day or have already removed the calls.

## 8. Testing strategy

### Unit tests (`Reshape.ElectricAi.Plans.Tests`)

| Target | Cases |
|---|---|
| `PreferencesExtractor` | `FakeOpenAiClient` round-trips canned `AiExtractedPreferences`. Assert field mapping: name from wizard, vibeTags preserved, enum strings parsed. Schema-violation → `LlmSchemaException`. Empty answers + null freeText → handled by validator (separate test). |
| `ItineraryBuilder` | Inject `IItinerarySection[]` fakes. Assert: all sections run; parallel (semaphore detects concurrency); order applied post-collection; one throwing section → snapshot still committed with diagnostic; `OperationCanceledException` re-thrown. |
| `GreetingSection`, `TransportSection`, `AccommodationSection` | Pure-read: snapshot in → expected JSON out. Cover null fields, solo vs withGroup. |
| `VibeActivitiesSection`, `FoodSection` | Fake `IVectorSearchService` returns canned `RetrievedChunk[]`. Assert embed query composition, category filter, top-5 slice, score passthrough. Empty result → empty array. |
| `TopArtistsSection` | Fake `IVectorSearchService` + fake `IEventLookupService`. Assert: must-see always in `topOverall`; byDay grouping by `EventUtc.Date`; top-3 per day; dedup between must-see + vector; date ordering. |
| `UserPreferencesMappingExtensions` | `ApplyExtracted(AiExtractedPreferences)` upserts scalars + replaces child collections. Cover: existing entity with prior values, brand-new entity, null fields preserve existing values. |

`PreferencesExtractor` test setup mirrors existing `PlanGenerator` tests: serialize-then-deserialize the queued envelope via STJ (per memory: direct anonymous-type cast to `LlmStructuredResult<AiExtractedPreferences>` is invalid).

### Integration tests (`Reshape.ElectricAi.Plans.Tests`)

Existing `AuthApiFactory : WebApplicationFactory<Program>` with env-var injection in `CreateHost(IHostBuilder)`.

| Endpoint | Cases |
|---|---|
| `POST /itinerary/generate` | (a) happy path — fakes inject canned LLM + canned VectorDb; assert 200, prefs persisted, snapshot persisted, all 6 sections present. (b) empty answers + empty freeText → 400. (c) LLM schema fail → 502, NO row written. (d) rate-limit trip → 429. (e) unauth → 401. (f) regen twice → second overwrites snapshot, no duplicate Plan row (unique index). |
| `PUT /preferences` | (a) happy path — body in, prefs replaced, snapshot rebuilt, NO LLM call (fake asserts 0 calls). (b) stale xmin → 409. (c) unauth → 401. |
| `PATCH /preferences` | Null fields = no change; non-null fields applied. Snapshot rebuilt. |
| `GET /preferences` | (a) no row → empty-defaults 200. (b) populated → DTO. |
| `GET /itinerary` | (a) no snapshot → 404. (b) present → 200. |

### Test VectorDb data

VectorDb test seeding helper `SeedTestVectorDataAsync` in fixture: 3 `EventEntry` rows (different dates, different artists), 3 `DocumentChunk` rows with `Activity.*` tags, 3 with `Food.*` tags. Embeddings = deterministic 1536-dim arrays from `FakeEmbeddingService` (no real OpenAI calls).

### Reset strategy

Per project rule: **NO `EnsureDeletedAsync`** in `WebApplicationFactory` fixtures (Npgsql pool 57P01 race). Use `TRUNCATE plans."Plans", plans."UserPreferences", plans."UserPreferenceVibeTags", plans."UserPreferenceGenres", plans."UserPreferenceFoodRestrictions", plans."UserPreferenceActivities", plans."UserPreferenceArtists" RESTART IDENTITY CASCADE`. VectorDb schema tables truncated similarly. Migrations run at host startup under `Development | Testing` gate; fixture uses `UseEnvironment("Testing")` (skips real EC embeddings seeder).

### Env vars for test fixtures

Set in `CreateHost(IHostBuilder)` BEFORE `base.CreateHost(builder)` (per memory pattern):

```
Auth__JwtSigningKey = "<32+ byte test key>"
ConnectionStrings__Plans = "<pg container conn>"
ConnectionStrings__VectorDb = "<pg container conn>"
OpenAi__ApiKey = "test-key"
OpenAi__Models__gpt-4o-mini__PromptCentsPer1K = "0.015"
OpenAi__Models__gpt-4o-mini__CompletionCentsPer1K = "0.060"
```

Cleared in `DisposeAsync` override.

### Test csproj warnings

`<WarningsNotAsErrors>` keeps existing: `CS1591;CA1707;CA1515;CA2007;CA1812;CA1711;CA1001;CA1819;CA1062;CA1024;CA1822;CA1861`. No changes.

### Race-condition tests

Not required for v1. Extraction is per-user serialized via rate limiter; snapshot upsert is single-row by `OwnerUserId` unique index.

### Coverage target

No formal threshold (repo doesn't enforce). Every endpoint has happy path + at least one error path. Every section class has at least one unit test.

## 9. Configuration

New options section, bound at startup:

```jsonc
// appsettings.json
"ItineraryGeneration": {
  "Model": "gpt-4o-mini",
  "MaxCompletionTokens": 1024,
  "Temperature": 0.2,
  "RateLimit":      { "PerHour": 10 },
  "PrefsRateLimit": { "PerHour": 30 },
  "Sections": {
    "VibeActivities": { "TopK": 5 },
    "Food":           { "TopK": 5 },
    "TopArtists":     { "VectorTopK": 30, "PerDay": 3, "Overall": 5 }
  }
}
```

Bound via `services.AddOptions<ItineraryGenerationOptions>().Bind(configuration.GetSection(ItineraryGenerationOptions.SectionName)).Validate(...).ValidateOnStart()`. Requires `Microsoft.Extensions.Options.ConfigurationExtensions` on Plans csproj (already present per memory).

## 10. Out-of-scope follow-ups (intentional)

- Snapshot version stamping for lazy refresh on VectorDb data changes
- LLM-assisted slice editing (per-day refinement)
- Plan export (PDF, calendar, share link)
- Group-shared itineraries (multiple users → one snapshot)
- Anonymous itinerary generation
- A/B testing infrastructure for prompt variants
- i18n of section payloads (BE currently emits raw fields; locale affects only LLM prompt; FE handles display text)

## 11. Files touched (summary)

### Added

- `src/Reshape.ElectricAi.Core/Services/Itinerary/IItinerarySection.cs`
- `src/Reshape.ElectricAi.Core/Services/Itinerary/IItineraryBuilder.cs`
- `src/Reshape.ElectricAi.Core/Services/Itinerary/IPreferencesExtractor.cs`
- `src/Reshape.ElectricAi.Core/Services/Itinerary/IEventLookupService.cs`
- `src/Reshape.ElectricAi.VectorDb/Services/EventLookupService.cs`
- `src/Reshape.ElectricAi.Core/Dtos/Itinerary/*.cs` (`ItineraryDto`, `ItinerarySectionDto`, `UserPreferencesSnapshot`, plus per-section data DTOs)
- `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesDto.cs` (reshaped; or extended in place)
- `src/Reshape.ElectricAi.Core/Dtos/Preferences/AiExtractedPreferences.cs`
- `src/Reshape.ElectricAi.Core/Enums/CrewKind.cs`
- `src/Reshape.ElectricAi.Plans/Services/Itinerary/ItineraryBuilder.cs`
- `src/Reshape.ElectricAi.Plans/Services/Itinerary/PreferencesExtractor.cs`
- `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/{Greeting,Transport,Accommodation,VibeActivities,Food,TopArtists}Section.cs`
- `src/Reshape.ElectricAi.Plans/Services/Itinerary/EnumNaturalLanguage.cs`
- `src/Reshape.ElectricAi.Plans/Services/Prompts/PreferencesExtractorSystemPrompt.md` (embedded resource)
- `src/Reshape.ElectricAi.Plans/Entities/UserPreferenceVibeTag.cs`
- `src/Reshape.ElectricAi.Plans/Configuration/ItineraryGenerationOptions.cs`
- `src/Reshape.ElectricAi.Plans/Persistence/Configurations/UserPreferenceVibeTagConfiguration.cs`
- `src/Reshape.ElectricAi.Plans/Migrations/<timestamp>_RedesignItineraryModel.cs`
- `src/Reshape.ElectricAi.Plans/Validators/{ItineraryGenerationRequestValidator,PreferencesReplaceRequestValidator,PreferencesPatchRequestValidator}.cs` (reshape existing)
- `src/Reshape.ElectricAi.Presentation/Controllers/V1/ItineraryController.cs`
- `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/*` (extractor, builder, each section)
- `tests/Reshape.ElectricAi.Plans.Tests/Endpoints/ItineraryEndpointTests.cs`
- `tests/Reshape.ElectricAi.Plans.Tests/Endpoints/PreferencesEndpointTests.cs` (reshape existing if present)

### Modified

- `src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj` — add `ProjectReference` to VectorDb
- `src/Reshape.ElectricAi.Plans/Entities/UserPreferences.cs` — new columns + new nav
- `src/Reshape.ElectricAi.Plans/Entities/Plan.cs` — drop columns
- `src/Reshape.ElectricAi.Plans/Persistence/Configurations/UserPreferencesConfiguration.cs`
- `src/Reshape.ElectricAi.Plans/Persistence/Configurations/PlanConfiguration.cs`
- `src/Reshape.ElectricAi.Plans/Persistence/PlansDbContext.cs` — register new DbSet, drop GroupPreferences DbSet
- `src/Reshape.ElectricAi.Plans/PlansModule.cs` — DI for new services + sections; drop `IGroupPreferencesService`
- `src/Reshape.ElectricAi.Plans/Services/PreferencesService.cs` — adjust mapping for new fields, drop GroupPreferences logic; `completionPercent` recomputed (12 dims)
- `src/Reshape.ElectricAi.Plans/Extensions/PreferencesMappingExtensions.cs` — add `ApplyExtracted`, drop cuisine handling
- `src/Reshape.ElectricAi.Plans/Services/GroupService.cs` — keep group CRUD untouched; remove any GroupPreferences couplings
- `src/Reshape.ElectricAi.Core/Dtos/Plans/*` — DELETE old `PlanDayDto/PlanFoodDto/PlanBudgetDto/...` and `AiPlanEnvelope`
- `src/Reshape.ElectricAi.Presentation/Controllers/V1/PlansController.cs` — drop `POST /plans/generate`; possibly delete entire controller if no other actions remain
- `src/Reshape.ElectricAi.Presentation/Controllers/V1/GroupsController.cs` — remove group-preferences actions

### Deleted

- `src/Reshape.ElectricAi.Plans/Services/PlanGenerator.cs`
- `src/Reshape.ElectricAi.Plans/Services/Generation/AiPlanEnvelope.cs`, `AiPreferences.cs`
- `src/Reshape.ElectricAi.Plans/Configuration/PlanGenerationOptions.cs`
- `src/Reshape.ElectricAi.Plans/Services/Prompts/PlanGeneratorSystemPrompt.md`
- `src/Reshape.ElectricAi.Plans/Services/IGroupPreferencesService.cs` + impl
- `src/Reshape.ElectricAi.Plans/Validators/PlanGenerationRequestValidator.cs`
- `src/Reshape.ElectricAi.Plans/Entities/GroupPreferences.cs`, `GroupPreferenceGenre.cs`, `GroupPreferenceFoodRestriction.cs`, `GroupPreferenceActivity.cs`, `GroupPreferenceArtist.cs`, `GroupPreferenceCuisine.cs`
- `src/Reshape.ElectricAi.Plans/Entities/UserPreferenceCuisine.cs`
- `src/Reshape.ElectricAi.Plans/Persistence/Configurations/{GroupPreferences,GroupPreferenceGenre,GroupPreferenceFoodRestriction,GroupPreferenceActivity,GroupPreferenceArtist,GroupPreferenceCuisine,UserPreferenceCuisine}Configuration.cs`
- `src/Reshape.ElectricAi.Core/Enums/Cuisine.cs` (if no other consumer)
- `src/Reshape.ElectricAi.Core/Enums/PlanScope.cs`

### Migration

- `src/Reshape.ElectricAi.Plans/Migrations/<timestamp>_RedesignItineraryModel.cs` (new)
- `src/Reshape.ElectricAi.Plans/Migrations/PlansDbContextModelSnapshot.cs` (regenerated)

## 12. Risks

- **FE breakage from removing `POST /plans/generate` and group-prefs endpoints.** Mitigation: same-PR coordination with FE owner.
- **VectorDb seeding insufficient for sections to return meaningful results in dev/staging.** Mitigation: extend `EcDataSeeder` to include `Activity.*` and `Food.*` documents (out of scope for this PR but called out).
- **LLM mis-extraction of free-form fields (vibeTags, mustSeeArtists, origin) producing noisy data.** Mitigation: strict JSON schema; per-field max lengths enforced server-side; user can correct via `PUT /preferences`.
- **`mustSeeArtists` exact-match via `ILIKE` is case-sensitive on some Postgres collations.** Mitigation: use `ILIKE` (case-insensitive) + `unaccent` extension if needed; for v1, accept the limitation and revisit if FE feedback shows misses.
- **Snapshot rebuild on every prefs PATCH may be expensive if user does many small edits.** Mitigation: rate limit (`prefs-update:{userId}` 30/hour); rebuild is still O(seconds) — acceptable.
- **Cross-project ref `Plans → VectorDb` creates a new dependency edge.** Mitigation: matches existing LiveFeed pattern; `IVectorSearchService` is on Core so the consumer only takes a runtime ref. Could extract a thinner contract later if churn becomes painful.

---

*End of spec.*
