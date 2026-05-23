# Itinerary Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **NO AUTO-COMMITS** (project rule, see CLAUDE.md memory). Every slice ends "STAGED — request user commit". User commits manually.

## Non-negotiable phase list (verbatim from CLAUDE.md, required at top of every plan)

> **The following phases are NON-NEGOTIABLE when starting a task that would result in code modification:**
>
> 1. **Invoke task-specific superpowers skill(s)** — match the task to a skill from §7 of CLAUDE.md. Fire BEFORE entering plan mode. Named mappings:
>    - New feature / behavior change → `superpowers:brainstorming`
>    - Bug, test failure, unexpected behavior, build failure → `superpowers:systematic-debugging`
>    - Implementation that admits unit tests → `superpowers:test-driven-development`
>    - About to claim "done" / "fixed" / "passing" → `superpowers:verification-before-completion`
>
>    If none of the named mappings fit, scan the full installed superpowers skill list for any skill that might help. If still nothing fits, proceed without one — that's an acceptable outcome, but document the full-list-scan result in the plan's Phase 1 application note. Silent skipping is not acceptable.
> 2. **Enter plan mode** (`EnterPlanMode`) — before ANY file edit. No exceptions for "small" or "trivial".
> 3. **Inventory / explore** — gather facts via Explore agents (parallel where useful) or direct reads. Do not guess.
> 4. **Design** — propose specific custom agents for review, exploration, or design feedback (NOT implementation — see §2 of CLAUDE.md). Review-agent dispatches MUST include "verify CODE.md compliance against the changed files" as an explicit directive. Recommend; do not decide unilaterally.
> 5. **Write the plan** to `.claude/plans/<slug>.md`. **Every plan MUST start by restating this phase list verbatim** so no phase is silently skipped.
> 6. **`ExitPlanMode`** — the single approval gate. Wait for explicit user approval.
> 7. **Execute** — YOU edit the files; only dispatch agents for review or parallel exploration. **Re-read [CODE.md](CODE.md) before each code edit** and verify the change honors every rule there. After approval.
> 8. **Verify** — build + tests + visible evidence. No "trust me" claims.
> 9. **Promote learnings to memory** — `/si:remember` for facts; direct-edit CODE.md (code rules), CLAUDE.md (workflow), or PROJECT.md (project context) for enforced rules. Penultimate step.
> 10. **Delete the plan file** — last step. Code + commit history is the source of truth after.

Phase 1 application note: `superpowers:brainstorming` was used to produce [docs/superpowers/specs/2026-05-23-itinerary-redesign-design.md](docs/superpowers/specs/2026-05-23-itinerary-redesign-design.md). `superpowers:test-driven-development` applies inside every implementation slice. `superpowers:verification-before-completion` applies at the Verify slice.

---

**Goal:** Replace the current "wizard answers → LLM emits prefs + full plan in one envelope" flow with: single-LLM-call preferences extraction, deterministic VectorDb-driven section pipeline, persisted itinerary snapshot (overwritten on regen), drop group-prefs entirely, repurpose `Plan` entity as snapshot store.

**Architecture:** `IPreferencesExtractor` is the only LLM consumer. `IItineraryBuilder` runs a fan-out of `IItinerarySection` impls in parallel (6 v1 sections — Greeting, Transport, VibeActivities, Food, TopArtists, Accommodation). Three sections enrich via existing `IVectorSearchService` + a new `IEventLookupService`. Snapshot persists into `Plan.ContentJson` (overwrite, unique index on `OwnerUserId`). Two write endpoints both rebuild the snapshot (`POST /itinerary/generate` does LLM+sections; `PUT/PATCH /preferences` does sections only).

**Tech Stack:** .NET 10, ASP.NET Core controllers, EF Core 10 + Npgsql, Pgvector, OpenAI SDK 2.10.0 (`gpt-4o-mini`, structured output via JsonSchemaExporter), FluentValidation 12, xUnit + Testcontainers.PostgreSql v4, `WebApplicationFactory<Program>` integration tests.

**Source spec:** [docs/superpowers/specs/2026-05-23-itinerary-redesign-design.md](docs/superpowers/specs/2026-05-23-itinerary-redesign-design.md)

---

## File map

### Created

| Path | Purpose |
|---|---|
| `src/Reshape.ElectricAi.Core/Enums/CrewKind.cs` | Solo \| WithGroup enum |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/AiExtractedPreferences.cs` | LLM response schema |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/AiExtractedCrew.cs` | nested DTO |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/AiExtractedTransportSuggestion.cs` | nested DTO |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/AiExtractedAccommodationSuggestion.cs` | nested DTO |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItineraryDto.cs` | top-level itinerary snapshot DTO |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItinerarySectionDto.cs` | { key, data, diagnostic? } wire shape |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/UserPreferencesSnapshot.cs` | immutable snapshot passed to sections |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItinerarySectionResult.cs` | section-internal result before serialization |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItineraryGenerationRequest.cs` | POST body DTO |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItineraryResponse.cs` | { preferences, itinerary } wrapper |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/GreetingSectionData.cs` | typed data payload |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/TransportSectionData.cs` | typed data payload |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/AccommodationSectionData.cs` | typed data payload |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/VibeActivitiesSectionData.cs` | typed data payload |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/FoodSectionData.cs` | typed data payload |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/TopArtistsSectionData.cs` | typed data payload |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/RecommendedActivityDto.cs` | item shape used by Activities + Food |
| `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/RecommendedArtistDto.cs` | item shape used by TopArtists |
| `src/Reshape.ElectricAi.Core/Services/Itinerary/IItinerarySection.cs` | section contract |
| `src/Reshape.ElectricAi.Core/Services/Itinerary/IItineraryBuilder.cs` | orchestrator contract |
| `src/Reshape.ElectricAi.Core/Services/Itinerary/IPreferencesExtractor.cs` | LLM extractor contract |
| `src/Reshape.ElectricAi.Core/Services/Itinerary/IEventLookupService.cs` | exact-match EventEntry lookup |
| `src/Reshape.ElectricAi.Plans/Configuration/ItineraryGenerationOptions.cs` | bound Options class |
| `src/Reshape.ElectricAi.Plans/Entities/UserPreferenceVibeTag.cs` | new child entity |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/UserPreferenceVibeTagConfiguration.cs` | EF config |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/PreferencesExtractor.cs` | LLM extractor impl |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/ItineraryBuilder.cs` | orchestrator impl |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/EnumNaturalLanguage.cs` | enum → natural-language helper for embeddings |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/GreetingSection.cs` | no-IO section |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/TransportSection.cs` | no-IO section |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/AccommodationSection.cs` | no-IO section |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/VibeActivitiesSection.cs` | VectorDb section |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/FoodSection.cs` | VectorDb section |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/TopArtistsSection.cs` | VectorDb + lookup section |
| `src/Reshape.ElectricAi.Plans/Services/Itinerary/ItineraryService.cs` | composes extractor + builder + persistence for POST/GET endpoints |
| `src/Reshape.ElectricAi.Plans/Services/Prompts/PreferencesExtractorSystemPrompt.md` | embedded LLM system prompt |
| `src/Reshape.ElectricAi.Plans/Validators/ItineraryGenerationRequestValidator.cs` | FluentValidation rules |
| `src/Reshape.ElectricAi.Plans/Migrations/<timestamp>_RedesignItineraryModel.cs` | EF migration |
| `src/Reshape.ElectricAi.VectorDb/Services/EventLookupService.cs` | IEventLookupService impl |
| `src/Reshape.ElectricAi.Presentation/Controllers/V1/ItineraryController.cs` | new controller |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/PreferencesExtractorTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/ItineraryBuilderTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/Sections/GreetingSectionTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/Sections/TransportSectionTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/Sections/AccommodationSectionTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/Sections/VibeActivitiesSectionTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/Sections/FoodSectionTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/Sections/TopArtistsSectionTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/EnumNaturalLanguageTests.cs` | unit |
| `tests/Reshape.ElectricAi.Plans.Tests/Endpoints/ItineraryEndpointTests.cs` | integration |
| `tests/Reshape.ElectricAi.Plans.Tests/Fakes/FakeVectorSearchService.cs` | testing fake |
| `tests/Reshape.ElectricAi.Plans.Tests/Fakes/FakeEventLookupService.cs` | testing fake |

### Modified

| Path | Change |
|---|---|
| `src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj` | ProjectReference to VectorDb; add `Microsoft.Extensions.Options.ConfigurationExtensions` if not already there |
| `src/Reshape.ElectricAi.Plans/Entities/UserPreferences.cs` | Add Name, Origin, CrewKind, CrewEstimatedSize, AccommodationNote, TransportNote columns + `VibeTags` nav collection |
| `src/Reshape.ElectricAi.Plans/Entities/Plan.cs` | Drop Scope, GroupId, TicketType, Tip, ExportedUtc; OwnerUserId NOT NULL; drop Group nav |
| `src/Reshape.ElectricAi.Plans/Entities/Group.cs` | Drop `Preferences` nav property |
| `src/Reshape.ElectricAi.Plans/Persistence/PlansDbContext.cs` | Add `UserPreferenceVibeTags` DbSet; drop `GroupPreferences*` DbSets and `UserPreferenceCuisines` DbSet |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/UserPreferencesConfiguration.cs` | New columns + new child collection mapping |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/PlanConfiguration.cs` | Drop columns; unique index on OwnerUserId |
| `src/Reshape.ElectricAi.Plans/Migrations/PlansDbContextModelSnapshot.cs` | Regenerated by `dotnet ef` |
| `src/Reshape.ElectricAi.Plans/PlansModule.cs` | DI: register extractor, builder, sections, lookup service, options; drop GroupPreferences registrations and `PlanGenerator`-related DI; drop validator registrations for deleted DTOs |
| `src/Reshape.ElectricAi.Plans/Services/PreferencesService.cs` | New mapping for new fields; drop cuisine handling; recompute completionPercent (12 dims); rebuild snapshot on PUT/PATCH |
| `src/Reshape.ElectricAi.Plans/Extensions/PreferencesMappingExtensions.cs` | Add `ApplyExtracted(AiExtractedPreferences)`; update `ApplyReplace`/`ApplyPatch`/`ToDto` for new shape; drop cuisine code |
| `src/Reshape.ElectricAi.Plans/Services/GroupService.cs` | Remove any references to dropped `GroupPreferences` nav/entity |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesDto.cs` | New shape (name, origin, crew, vibeTags, suggestedTransport/Accommodation as objects) |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesReplaceRequest.cs` | New shape mirroring PreferencesDto |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesPatchRequest.cs` | New shape mirroring PreferencesDto with nullable semantics |
| `src/Reshape.ElectricAi.Core/Services/IPreferencesService.cs` | Method signatures unchanged; DTO shapes change |
| `src/Reshape.ElectricAi.Presentation/Controllers/V1/PreferencesController.cs` | Wire snapshot rebuild after each PUT/PATCH (delegate to `IItineraryService`) |
| `src/Reshape.ElectricAi.Presentation/Controllers/V1/PlansController.cs` | Remove `POST /plans/generate` (and the controller if no actions left) |
| `src/Reshape.ElectricAi.Presentation/Controllers/V1/GroupsController.cs` | Remove `GET/PUT/PATCH /groups/{id}/preferences` actions |
| `src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs` | Register `IEventLookupService` |
| `tests/Reshape.ElectricAi.Plans.Tests/Endpoints/PreferencesEndpointTests.cs` | Update for new DTO shape + snapshot-rebuild assertions |
| `tests/Reshape.ElectricAi.Plans.Tests/Endpoints/AuthApiFactory.cs` | Add VectorDb env vars + seed VectorDb test data helper |

### Deleted

| Path |
|---|
| `src/Reshape.ElectricAi.Plans/Services/PlanGenerator.cs` |
| `src/Reshape.ElectricAi.Plans/Services/Generation/AiPlanEnvelope.cs` |
| `src/Reshape.ElectricAi.Plans/Services/Generation/AiPreferences.cs` |
| `src/Reshape.ElectricAi.Plans/Services/Generation/AiPlanRoot.cs` (if exists) |
| `src/Reshape.ElectricAi.Plans/Configuration/PlanGenerationOptions.cs` |
| `src/Reshape.ElectricAi.Plans/Services/Prompts/PlanGeneratorSystemPrompt.md` |
| `src/Reshape.ElectricAi.Plans/Validators/PlanGenerationRequestValidator.cs` |
| `src/Reshape.ElectricAi.Plans/Services/IGroupPreferencesService.cs` (Core or Plans — wherever it lives) |
| `src/Reshape.ElectricAi.Plans/Services/GroupPreferencesService.cs` (or impl wherever it lives) |
| `src/Reshape.ElectricAi.Plans/Entities/GroupPreferences.cs` |
| `src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceGenre.cs` |
| `src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceFoodRestriction.cs` |
| `src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceActivity.cs` |
| `src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceArtist.cs` |
| `src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceCuisine.cs` |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferencesConfiguration.cs` |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceGenreConfiguration.cs` |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceFoodRestrictionConfiguration.cs` |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceActivityConfiguration.cs` |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceArtistConfiguration.cs` |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceCuisineConfiguration.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanDayDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanFoodDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanBudgetDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanConcertDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanActivityDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanTransportDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanTransportLegDto.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanGenerationRequest.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/PlanGenerationResult.cs` |
| `src/Reshape.ElectricAi.Core/Dtos/Plans/WizardAnswer.cs` (or move/reshape to `Dtos/Itinerary/WizardAnswer.cs` without QuestionId) |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesCuisinesDto.cs` (if exists) |
| `src/Reshape.ElectricAi.Core/Enums/PlanScope.cs` |

---

## Slice ordering rationale

The DB migration drops columns and tables that current `PlanGenerator`, `IGroupPreferencesService`, and the old plan-controller all read/write. So:

1. First, **delete the old code** so nothing else compiles against the dropped entities/columns (Slice 1).
2. Then **reshape entities + DTOs + migration** (Slice 2) — the build will be red after Slice 1 until 2 lands; Slice 2 is the same commit area to keep CI green.
3. Then **add new Core abstractions** (Slice 3).
4. Then **per-service implementations bottom-up**: extractor (4) → no-IO sections (5) → VectorDb sections (6) → builder orchestrator (7) → itinerary service composing all (8).
5. Then **wire the endpoints** (Slice 9).
6. Then **integration tests** (Slice 10).
7. Then **verify + memory promote + delete plan** (Slice 11).

Slices 1 + 2 land in the same commit to keep main green. After that each slice is its own commit.

---

## Slice 1+2: Demolition + Reshape (single commit)

**Files (modified):** UserPreferences entity, Plan entity, Group entity, PlansDbContext, UserPreferencesConfiguration, PlanConfiguration, PlansModule (DI), PreferencesMappingExtensions, PreferencesService, GroupService, GroupsController, PlansController, PreferencesController (snapshot wiring deferred to Slice 9), Core DTOs (PreferencesDto, PreferencesReplaceRequest, PreferencesPatchRequest, WizardAnswer if reshape).

**Files (created):** UserPreferenceVibeTag entity + config; new CrewKind enum; Migration `RedesignItineraryModel`.

**Files (deleted):** see file map.

### Task 1.1 — Re-read CODE.md, gather current state

- [ ] **Step 1: Re-read CODE.md** (CLAUDE.md Phase 7 requirement)

```bash
# in chat: Read CODE.md once at slice start; re-read before any specific file edit if you've lost track
```

- [ ] **Step 2: Find every reference to types we're about to drop**

```bash
# We need to know who imports PlanGenerator, AiPlanEnvelope, AiPreferences, IGroupPreferencesService,
# GroupPreferences*, UserPreferenceCuisine, PlanScope, PlanDayDto, PlanFoodDto, etc. so Slice 1+2
# leaves no compilation errors after demolition.
```

Use Grep:
- pattern: `PlanGenerator|AiPlanEnvelope|AiPreferences|IGroupPreferencesService|GroupPreferences|UserPreferenceCuisine|PlanScope|PlanDto|PlanDayDto|PlanFoodDto|PlanBudgetDto|PlanConcertDto|PlanActivityDto|PlanTransportDto|PlanGenerationRequest|PlanGenerationResult|WizardAnswer\.QuestionId`
- type: `cs`

Record the hit list; every reference must be deleted or updated in this slice.

### Task 1.2 — Delete dropped Plans-side code

- [ ] **Step 1: Delete files** (single Bash with `rm`)

```bash
rm src/Reshape.ElectricAi.Plans/Services/PlanGenerator.cs
rm src/Reshape.ElectricAi.Plans/Services/Generation/AiPlanEnvelope.cs
rm src/Reshape.ElectricAi.Plans/Services/Generation/AiPreferences.cs
rm src/Reshape.ElectricAi.Plans/Configuration/PlanGenerationOptions.cs
rm src/Reshape.ElectricAi.Plans/Services/Prompts/PlanGeneratorSystemPrompt.md
rm src/Reshape.ElectricAi.Plans/Validators/PlanGenerationRequestValidator.cs
```

(Also delete `Reshape.ElectricAi.Plans/Services/Generation/` if it becomes empty.)

Find + delete `IGroupPreferencesService` + impl (path depends on where it actually lives — Grep first):

```bash
# Likely paths:
rm src/Reshape.ElectricAi.Core/Services/IGroupPreferencesService.cs    # if in Core
rm src/Reshape.ElectricAi.Plans/Services/GroupPreferencesService.cs    # impl
```

Delete dropped entities + configs (after confirming Grep results show no remaining references):

```bash
rm src/Reshape.ElectricAi.Plans/Entities/GroupPreferences.cs
rm src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceGenre.cs
rm src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceFoodRestriction.cs
rm src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceActivity.cs
rm src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceArtist.cs
rm src/Reshape.ElectricAi.Plans/Entities/GroupPreferenceCuisine.cs

rm src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferencesConfiguration.cs
rm src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceGenreConfiguration.cs
rm src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceFoodRestrictionConfiguration.cs
rm src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceActivityConfiguration.cs
rm src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceArtistConfiguration.cs
rm src/Reshape.ElectricAi.Plans/Persistence/Configurations/GroupPreferenceCuisineConfiguration.cs
```

### Task 1.3 — Delete Core DTOs

- [ ] **Step 1: Delete old plan DTOs**

```bash
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanDayDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanFoodDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanBudgetDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanConcertDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanActivityDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanTransportDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanTransportLegDto.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanGenerationRequest.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/PlanGenerationResult.cs
rm src/Reshape.ElectricAi.Core/Dtos/Plans/WizardAnswer.cs   # will recreate in Slice 3 under Dtos/Itinerary
```

- [ ] **Step 2: Delete dropped enums** (check Grep first for `PlanScope`, `Cuisine`)

```bash
rm src/Reshape.ElectricAi.Core/Enums/PlanScope.cs
```

(`Cuisine` enum stays — FE asked us to keep cuisines in the Food section response; LLM extracts user's preferred cuisines into the existing `UserPreferenceCuisines` child table.)

- [ ] **Step 3: Delete `Reshape.ElectricAi.Core/Services/IGroupPreferencesService.cs`** if it lived in Core.

### Task 1.4 — Strip controller actions referencing dropped code

- [ ] **Step 1: PlansController** — delete the `POST /plans/generate` action and its constructor injection of `IPlanGenerator`. If no other actions remain, delete the controller file entirely.

- [ ] **Step 2: GroupsController** — delete the 3 group-preferences actions and the `IGroupPreferencesService` injection. Keep the other 4 group CRUD actions intact.

- [ ] **Step 3: PreferencesController** — keep PUT/PATCH/GET intact for now; snapshot wiring lands in Slice 9.

### Task 1.5 — Reshape `Plan` entity

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Entities/Plan.cs`** to:

```csharp
namespace Reshape.ElectricAi.Plans.Entities;

public sealed class Plan
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }            // NOT NULL
    public string ContentJson { get; set; } = "{}";  // jsonb; stores ItineraryDto
    public DateTime GeneratedUtc { get; set; }

    public User? Owner { get; set; }
}
```

Removed: `Scope`, `GroupId`, `TicketType`, `Tip`, `ExportedUtc`, `Group` nav.

- [ ] **Step 2: Edit `PlanConfiguration.cs`** — drop column mappings for removed fields; add unique index:

```csharp
builder.HasIndex(p => p.OwnerUserId).IsUnique();
builder.Property(p => p.OwnerUserId).IsRequired();
builder.Property(p => p.ContentJson).HasColumnType("jsonb").IsRequired();
```

Remove the GroupId FK config and the XOR CHECK constraint.

### Task 1.6 — Extend `UserPreferences` entity

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Entities/UserPreferences.cs`**:

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Entities;

public sealed class UserPreferences
{
    public Guid UserId { get; set; }
    public DateTime UpdatedUtc { get; set; }

    // Scalars (existing)
    public TicketType? TicketType { get; set; }
    public AccommodationType? Accommodation { get; set; }
    public TransportMode? Transport { get; set; }
    public AgeGroup? AgeGroup { get; set; }

    // Scalars (NEW)
    public string? Name { get; set; }
    public string? Origin { get; set; }
    public CrewKind? CrewKind { get; set; }
    public short? CrewEstimatedSize { get; set; }
    public string? AccommodationNote { get; set; }
    public string? TransportNote { get; set; }

    // Child collections (existing — Cuisines KEPT per FE request)
    public List<UserPreferenceGenre> Genres { get; set; } = [];
    public List<UserPreferenceFoodRestriction> FoodRestrictions { get; set; } = [];
    public List<UserPreferenceActivity> Activities { get; set; } = [];
    public List<UserPreferenceArtist> Artists { get; set; } = [];
    public List<UserPreferenceCuisine> Cuisines { get; set; } = [];

    // NEW child collection
    public List<UserPreferenceVibeTag> VibeTags { get; set; } = [];

    public User? User { get; set; }
}
```

Drop `Cuisines` nav. Verify enum names against current Core/Enums (`AccommodationType`, `TransportMode`, `AgeGroup` — use existing names; do NOT rename).

- [ ] **Step 2: Create `src/Reshape.ElectricAi.Plans/Entities/UserPreferenceVibeTag.cs`**:

```csharp
namespace Reshape.ElectricAi.Plans.Entities;

public sealed class UserPreferenceVibeTag
{
    public Guid UserId { get; set; }
    public string Value { get; set; } = string.Empty;

    public UserPreferences? UserPreferences { get; set; }
}
```

- [ ] **Step 3: Create `src/Reshape.ElectricAi.Plans/Persistence/Configurations/UserPreferenceVibeTagConfiguration.cs`**:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Configurations;

internal sealed class UserPreferenceVibeTagConfiguration : IEntityTypeConfiguration<UserPreferenceVibeTag>
{
    public void Configure(EntityTypeBuilder<UserPreferenceVibeTag> b)
    {
        b.ToTable("UserPreferenceVibeTags");
        b.HasKey(x => new { x.UserId, x.Value });
        b.Property(x => x.Value).HasMaxLength(60).IsRequired();
        b.HasOne(x => x.UserPreferences)
            .WithMany(p => p.VibeTags)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: Update `UserPreferencesConfiguration.cs`** — drop the `Cuisines` mapping; add the new scalar columns:

```csharp
b.Property(x => x.Name).HasMaxLength(80);
b.Property(x => x.Origin).HasMaxLength(120);
b.Property(x => x.AccommodationNote).HasMaxLength(200);
b.Property(x => x.TransportNote).HasMaxLength(200);
b.Property(x => x.CrewKind);
b.Property(x => x.CrewEstimatedSize);
b.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
```

Verify all child-table HasMany/WithOne calls remain intact for Genres/FoodRestrictions/Activities/Artists.

### Task 1.7 — Strip `Group.Preferences` nav

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Entities/Group.cs`** — remove `public GroupPreferences? Preferences { get; set; }`.

### Task 1.8 — Update `PlansDbContext`

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Persistence/PlansDbContext.cs`**:

Remove DbSets:
- `GroupPreferences`
- `GroupPreferenceGenres`
- `GroupPreferenceFoodRestrictions`
- `GroupPreferenceActivities`
- `GroupPreferenceArtists`
- `GroupPreferenceCuisines`

Keep DbSets:
- `UserPreferenceCuisines` (FE requested cuisines stay)

Add DbSet:
- `UserPreferenceVibeTags`

Configuration scan (`modelBuilder.ApplyConfigurationsFromAssembly(...)`) picks up the new config automatically — no manual `ApplyConfiguration<T>()` needed unless project uses explicit registration.

### Task 1.9 — Create `CrewKind` enum

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Enums/CrewKind.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Enums;

public enum CrewKind
{
    Solo = 0,
    WithGroup = 1
}
```

### Task 1.10 — Reshape Core preference DTOs

- [ ] **Step 1: Replace `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesDto.cs`** with the new shape:

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Preferences;

public sealed record PreferencesDto(
    string? Name,
    string? Origin,
    CrewDto? Crew,
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<MusicGenre> MusicGenres,
    IReadOnlyList<string> MustSeeArtists,
    IReadOnlyList<FoodRestriction> FoodRestrictions,
    IReadOnlyList<Cuisine> Cuisines,
    IReadOnlyList<ActivityInterest> ActivityInterests,
    TransportSuggestionDto? SuggestedTransport,
    AccommodationSuggestionDto? SuggestedAccommodation,
    TicketType? TicketType,
    AgeGroup? AgeGroup,
    int CompletionPercent);

public sealed record CrewDto(CrewKind Kind, int? EstimatedSize);
public sealed record TransportSuggestionDto(TransportMode Mode, string? Note);
public sealed record AccommodationSuggestionDto(AccommodationType Type, string? Note);
```

(Field names: keep `MustSeeArtists`/`MusicGenres`/`FoodRestrictions` to match existing `UserPreferenceArtist`/`UserPreferenceGenre`/`UserPreferenceFoodRestriction` collection names.)

- [ ] **Step 2: Replace `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesReplaceRequest.cs`** with PUT-body shape (mirrors PreferencesDto without CompletionPercent):

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Preferences;

public sealed record PreferencesReplaceRequest(
    string? Name,
    string? Origin,
    CrewDto? Crew,
    IReadOnlyList<string>? VibeTags,
    IReadOnlyList<MusicGenre>? MusicGenres,
    IReadOnlyList<string>? MustSeeArtists,
    IReadOnlyList<FoodRestriction>? FoodRestrictions,
    IReadOnlyList<Cuisine>? Cuisines,
    IReadOnlyList<ActivityInterest>? ActivityInterests,
    TransportSuggestionDto? SuggestedTransport,
    AccommodationSuggestionDto? SuggestedAccommodation,
    TicketType? TicketType,
    AgeGroup? AgeGroup);
```

- [ ] **Step 3: Replace `src/Reshape.ElectricAi.Core/Dtos/Preferences/PreferencesPatchRequest.cs`** (PATCH semantics — `null` = no change at the request level; nested collections passed as full replacements when present):

Same property list as `PreferencesReplaceRequest` but document the contract: a `null` property = no change; a non-null collection = full replace; explicit clear of a scalar is not supported via PATCH (use PUT). This matches existing project STJ convention (see memory note on `STJ PATCH convention`).

- [ ] **Step 4: Delete any `PreferencesCuisinesDto.cs`** if it exists.

### Task 1.11 — Update `PreferencesMappingExtensions`

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Extensions/PreferencesMappingExtensions.cs`** — keep cuisine handling. Update `ApplyReplace`, `ApplyPatch`, `ToDto` for new fields (Name/Origin/Crew/VibeTags/AccommodationNote/TransportNote) AND add `ApplyExtracted` that maps `AiExtractedPreferences` into the entity (cuisines included). Recompute `CompletionPercent`:

```csharp
public static int ComputeCompletionPercent(UserPreferences p)
{
    int filled = 0;
    if (!string.IsNullOrEmpty(p.Name)) filled++;
    if (!string.IsNullOrEmpty(p.Origin)) filled++;
    if (p.CrewKind.HasValue) filled++;
    if (p.TicketType.HasValue) filled++;
    if (p.AgeGroup.HasValue) filled++;
    if (p.Transport.HasValue) filled++;
    if (p.Accommodation.HasValue) filled++;
    if (p.VibeTags.Count > 0) filled++;
    if (p.Genres.Count > 0) filled++;
    if (p.Artists.Count > 0) filled++;
    if (p.FoodRestrictions.Count > 0) filled++;
    if (p.Cuisines.Count > 0) filled++;
    if (p.Activities.Count > 0) filled++;
    return filled * 100 / 13;
}
```

### Task 1.12 — Update `PreferencesService`

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Services/PreferencesService.cs`** to read/write the new fields via the updated mapping extensions. Drop any GroupPreferences references. Verify `UserPreferencesWithChildrenSpec` includes ALL child collections including the new `VibeTags` and the kept `Cuisines` (6 Includes total: Genres, FoodRestrictions, Activities, Artists, Cuisines, VibeTags) with SplitQuery — edit the spec accordingly.

### Task 1.13 — Update `GroupService` + tests

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Services/GroupService.cs`** — remove any read/write of `GroupPreferences` (e.g. `group.Preferences = ...` in create flow). Member add/remove + ownership rules unchanged.

- [ ] **Step 2: Update or delete existing tests** that target group preferences endpoints. Search `tests/Reshape.ElectricAi.Plans.Tests` for `GroupPreferences` and delete those test classes.

### Task 1.14 — Update `PlansModule` DI

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/PlansModule.cs`** — remove registrations for `IPlanGenerator`, `PlanGenerationOptions`, `IGroupPreferencesService`, `PlanGenerationRequestValidator`. The validator reflection scan `RegisterValidators` will skip deleted validators automatically; new validators land in later slices.

### Task 1.15 — Create EF migration

- [ ] **Step 1: Generate migration**

```bash
cd src/Reshape.ElectricAi.Plans
$env:RESHAPE_PLANS_CONNECTION = "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres"
dotnet ef migrations add RedesignItineraryModel
```

- [ ] **Step 2: Review the generated migration** and append the destructive cleanup at the top of `Up()`:

```csharp
// Wipe existing snapshots (incompatible ContentJson shape) BEFORE column drops.
migrationBuilder.Sql(@"DELETE FROM plans.""Plans"";");
```

Verify the auto-generated steps include:
- Drop FK + index on `Plans.GroupId`
- Drop columns `Plans.Scope`, `Plans.GroupId`, `Plans.TicketType`, `Plans.Tip`, `Plans.ExportedUtc`
- Alter `Plans.OwnerUserId` to NOT NULL
- Create unique index `IX_Plans_OwnerUserId` on `Plans.OwnerUserId`
- Add columns to `UserPreferences`: `Name`, `Origin`, `CrewKind`, `CrewEstimatedSize`, `AccommodationNote`, `TransportNote`
- Create table `plans.UserPreferenceVibeTags` with composite PK + FK cascade
- Drop tables `plans.GroupPreferenceCuisines`, `plans.GroupPreferenceArtists`, `plans.GroupPreferenceActivities`, `plans.GroupPreferenceFoodRestrictions`, `plans.GroupPreferenceGenres`, `plans.GroupPreferences` (DO NOT drop `plans.UserPreferenceCuisines` — kept per FE request)

If the auto-generated `Down()` is non-trivial, simplify to `throw new NotSupportedException("Destructive migration; no down support.")` per project precedent (verify against existing migrations first).

- [ ] **Step 3: Apply migration to dev DB**

```bash
dotnet ef database update
```

Verify with `psql`:

```sql
\d plans."Plans"
\d plans."UserPreferences"
\d plans."UserPreferenceVibeTags"
SELECT table_name FROM information_schema.tables WHERE table_schema = 'plans' ORDER BY 1;
-- Confirm: no GroupPreferences*, no UserPreferenceCuisines
```

### Task 1.16 — Verify build

- [ ] **Step 1: Build**

```bash
dotnet build
```

Expected: green. Any compile errors point to a missed reference to dropped types — Grep again, fix, re-build.

- [ ] **Step 2: Run existing tests (those that survive)**

```bash
dotnet test --no-build
```

Expected: existing pref tests pass against new DTO shape (they may need updates if they assert old field names — fix inline). Plan-generation tests should be deleted in Step 1.2 — verify nothing references `PlanGenerator` anymore.

### Task 1.17 — STAGED — request user commit

Commit message draft:

```
feat(plans): demolish plan-generator + group-prefs; reshape Plan + UserPreferences

- Drop PlanGenerator/AiPlanEnvelope/PlanGenerationOptions and the
  monolithic LLM plan envelope (replaced in subsequent commits).
- Drop IGroupPreferencesService + impl + 3 group-prefs endpoints.
- Drop GroupPreferences* tables + UserPreferenceCuisines table.
- Reshape Plan entity: OwnerUserId NOT NULL + unique index; drop
  Scope/GroupId/TicketType/Tip/ExportedUtc columns; ContentJson now
  holds new ItineraryDto (DTO arrives in subsequent slice).
- Extend UserPreferences with Name/Origin/Crew/AccommodationNote/
  TransportNote scalars + new UserPreferenceVibeTags child table.
- Add CrewKind enum.
- Reshape PreferencesDto/Replace/Patch DTOs for the new field set.
- EF migration RedesignItineraryModel wipes Plans rows before
  destructive schema change.
```

**STOP. Do not commit. Surface diff to user.**

---

## Slice 3: Add Core abstractions for itinerary

**Files (created):** `IItinerarySection`, `IItineraryBuilder`, `IPreferencesExtractor`, `IEventLookupService`, `UserPreferencesSnapshot`, `ItinerarySectionResult`, `ItineraryDto`, `ItinerarySectionDto`, `ItineraryGenerationRequest`, `ItineraryResponse`, per-section data DTOs, `RecommendedActivityDto`, `RecommendedArtistDto`, `AiExtractedPreferences` + nested types, new `WizardAnswer` (no QuestionId).

### Task 3.1 — `WizardAnswer` (Core)

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Dtos/Itinerary/WizardAnswer.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record WizardAnswer(string Question, string Answer, DateTimeOffset? AnsweredAt);
```

`AnsweredAt` accepted but not persisted. `Question` strings are opaque to BE.

### Task 3.2 — `ItineraryGenerationRequest`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItineraryGenerationRequest.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryGenerationRequest(
    int Version,
    string Locale,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<WizardAnswer> Answers,
    string? FreeText);
```

### Task 3.3 — `AiExtractedPreferences` + nested DTOs

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Dtos/Preferences/AiExtractedPreferences.cs`**:

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Preferences;

public sealed record AiExtractedPreferences(
    string? Name,
    string? Origin,
    AiExtractedCrew? Crew,
    IReadOnlyList<string>? VibeTags,
    IReadOnlyList<MusicGenre>? MusicGenres,
    IReadOnlyList<string>? MustSeeArtists,
    IReadOnlyList<FoodRestriction>? FoodRestrictions,
    IReadOnlyList<Cuisine>? Cuisines,
    IReadOnlyList<ActivityInterest>? ActivityInterests,
    AiExtractedTransportSuggestion? SuggestedTransport,
    AiExtractedAccommodationSuggestion? SuggestedAccommodation,
    TicketType? TicketType,
    AgeGroup? AgeGroup);
```

- [ ] **Step 2: Create the three nested DTOs** (`AiExtractedCrew`, `AiExtractedTransportSuggestion`, `AiExtractedAccommodationSuggestion`) — each as a small `sealed record` with `Kind`/`Mode`/`Type` + `Note`/`EstimatedSize` properties as in spec §4.

### Task 3.4 — `UserPreferencesSnapshot` + `ItinerarySectionResult`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Dtos/Itinerary/UserPreferencesSnapshot.cs`**:

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

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
    IReadOnlyList<Cuisine> Cuisines,
    IReadOnlyList<ActivityInterest> ActivityInterests,
    TransportMode? TransportMode,
    string? TransportNote,
    AccommodationType? AccommodationType,
    string? AccommodationNote,
    TicketType? TicketType,
    AgeGroup? AgeGroup);
```

- [ ] **Step 2: Create `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItinerarySectionResult.cs`**:

```csharp
using System.Text.Json.Nodes;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItinerarySectionResult(string Key, int Order, JsonNode Data, string? Diagnostic);
```

### Task 3.5 — Section data DTOs (typed, before JsonNode serialization)

- [ ] **Step 1: Create the six section data records** under `src/Reshape.ElectricAi.Core/Dtos/Itinerary/Sections/`:

```csharp
// GreetingSectionData.cs
public sealed record GreetingSectionData(string? Name, string? Origin, GreetingCrewDto? Crew);
public sealed record GreetingCrewDto(CrewKind Kind, int? Size);

// TransportSectionData.cs
public sealed record TransportSectionData(TransportMode? Mode, string? Note);

// AccommodationSectionData.cs
public sealed record AccommodationSectionData(AccommodationType? Type, string? Note);

// VibeActivitiesSectionData.cs
public sealed record VibeActivitiesSectionData(
    IReadOnlyList<string> VibeTags,
    IReadOnlyList<RecommendedActivityDto> TopActivities);

// FoodSectionData.cs
public sealed record FoodSectionData(
    IReadOnlyList<FoodRestriction> Restrictions,
    IReadOnlyList<Cuisine> PreferredCuisines,
    IReadOnlyList<RecommendedActivityDto> TopRestaurants);

// TopArtistsSectionData.cs
public sealed record TopArtistsSectionData(
    IReadOnlyList<RecommendedArtistDto> TopOverall,
    IReadOnlyList<ArtistDayDto> ByDay);
public sealed record ArtistDayDto(DateOnly Date, IReadOnlyList<RecommendedArtistDto> Artists);

// RecommendedActivityDto.cs
public sealed record RecommendedActivityDto(Guid Id, string Title, string Snippet, float Score);

// RecommendedArtistDto.cs
public sealed record RecommendedArtistDto(Guid Id, string Title, DateTimeOffset EventUtc, float Score);
```

### Task 3.6 — `ItineraryDto` + `ItinerarySectionDto`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItineraryDto.cs`**:

```csharp
using System.Text.Json.Nodes;

namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryDto(DateTime GeneratedUtc, IReadOnlyList<ItinerarySectionDto> Sections);
public sealed record ItinerarySectionDto(string Key, JsonNode Data, string? Diagnostic);
```

(Serialization to/from `Plan.ContentJson` jsonb via `LlmJsonOptions.Default` per existing pattern; tested in Slice 8.)

- [ ] **Step 2: Create `src/Reshape.ElectricAi.Core/Dtos/Itinerary/ItineraryResponse.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Dtos.Itinerary;

public sealed record ItineraryResponse(PreferencesDto Preferences, ItineraryDto Itinerary);
```

### Task 3.7 — Service contracts in Core

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Services/Itinerary/IItinerarySection.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IItinerarySection
{
    string Key { get; }
    int Order { get; }
    Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Create `IItineraryBuilder.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IItineraryBuilder
{
    Task<ItineraryDto> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Create `IPreferencesExtractor.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IPreferencesExtractor
{
    Task<AiExtractedPreferences> ExtractAsync(
        IReadOnlyList<WizardAnswer> answers,
        string? freeText,
        string locale,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Create `IEventLookupService.cs`**:

```csharp
namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IEventLookupService
{
    Task<IReadOnlyList<MatchedEvent>> FindByTitlesAsync(IReadOnlyList<string> titles, CancellationToken cancellationToken);
}

public sealed record MatchedEvent(Guid Id, string Title, DateTimeOffset EventUtc);
```

### Task 3.8 — Build + commit

- [ ] **Step 1: Build**

```bash
dotnet build
```

Expected: green. No tests yet for these (pure DTO + interface declarations).

- [ ] **Step 2: STAGED — request user commit**

```
feat(core): add itinerary + extractor contracts and DTOs
```

---

## Slice 4: `EnumNaturalLanguage` helper (TDD)

**Files:** create `src/Reshape.ElectricAi.Plans/Services/Itinerary/EnumNaturalLanguage.cs` + test `tests/Reshape.ElectricAi.Plans.Tests/Services/Itinerary/EnumNaturalLanguageTests.cs`.

### Task 4.1 — Failing test

- [ ] **Step 1: Create test file**

```csharp
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Services.Itinerary;

public class EnumNaturalLanguageTests
{
    [Theory]
    [InlineData(FoodRestriction.Vegetarian, "vegetarian friendly")]
    [InlineData(FoodRestriction.Vegan, "vegan friendly")]
    [InlineData(FoodRestriction.NoGluten, "gluten free")]
    [InlineData(FoodRestriction.Halal, "halal")]
    public void FoodRestriction_to_text(FoodRestriction r, string expected)
    {
        Assert.Equal(expected, EnumNaturalLanguage.ForEmbedding(r));
    }

    [Theory]
    [InlineData(ActivityInterest.FoodTour, "food tour")]
    [InlineData(ActivityInterest.Workshop, "workshop")]
    public void ActivityInterest_to_text(ActivityInterest a, string expected)
    {
        Assert.Equal(expected, EnumNaturalLanguage.ForEmbedding(a));
    }
}
```

- [ ] **Step 2: Run** `dotnet test --filter EnumNaturalLanguageTests` → expect compile error (`EnumNaturalLanguage` not found).

### Task 4.2 — Implement

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Plans/Services/Itinerary/EnumNaturalLanguage.cs`**:

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal static class EnumNaturalLanguage
{
    public static string ForEmbedding(FoodRestriction r) => r switch
    {
        FoodRestriction.Vegetarian => "vegetarian friendly",
        FoodRestriction.Vegan => "vegan friendly",
        FoodRestriction.NoGluten => "gluten free",
        FoodRestriction.NoDairy => "dairy free",
        FoodRestriction.NoMeat => "no meat",
        FoodRestriction.NoPork => "no pork",
        FoodRestriction.NoPeanuts => "no peanuts",
        FoodRestriction.NoShellfish => "no shellfish",
        FoodRestriction.NoEggs => "no eggs",
        FoodRestriction.Halal => "halal",
        FoodRestriction.Kosher => "kosher",
        _ => r.ToString().ToLowerInvariant()
    };

    public static string ForEmbedding(ActivityInterest a) =>
        System.Text.RegularExpressions.Regex
            .Replace(a.ToString(), "([a-z])([A-Z])", "$1 $2")
            .ToLowerInvariant();
}
```

(`ActivityInterest` enum names — confirm actual values; the regex splits PascalCase → words.)

- [ ] **Step 2: Run** `dotnet test --filter EnumNaturalLanguageTests` → expect pass.

### Task 4.3 — STAGED — request user commit

```
feat(plans): EnumNaturalLanguage helper for embedding queries
```

---

## Slice 5: No-IO sections (TDD; 3 sections)

**Files:** `GreetingSection`, `TransportSection`, `AccommodationSection` + 3 test files.

### Task 5.1 — `GreetingSection` test

- [ ] **Step 1: Create `GreetingSectionTests.cs`**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Plans.Services.Itinerary.Sections;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Services.Itinerary.Sections;

public class GreetingSectionTests
{
    [Fact]
    public async Task Emits_name_origin_crew()
    {
        var snap = new UserPreferencesSnapshot(
            Guid.NewGuid(), "Paul", "Cluj", CrewKind.WithGroup, 4,
            [], [], [], [], [], null, null, null, null, null, null);

        var section = new GreetingSection();
        var result = await section.BuildAsync(snap, CancellationToken.None);

        Assert.Equal("greeting", result.Key);
        Assert.Equal(10, result.Order);
        Assert.Null(result.Diagnostic);

        var data = result.Data.AsObject();
        Assert.Equal("Paul", (string?)data["name"]);
        Assert.Equal("Cluj", (string?)data["origin"]);
        Assert.Equal("WithGroup", (string?)data["crew"]?["kind"]);
        Assert.Equal(4, (int?)data["crew"]?["size"]);
    }

    [Fact]
    public async Task Handles_nulls()
    {
        var snap = new UserPreferencesSnapshot(
            Guid.NewGuid(), null, null, null, null,
            [], [], [], [], [], null, null, null, null, null, null);

        var section = new GreetingSection();
        var result = await section.BuildAsync(snap, CancellationToken.None);

        var data = result.Data.AsObject();
        Assert.Null((string?)data["name"]);
        Assert.Null((string?)data["origin"]);
        Assert.Null(data["crew"]);
    }
}
```

- [ ] **Step 2: Run** → expect compile error (`GreetingSection` not found).

### Task 5.2 — `GreetingSection` implementation

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Plans/Services/Itinerary/Sections/GreetingSection.cs`**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class GreetingSection : IItinerarySection
{
    public string Key => "greeting";
    public int Order => 10;

    public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot p, CancellationToken ct)
    {
        var data = new GreetingSectionData(
            p.Name,
            p.Origin,
            p.CrewKind is null ? null : new GreetingCrewDto(p.CrewKind.Value, p.CrewEstimatedSize));
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return Task.FromResult(new ItinerarySectionResult(Key, Order, node, null));
    }
}
```

- [ ] **Step 2: Run** → expect pass.

### Task 5.3 — `TransportSection` (test + impl)

- [ ] **Step 1: Test**

```csharp
public class TransportSectionTests
{
    [Fact]
    public async Task Emits_mode_and_note()
    {
        var snap = new UserPreferencesSnapshot(
            Guid.NewGuid(), null, null, null, null,
            [], [], [], [], [], TransportMode.Car, "good route",
            null, null, null, null);
        var result = await new TransportSection().BuildAsync(snap, CancellationToken.None);
        Assert.Equal("transport", result.Key);
        Assert.Equal(20, result.Order);
        var data = result.Data.AsObject();
        Assert.Equal("Car", (string?)data["mode"]);
        Assert.Equal("good route", (string?)data["note"]);
    }

    [Fact]
    public async Task Handles_nulls()
    {
        var snap = new UserPreferencesSnapshot(
            Guid.NewGuid(), null, null, null, null,
            [], [], [], [], [], null, null, null, null, null, null);
        var result = await new TransportSection().BuildAsync(snap, CancellationToken.None);
        var data = result.Data.AsObject();
        Assert.Null((string?)data["mode"]);
        Assert.Null((string?)data["note"]);
    }
}
```

- [ ] **Step 2: Implementation**

```csharp
internal sealed class TransportSection : IItinerarySection
{
    public string Key => "transport";
    public int Order => 20;
    public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot p, CancellationToken ct)
    {
        var data = new TransportSectionData(p.TransportMode, p.TransportNote);
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return Task.FromResult(new ItinerarySectionResult(Key, Order, node, null));
    }
}
```

### Task 5.4 — `AccommodationSection` (test + impl)

- [ ] **Step 1: Test**

```csharp
public class AccommodationSectionTests
{
    [Fact]
    public async Task Emits_type_and_note()
    {
        var snap = new UserPreferencesSnapshot(
            Guid.NewGuid(), null, null, null, null, [], [], [], [], [],
            null, null, AccommodationType.Camping, "near main stage", null, null);
        var result = await new AccommodationSection().BuildAsync(snap, CancellationToken.None);
        Assert.Equal("accommodation", result.Key);
        Assert.Equal(60, result.Order);
        var data = result.Data.AsObject();
        Assert.Equal("Camping", (string?)data["type"]);
        Assert.Equal("near main stage", (string?)data["note"]);
    }

    [Fact]
    public async Task Handles_nulls()
    {
        var snap = new UserPreferencesSnapshot(
            Guid.NewGuid(), null, null, null, null, [], [], [], [], [],
            null, null, null, null, null, null);
        var result = await new AccommodationSection().BuildAsync(snap, CancellationToken.None);
        var data = result.Data.AsObject();
        Assert.Null((string?)data["type"]);
        Assert.Null((string?)data["note"]);
    }
}
```

- [ ] **Step 2: Implementation**

```csharp
internal sealed class AccommodationSection : IItinerarySection
{
    public string Key => "accommodation";
    public int Order => 60;
    public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot p, CancellationToken ct)
    {
        var data = new AccommodationSectionData(p.AccommodationType, p.AccommodationNote);
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return Task.FromResult(new ItinerarySectionResult(Key, Order, node, null));
    }
}
```

### Task 5.5 — Run all unit tests + STAGED

- [ ] **Step 1:** `dotnet test`
- [ ] **Step 2: STAGED — request user commit**

```
feat(plans): no-IO itinerary sections (Greeting, Transport, Accommodation)
```

---

## Slice 6: `ItineraryBuilder` orchestrator (TDD)

### Task 6.1 — Failing test (parallel, ordering, fail-soft)

- [ ] **Step 1: Create `ItineraryBuilderTests.cs`**

```csharp
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Plans.Services.Itinerary;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Services.Itinerary;

public class ItineraryBuilderTests
{
    private static UserPreferencesSnapshot Empty() => new(
        Guid.NewGuid(), null, null, null, null, [], [], [], [], [], null, null, null, null, null, null);

    [Fact]
    public async Task Runs_all_sections_parallel_and_orders_result()
    {
        var sem = new SemaphoreSlim(0, 2);
        var sections = new IItinerarySection[]
        {
            new DelaySection("b", 20, sem),
            new DelaySection("a", 10, sem),
        };
        var builder = new ItineraryBuilder(sections, NullLogger<ItineraryBuilder>.Instance);

        var task = builder.BuildAsync(Empty(), CancellationToken.None);
        // Both sections should be awaiting the semaphore concurrently.
        await Task.Delay(50);
        sem.Release(2);
        var dto = await task;

        Assert.Equal(["a", "b"], dto.Sections.Select(s => s.Key));
    }

    [Fact]
    public async Task Failing_section_emits_diagnostic_others_succeed()
    {
        var sections = new IItinerarySection[]
        {
            new ThrowingSection("bad", 50),
            new ConstSection("good", 10, JsonNode.Parse("""{"ok":true}""")!),
        };
        var builder = new ItineraryBuilder(sections, NullLogger<ItineraryBuilder>.Instance);
        var dto = await builder.BuildAsync(Empty(), CancellationToken.None);
        Assert.Collection(dto.Sections,
            s => { Assert.Equal("good", s.Key); Assert.Null(s.Diagnostic); },
            s => { Assert.Equal("bad", s.Key); Assert.StartsWith("section-failed:", s.Diagnostic); });
    }

    [Fact]
    public async Task Cancellation_propagates()
    {
        var sections = new IItinerarySection[] { new CancellableSection() };
        var builder = new ItineraryBuilder(sections, NullLogger<ItineraryBuilder>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => builder.BuildAsync(Empty(), cts.Token));
    }

    private sealed class DelaySection(string key, int order, SemaphoreSlim sem) : IItinerarySection
    {
        public string Key => key; public int Order => order;
        public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken ct)
        {
            await sem.WaitAsync(ct);
            return new ItinerarySectionResult(key, order, JsonNode.Parse("{}")!, null);
        }
    }
    private sealed class ThrowingSection(string key, int order) : IItinerarySection
    {
        public string Key => key; public int Order => order;
        public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken __)
            => throw new InvalidOperationException("boom");
    }
    private sealed class ConstSection(string key, int order, JsonNode data) : IItinerarySection
    {
        public string Key => key; public int Order => order;
        public Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken __)
            => Task.FromResult(new ItinerarySectionResult(key, order, data, null));
    }
    private sealed class CancellableSection : IItinerarySection
    {
        public string Key => "c"; public int Order => 1;
        public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot _, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(Timeout.Infinite, ct);
            return null!;
        }
    }
}
```

- [ ] **Step 2: Run** → expect compile fail.

### Task 6.2 — Implement `ItineraryBuilder`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Plans/Services/Itinerary/ItineraryBuilder.cs`**

```csharp
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal sealed partial class ItineraryBuilder(
    IEnumerable<IItinerarySection> sections,
    ILogger<ItineraryBuilder> logger) : IItineraryBuilder
{
    public async Task<ItineraryDto> BuildAsync(UserPreferencesSnapshot prefs, CancellationToken ct)
    {
        var tasks = sections.Select(s => RunSafe(s, prefs, ct)).ToArray();
        var results = await Task.WhenAll(tasks);
        var ordered = results
            .OrderBy(r => r.Order)
            .Select(r => new ItinerarySectionDto(r.Key, r.Data, r.Diagnostic))
            .ToList();
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

    [LoggerMessage(EventId = 7001, Level = LogLevel.Warning, Message = "Itinerary section failed key={Key}")]
    private static partial void LogSectionFailed(ILogger logger, string key, Exception ex);
}
```

- [ ] **Step 2: Run** → expect pass.

### Task 6.3 — STAGED

```
feat(plans): ItineraryBuilder orchestrator (parallel sections, fail-soft)
```

---

## Slice 7: `IEventLookupService` impl + VectorDb sections (TDD)

### Task 7.1 — Project reference Plans → VectorDb

- [ ] **Step 1: Edit `src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj`** — add ProjectReference:

```xml
<ItemGroup>
  <ProjectReference Include="..\Reshape.ElectricAi.VectorDb\Reshape.ElectricAi.VectorDb.csproj" />
</ItemGroup>
```

- [ ] **Step 2: Build** to confirm no circular deps. VectorDb already depends on Infrastructure (per memory); Plans should not be in VectorDb's chain. Verify with `dotnet build` from solution root.

### Task 7.2 — `EventLookupService` in VectorDb

- [ ] **Step 1: Create `src/Reshape.ElectricAi.VectorDb/Services/EventLookupService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class EventLookupService(VectorDbContext context) : IEventLookupService
{
    public async Task<IReadOnlyList<MatchedEvent>> FindByTitlesAsync(
        IReadOnlyList<string> titles, CancellationToken ct)
    {
        if (titles is null || titles.Count == 0) return [];

        // Case-insensitive contains-match for each provided title.
        var lowered = titles.Select(t => t.ToLowerInvariant()).ToArray();

        var rows = await context.EventEntries
            .AsNoTracking()
            .Where(e => lowered.Any(t => EF.Functions.ILike(e.Title, "%" + t + "%")))
            .Select(e => new { e.Id, e.Title, e.EventUtc })
            .ToListAsync(ct);

        return rows.Select(r => new MatchedEvent(r.Id, r.Title, r.EventUtc)).ToList();
    }
}
```

- [ ] **Step 2: Register in `VectorDbModule`** — add `services.AddScoped<IEventLookupService, EventLookupService>();`.

### Task 7.3 — `VibeActivitiesSection` (TDD)

- [ ] **Step 1: Test** — fake `IVectorSearchService.SearchDocumentsAsync` returns canned 5 `RetrievedChunk` rows. Assert section emits `vibeTags` passthrough + `topActivities` of 5 with id/title/snippet/score. Empty vibeTags + empty activityInterests → still calls vector search (with empty composite query) and emits whatever vector returns.

- [ ] **Step 2: Implement**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class VibeActivitiesSection(IVectorSearchService vector) : IItinerarySection
{
    public string Key => "vibeActivities";
    public int Order => 30;

    public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot p, CancellationToken ct)
    {
        var queryParts = p.VibeTags
            .Concat(p.ActivityInterests.Select(EnumNaturalLanguage.ForEmbedding))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var queryText = string.Join(' ', queryParts);
        if (string.IsNullOrWhiteSpace(queryText))
            queryText = "festival activity";

        var filter = new DocumentSearchFilter(
            QueryText: queryText,
            TopK: 5,
            UserContext: new Dictionary<Category, IReadOnlyList<string>> { [Category.Activity] = new List<string> { "all" } });
        var hits = await vector.SearchDocumentsAsync(filter, ct);

        var data = new VibeActivitiesSectionData(
            p.VibeTags,
            hits.Select(h => new RecommendedActivityDto(h.DocumentId, h.Title, h.Content, h.Score)).ToList());
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return new ItinerarySectionResult(Key, Order, node, null);
    }
}
```

(Note: `UserContext`'s value list semantics need verification against `CategoryTagsHelper.ToTags` — confirm whether `{ Category.Activity: ["all"] }` becomes the tag `"Activity.all"` and whether the seeded docs use that tag. Adjust seeding in integration test setup.)

- [ ] **Step 3: Run tests** → pass.

### Task 7.4 — `FoodSection` (TDD)

- [ ] **Step 1: Test** — fake `IVectorSearchService.SearchDocumentsAsync` returns 5 canned restaurants. Assert: query string is composed from `FoodRestriction` enum via `EnumNaturalLanguage` plus snapshot `Cuisines.Select(c => c.ToString().ToLowerInvariant())` joined; with empty restrictions AND empty cuisines → query falls back to `"restaurant"`; filter is `Category.Food`; emits `restrictions` passthrough + `preferredCuisines` passthrough + `topRestaurants` (up to 5).

- [ ] **Step 2: Implement** mirrors `VibeActivitiesSection` shape with food filter and additional `Cuisines` passthrough:

```csharp
internal sealed class FoodSection(IVectorSearchService vector) : IItinerarySection
{
    public string Key => "food";
    public int Order => 40;

    public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot p, CancellationToken ct)
    {
        var parts = p.FoodRestrictions.Select(EnumNaturalLanguage.ForEmbedding)
            .Concat(p.Cuisines.Select(c => c.ToString().ToLowerInvariant()))
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var queryText = string.Join(' ', parts);
        if (string.IsNullOrWhiteSpace(queryText)) queryText = "restaurant";

        var filter = new DocumentSearchFilter(
            QueryText: queryText,
            TopK: 5,
            UserContext: new Dictionary<Category, IReadOnlyList<string>> { [Category.Food] = new List<string> { "all" } });
        var hits = await vector.SearchDocumentsAsync(filter, ct);

        var data = new FoodSectionData(
            p.FoodRestrictions,
            p.Cuisines,
            hits.Select(h => new RecommendedActivityDto(h.DocumentId, h.Title, h.Content, h.Score)).ToList());
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return new ItinerarySectionResult(Key, Order, node, null);
    }
}
```

### Task 7.5 — `TopArtistsSection` (TDD)

- [ ] **Step 1: Test** — fake `IEventLookupService` returns 2 must-see matches; fake `IVectorSearchService.SearchEventsAsync` returns 10 events across 3 days. Assert:
  - `topOverall` includes both must-see entries (artificial top score) + dedup with vector hits, length ≤ 5
  - `byDay` grouped by `EventUtc.Date`, ordered by date ascending, each day has ≤ 3 artists
  - When must-see name has no match, silently skipped (no diagnostic)

- [ ] **Step 2: Implement**

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Itinerary.Sections;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;

namespace Reshape.ElectricAi.Plans.Services.Itinerary.Sections;

internal sealed class TopArtistsSection(
    IVectorSearchService vector,
    IEventLookupService events) : IItinerarySection
{
    public string Key => "topArtists";
    public int Order => 50;

    public async Task<ItinerarySectionResult> BuildAsync(UserPreferencesSnapshot p, CancellationToken ct)
    {
        var mustSeeTask = events.FindByTitlesAsync(p.MustSeeArtists, ct);
        var queryText = string.Join(' ', p.MusicGenres.Select(g => g.ToString()).Concat(p.VibeTags));
        if (string.IsNullOrWhiteSpace(queryText)) queryText = "live music";

        var vectorTask = vector.SearchEventsAsync(new EventSearchFilter(
            QueryText: queryText,
            TopK: 30,
            UserContext: new Dictionary<Category, IReadOnlyList<string>> { [Category.Music] = new List<string> { "all" } }), ct);

        await Task.WhenAll(mustSeeTask, vectorTask);

        var mustSeeArtists = (await mustSeeTask)
            .Select(m => new RecommendedArtistDto(m.Id, m.Title, m.EventUtc, 1.0f))
            .ToList();
        var vectorArtists = (await vectorTask)
            .Select(v => new RecommendedArtistDto(v.FeedEntryId, v.Title, v.EventUtc, v.Score))
            .ToList();

        var merged = mustSeeArtists
            .Concat(vectorArtists)
            .GroupBy(a => a.Id)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .ToList();

        var topOverall = merged
            .OrderByDescending(a => a.Score)
            .Take(5)
            .ToList();
        var byDay = merged
            .GroupBy(a => DateOnly.FromDateTime(a.EventUtc.UtcDateTime.Date))
            .OrderBy(g => g.Key)
            .Select(g => new ArtistDayDto(
                g.Key,
                g.OrderByDescending(a => a.Score).Take(3).ToList()))
            .ToList();

        var data = new TopArtistsSectionData(topOverall, byDay);
        var node = JsonSerializer.SerializeToNode(data, LlmJsonOptions.Default)!;
        return new ItinerarySectionResult(Key, Order, node, null);
    }
}
```

- [ ] **Step 3: Run tests** → pass.

### Task 7.6 — STAGED

```
feat(plans): VectorDb-backed sections (VibeActivities, Food, TopArtists) + IEventLookupService
```

---

## Slice 8: `PreferencesExtractor` (TDD)

### Task 8.1 — System prompt resource

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Plans/Services/Prompts/PreferencesExtractorSystemPrompt.md`**

Content: precise instructions to the LLM. Outline:
- Role: extract structured user prefs from wizard answers + free text
- Output: emit ONLY via the structured response tool; never plaintext
- Field guidance per field (Name, Origin, Crew {kind, estimatedSize}, VibeTags free-form short tags, MustSeeArtists exact names, MusicGenres + FoodRestrictions + Cuisines + ActivityInterests strict enums, SuggestedTransport, SuggestedAccommodation)
- **Multi-field answers are normal**: FE may combine multiple extraction targets into a single wizard question (e.g. one question asks both origin and crew; another asks vibe and between-set activities; another asks must-see artists and favourite genres). Parse all relevant fields from each answer regardless of which question asked it.
- Field hints may also appear in the trailing `freeText` ("anything else we should know" / "tell us about you"). Treat freeText as equally authoritative.
- Locale handling: if `locale=ro`, accept Romanian answers; output English enum values regardless. Romanian diacritics (ăîâțș) preserved in free-text fields (Name, Origin, VibeTags, MustSeeArtists).
- Brevity: VibeTags ≤ 60 chars each, ≤ 6 entries; MustSeeArtists ≤ 80 chars each, ≤ 10 entries; Origin ≤ 120 chars; Name ≤ 80 chars; TransportNote / AccommodationNote ≤ 200 chars.
- If a field is uncertain or absent, emit `null` (NOT a guess). Strict schema permits null on every field.

- [ ] **Step 2: Add to csproj as embedded resource** in `Reshape.ElectricAi.Plans.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Services\Prompts\PreferencesExtractorSystemPrompt.md" />
</ItemGroup>
```

### Task 8.2 — `ItineraryGenerationOptions`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Plans/Configuration/ItineraryGenerationOptions.cs`**

```csharp
namespace Reshape.ElectricAi.Plans.Configuration;

public sealed class ItineraryGenerationOptions
{
    public const string SectionName = "ItineraryGeneration";

    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxCompletionTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.2;
    public RateLimitOptions RateLimit { get; set; } = new() { PerHour = 10 };
    public RateLimitOptions PrefsRateLimit { get; set; } = new() { PerHour = 30 };

    public sealed class RateLimitOptions
    {
        public int PerHour { get; set; }
    }
}
```

### Task 8.3 — Failing test for extractor

- [ ] **Step 1: Create `PreferencesExtractorTests.cs`**

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Configuration;
using Reshape.ElectricAi.Plans.Services.Itinerary;
using Reshape.ElectricAi.Plans.Tests.Fakes;
using Xunit;

namespace Reshape.ElectricAi.Plans.Tests.Services.Itinerary;

public class PreferencesExtractorTests
{
    [Fact]
    public async Task Extracts_canned_fields()
    {
        var fakeOpenAi = new FakeOpenAiClient();
        fakeOpenAi.Enqueue(new AiExtractedPreferences(
            Name: "Paul", Origin: "Cluj",
            Crew: new AiExtractedCrew(CrewKind.WithGroup, 4),
            VibeTags: ["full row", "party"],
            MusicGenres: null, MustSeeArtists: ["Teddy Swims"],
            FoodRestrictions: [FoodRestriction.Vegetarian],
            ActivityInterests: null,
            SuggestedTransport: new AiExtractedTransportSuggestion(TransportMode.Car, null),
            SuggestedAccommodation: new AiExtractedAccommodationSuggestion(AccommodationType.Camping, null),
            TicketType: null, AgeGroup: null), usage: default);

        var opts = Options.Create(new ItineraryGenerationOptions());
        var extractor = new PreferencesExtractor(fakeOpenAi, opts, NullLogger<PreferencesExtractor>.Instance);

        var result = await extractor.ExtractAsync(
            answers: [new WizardAnswer("Q", "A", null)],
            freeText: null,
            locale: "en",
            ct: CancellationToken.None);

        Assert.Equal("Paul", result.Name);
        Assert.Equal("Cluj", result.Origin);
        Assert.Equal(CrewKind.WithGroup, result.Crew!.Kind);
        Assert.Equal(4, result.Crew.EstimatedSize);
        Assert.Equal(["Teddy Swims"], result.MustSeeArtists);
    }

    [Fact]
    public async Task Schema_violation_terminal()
    {
        var fakeOpenAi = new FakeOpenAiClient();
        fakeOpenAi.EnqueueSchemaFailure();
        var extractor = new PreferencesExtractor(
            fakeOpenAi, Options.Create(new ItineraryGenerationOptions()),
            NullLogger<PreferencesExtractor>.Instance);

        await Assert.ThrowsAsync<LlmSchemaException>(() => extractor.ExtractAsync([], "anything", "en", CancellationToken.None));
    }
}
```

- [ ] **Step 2: Verify `FakeOpenAiClient` supports `EnqueueSchemaFailure`** (memory says fake uses STJ round-trip — add a sentinel for schema-fail to throw `LlmSchemaException` from `CompleteStructuredAsync<T>`). If not present, extend the existing fake.

- [ ] **Step 3: Run** → expect compile fail.

### Task 8.4 — Implement `PreferencesExtractor`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Plans/Services/Itinerary/PreferencesExtractor.cs`**

```csharp
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Dtos.Itinerary;
using Reshape.ElectricAi.Core.Dtos.Preferences;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Services.Itinerary;
using Reshape.ElectricAi.Plans.Configuration;

namespace Reshape.ElectricAi.Plans.Services.Itinerary;

internal sealed partial class PreferencesExtractor(
    IOpenAiClient openAi,
    IOptions<ItineraryGenerationOptions> options,
    ILogger<PreferencesExtractor> logger) : IPreferencesExtractor
{
    private static readonly string SystemPrompt = LoadEmbeddedPrompt();
    private static readonly JsonNode ResponseSchema = JsonSchemaExporter.GetJsonSchemaAsNode(
        LlmJsonOptions.Default, typeof(AiExtractedPreferences));

    private readonly ItineraryGenerationOptions _options = options.Value;

    public async Task<AiExtractedPreferences> ExtractAsync(
        IReadOnlyList<WizardAnswer> answers,
        string? freeText,
        string locale,
        CancellationToken ct)
    {
        LogStarted(logger, answers.Count, freeText?.Length ?? 0, locale);
        var userPrompt = BuildUserPrompt(answers, freeText, locale);
        var llm = await openAi.CompleteStructuredAsync<AiExtractedPreferences>(
            SystemPrompt, userPrompt, ResponseSchema,
            _options.Model, _options.MaxCompletionTokens, _options.Temperature, ct);
        LogCompleted(logger, llm.Usage.PromptTokens, llm.Usage.CompletionTokens, llm.Usage.CostCents);
        return llm.Value;
    }

    private static string BuildUserPrompt(IReadOnlyList<WizardAnswer> answers, string? freeText, string locale)
    {
        var sb = new StringBuilder();
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"locale={locale}");
        sb.AppendLine("User answered the wizard like this:");
        var i = 1;
        foreach (var a in answers)
        {
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{i++}. {a.Question}");
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"   -> {a.Answer}");
        }
        sb.AppendLine("Additional notes from the user:");
        sb.AppendLine(string.IsNullOrWhiteSpace(freeText) ? "(none)" : freeText);
        sb.AppendLine("Extract the structured user preferences via the response tool.");
        return sb.ToString();
    }

    private static string LoadEmbeddedPrompt()
    {
        var asm = typeof(PreferencesExtractor).Assembly;
        const string resource = "Reshape.ElectricAi.Plans.Services.Prompts.PreferencesExtractorSystemPrompt.md";
        using var stream = asm.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded resource missing: {resource}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [LoggerMessage(EventId = 7101, Level = LogLevel.Information,
        Message = "PreferencesExtraction started answers={Answers} freeTextLength={FreeTextLength} locale={Locale}")]
    private static partial void LogStarted(ILogger logger, int answers, int freeTextLength, string locale);

    [LoggerMessage(EventId = 7102, Level = LogLevel.Information,
        Message = "PreferencesExtraction completed promptTokens={Prompt} completionTokens={Completion} costCents={Cost}")]
    private static partial void LogCompleted(ILogger logger, int prompt, int completion, int cost);
}
```

- [ ] **Step 2: Run tests** → pass. If `FakeOpenAiClient.EnqueueSchemaFailure` doesn't exist yet, extend it: add a queue of "next call throws `LlmSchemaException`" markers and check before dequeue.

### Task 8.5 — STAGED

```
feat(plans): PreferencesExtractor (single-purpose LLM call) + ItineraryGenerationOptions
```

---

## Slice 9: `ItineraryService` + endpoints + DI wire-up

### Task 9.1 — `IItineraryService` (Core) + impl (Plans)

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Core/Services/Itinerary/IItineraryService.cs`**

```csharp
namespace Reshape.ElectricAi.Core.Services.Itinerary;

public interface IItineraryService
{
    Task<ItineraryResponse> GenerateAsync(Guid userId, ItineraryGenerationRequest request, CancellationToken ct);
    Task<ItineraryResponse?> GetAsync(Guid userId, CancellationToken ct);
    Task<ItineraryResponse> RebuildAfterPrefsChangeAsync(Guid userId, CancellationToken ct);
}
```

- [ ] **Step 2: Create `src/Reshape.ElectricAi.Plans/Services/Itinerary/ItineraryService.cs`**

Behavior:
1. `GenerateAsync` — acquire rate-limiter (`itinerary-gen:{userId}`); ensure user exists; extract via `IPreferencesExtractor`; open EF transaction; upsert `UserPreferences` (apply extracted via new `ApplyExtracted`); build snapshot via `UserPreferencesSnapshot.From(entity)`; build `ItineraryDto` via `IItineraryBuilder`; upsert `Plan` (single row per OwnerUserId); `SaveChanges`; commit.
2. `GetAsync` — load `Plan` for user; deserialize `ContentJson` to `ItineraryDto`; load prefs; return `ItineraryResponse` or null.
3. `RebuildAfterPrefsChangeAsync` — load prefs; build `UserPreferencesSnapshot`; build `ItineraryDto`; upsert `Plan`; return `ItineraryResponse`.

(Code is straightforward but long; layout the methods + private helpers `BuildSnapshot(UserPreferences)`, `UpsertPlan(Guid userId, ItineraryDto)`. Use existing `IRepository<UserPreferences>` + `IRepository<Plan>` per project pattern. Single `await using var tx = await dbContext.Database.BeginTransactionAsync(ct);` wraps all writes.)

- [ ] **Step 3: Unit tests for `ItineraryService.GenerateAsync` happy path + section failure** (Plans.Tests). Use fakes for extractor + builder + repositories.

### Task 9.2 — `ItineraryController`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Presentation/Controllers/V1/ItineraryController.cs`**

```csharp
[ApiController]
[Route("api/v1/itinerary")]
[Authorize]
public sealed class ItineraryController(IItineraryService service) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult<ItineraryResponse>> Generate(
        [FromBody] ItineraryGenerationRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();   // existing extension per project convention
        var response = await service.GenerateAsync(userId, request, ct);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<ItineraryResponse>> Get(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var response = await service.GetAsync(userId, ct);
        return response is null
            ? NotFound(new { error = new { code = "itinerary-not-found", message = "No itinerary generated yet." } })
            : Ok(response);
    }
}
```

(`User.GetUserId()` — verify the actual extension name in existing controllers; reuse same pattern as `PreferencesController`.)

### Task 9.3 — `ItineraryGenerationRequestValidator`

- [ ] **Step 1: Create `src/Reshape.ElectricAi.Plans/Validators/ItineraryGenerationRequestValidator.cs`**

```csharp
using FluentValidation;
using Reshape.ElectricAi.Core.Dtos.Itinerary;

namespace Reshape.ElectricAi.Plans.Validators;

internal sealed class ItineraryGenerationRequestValidator : AbstractValidator<ItineraryGenerationRequest>
{
    public ItineraryGenerationRequestValidator()
    {
        RuleFor(x => x.Version).Equal(1).WithMessage("unsupported-version");
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(8);
        RuleFor(x => x)
            .Must(r => (r.Answers is { Count: > 0 }) || !string.IsNullOrWhiteSpace(r.FreeText))
            .WithMessage("preferences-required");
        RuleForEach(x => x.Answers).ChildRules(a =>
        {
            a.RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
            a.RuleFor(x => x.Answer).NotEmpty().MaximumLength(2000);
        });
        RuleFor(x => x.FreeText).MaximumLength(4000);
    }
}
```

### Task 9.4 — `PreferencesController` snapshot rebuild

- [ ] **Step 1: Inject `IItineraryService` into `PreferencesController`**.

- [ ] **Step 2: After successful PUT/PATCH save, await `service.RebuildAfterPrefsChangeAsync(userId, ct)`** and return its `ItineraryResponse.Preferences` (so the response shape stays a `PreferencesDto` for backwards-shape compatibility, but the snapshot is rebuilt as a side effect).

- [ ] **Step 3: Add rate-limit per `prefs-update:{userId}`** before the write.

### Task 9.5 — `PlansModule` final DI wiring

- [ ] **Step 1: Edit `PlansModule.cs`** — register:

```csharp
services.AddOptions<ItineraryGenerationOptions>()
    .Bind(configuration.GetSection(ItineraryGenerationOptions.SectionName))
    .Validate(o => o.MaxCompletionTokens > 0, "MaxCompletionTokens must be > 0")
    .ValidateOnStart();

services.AddScoped<IPreferencesExtractor, PreferencesExtractor>();
services.AddScoped<IItineraryBuilder, ItineraryBuilder>();
services.AddScoped<IItineraryService, ItineraryService>();

services.AddScoped<IItinerarySection, GreetingSection>();
services.AddScoped<IItinerarySection, TransportSection>();
services.AddScoped<IItinerarySection, AccommodationSection>();
services.AddScoped<IItinerarySection, VibeActivitiesSection>();
services.AddScoped<IItinerarySection, FoodSection>();
services.AddScoped<IItinerarySection, TopArtistsSection>();
```

(`IEventLookupService` is registered in `VectorDbModule` per Task 7.2.)

### Task 9.6 — Build + STAGED

- [ ] **Step 1:** `dotnet build`
- [ ] **Step 2:** `dotnet test --no-build` (unit tests pass; integration tests for new endpoints land in Slice 10)
- [ ] **Step 3:** STAGED

```
feat(plans): ItineraryService + ItineraryController + endpoint validators + DI
```

---

## Slice 10: Integration tests

### Task 10.1 — Extend `AuthApiFactory` for VectorDb

- [ ] **Step 1: Add env vars** to `CreateHost(IHostBuilder)` BEFORE `base.CreateHost(builder)`:

```csharp
Environment.SetEnvironmentVariable("OpenAi__ApiKey", "test-key");
Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__PromptCentsPer1K", "0.015");
Environment.SetEnvironmentVariable("OpenAi__Models__gpt-4o-mini__CompletionCentsPer1K", "0.060");
Environment.SetEnvironmentVariable("ConnectionStrings__VectorDb", _pgContainer.GetConnectionString());
```

(Per memory: required for `AiChatModule + VectorDbModule fail-fast on missing OpenAi:ApiKey`.)

- [ ] **Step 2: Verify `UseEnvironment("Testing")`** is set (per memory rule). Add if missing.

- [ ] **Step 3: Add `SeedTestVectorDataAsync(VectorDbContext, IEmbeddingService)` helper**: 3 EventEntries with hand-crafted `EventUtc` (3 distinct dates), 3 DocumentChunks with `Activity.all` tag, 3 with `Food.all` tag. `FakeEmbeddingService` returns deterministic 1536-dim vectors per input.

- [ ] **Step 4: Replace any `EnsureDeletedAsync` in fixtures with `TRUNCATE ... RESTART IDENTITY CASCADE`** (memory rule — see "Postgres test-reset rule"). Truncate list includes new tables: `Plans.UserPreferenceVibeTags`. Remove dropped tables from truncate list.

### Task 10.2 — `ItineraryEndpointTests`

- [ ] **Step 1: Create `tests/Reshape.ElectricAi.Plans.Tests/Endpoints/ItineraryEndpointTests.cs`**. Happy-path test in full; remaining cases follow the same setup pattern:

```csharp
public class ItineraryEndpointTests : IClassFixture<AuthApiFactory>, IAsyncLifetime
{
    private readonly AuthApiFactory _factory;
    public ItineraryEndpointTests(AuthApiFactory f) { _factory = f; }

    public async Task InitializeAsync() { await _factory.ResetDatabaseAsync(); }
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Generate_happy_path_returns_200_with_six_sections()
    {
        // Arrange
        var (client, userId) = await _factory.AuthorizedClientAsync();
        _factory.FakeOpenAi.Enqueue(new AiExtractedPreferences(
            Name: "Paul", Origin: "Cluj",
            Crew: new AiExtractedCrew(CrewKind.WithGroup, 4),
            VibeTags: ["full row"],
            MusicGenres: [MusicGenre.Pop],
            MustSeeArtists: ["Teddy Swims"],
            FoodRestrictions: [FoodRestriction.Vegetarian],
            ActivityInterests: [ActivityInterest.FoodTour],
            SuggestedTransport: new AiExtractedTransportSuggestion(TransportMode.Car, null),
            SuggestedAccommodation: new AiExtractedAccommodationSuggestion(AccommodationType.Camping, null),
            TicketType: null, AgeGroup: null), usage: default);
        await _factory.SeedTestVectorDataAsync();

        var body = new ItineraryGenerationRequest(
            Version: 1, Locale: "en", SubmittedAt: DateTimeOffset.UtcNow,
            Answers: [new WizardAnswer("What should we call you?", "Paul", null)],
            FreeText: null);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/itinerary/generate", body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>();
        Assert.NotNull(dto);
        Assert.Equal("Paul", dto!.Preferences.Name);
        Assert.Equal(6, dto.Itinerary.Sections.Count);
        Assert.Contains(dto.Itinerary.Sections, s => s.Key == "greeting");
        Assert.Contains(dto.Itinerary.Sections, s => s.Key == "topArtists");

        // DB state
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        Assert.Equal(1, await db.Plans.CountAsync(p => p.OwnerUserId == userId));
        var prefs = await db.UserPreferences.Include(x => x.VibeTags).FirstAsync(x => x.UserId == userId);
        Assert.Equal("Paul", prefs.Name);
        Assert.Contains(prefs.VibeTags, t => t.Value == "full row");
    }

    [Fact]
    public async Task Generate_empty_inputs_returns_400_preferences_required()
    {
        var (client, _) = await _factory.AuthorizedClientAsync();
        var body = new ItineraryGenerationRequest(1, "en", DateTimeOffset.UtcNow, [], null);
        var response = await client.PostAsJsonAsync("/api/v1/itinerary/generate", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("preferences-required", err!.Error.Code);
    }

    [Fact]
    public async Task Generate_llm_schema_fail_returns_502_and_no_row_written()
    {
        var (client, userId) = await _factory.AuthorizedClientAsync();
        _factory.FakeOpenAi.EnqueueSchemaFailure();
        var body = new ItineraryGenerationRequest(1, "en", DateTimeOffset.UtcNow,
            [new WizardAnswer("Q", "A", null)], null);
        var response = await client.PostAsJsonAsync("/api/v1/itinerary/generate", body);
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        Assert.Equal(0, await db.Plans.CountAsync(p => p.OwnerUserId == userId));
        Assert.Equal(0, await db.UserPreferences.CountAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task Generate_rate_limit_returns_429()
    {
        var (client, _) = await _factory.AuthorizedClientAsync();
        await _factory.SeedTestVectorDataAsync();
        // Fire PerHour+1 requests; on the last, expect 429.
        for (var i = 0; i < 10; i++)
        {
            _factory.FakeOpenAi.Enqueue(SampleExtractedPrefs(), usage: default);
            var ok = await client.PostAsJsonAsync("/api/v1/itinerary/generate", SampleRequest());
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }
        var blocked = await client.PostAsJsonAsync("/api/v1/itinerary/generate", SampleRequest());
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task Generate_unauth_returns_401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/v1/itinerary/generate", SampleRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Generate_twice_overwrites_snapshot_no_duplicate_plan_row()
    {
        var (client, userId) = await _factory.AuthorizedClientAsync();
        await _factory.SeedTestVectorDataAsync();
        _factory.FakeOpenAi.Enqueue(SampleExtractedPrefs(name: "Paul"), usage: default);
        await client.PostAsJsonAsync("/api/v1/itinerary/generate", SampleRequest());
        _factory.FakeOpenAi.Enqueue(SampleExtractedPrefs(name: "Filip"), usage: default);
        await client.PostAsJsonAsync("/api/v1/itinerary/generate", SampleRequest());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
        Assert.Equal(1, await db.Plans.CountAsync(p => p.OwnerUserId == userId));
        var prefs = await db.UserPreferences.FirstAsync(x => x.UserId == userId);
        Assert.Equal("Filip", prefs.Name);
    }

    [Fact]
    public async Task Get_no_snapshot_returns_404_itinerary_not_found()
    {
        var (client, _) = await _factory.AuthorizedClientAsync();
        var response = await client.GetAsync("/api/v1/itinerary");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.Equal("itinerary-not-found", err!.Error.Code);
    }

    [Fact]
    public async Task Get_with_snapshot_returns_200()
    {
        var (client, _) = await _factory.AuthorizedClientAsync();
        await _factory.SeedTestVectorDataAsync();
        _factory.FakeOpenAi.Enqueue(SampleExtractedPrefs(), usage: default);
        await client.PostAsJsonAsync("/api/v1/itinerary/generate", SampleRequest());

        var response = await client.GetAsync("/api/v1/itinerary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ItineraryResponse>();
        Assert.Equal(6, dto!.Itinerary.Sections.Count);
    }

    // Helpers
    private static ItineraryGenerationRequest SampleRequest() => new(
        1, "en", DateTimeOffset.UtcNow,
        [new WizardAnswer("What should we call you?", "Paul", null)],
        null);

    private static AiExtractedPreferences SampleExtractedPrefs(string? name = "Paul") => new(
        Name: name, Origin: "Cluj",
        Crew: new AiExtractedCrew(CrewKind.Solo, null),
        VibeTags: ["party"], MusicGenres: null, MustSeeArtists: null,
        FoodRestrictions: null, ActivityInterests: null,
        SuggestedTransport: null, SuggestedAccommodation: null,
        TicketType: null, AgeGroup: null);

    private sealed record ErrorEnvelope(ErrorBody Error);
    private sealed record ErrorBody(string Code, string Message);
}
```

All other cases reuse `SampleRequest()` / `SampleExtractedPrefs()` and `_factory.FakeOpenAi.Enqueue(...)` from this template.

### Task 10.3 — Update `PreferencesEndpointTests`

- [ ] **Step 1:** Adjust assertions for new DTO shape (new field set, `completionPercent` recomputed).
- [ ] **Step 2:** Add a test asserting `PUT /preferences` triggers snapshot rebuild (fetch `GET /itinerary` after PUT, assert sections updated) AND that the LLM was NOT called (fake OpenAI client call count == 0).
- [ ] **Step 3:** Add test asserting stale `xmin` → 409 `preferences-conflict`.

### Task 10.4 — Test csproj warnings

- [ ] **Step 1:** Verify `tests/Reshape.ElectricAi.Plans.Tests/Reshape.ElectricAi.Plans.Tests.csproj` `<WarningsNotAsErrors>` includes `CS1591;CA1707;CA1515;CA2007;CA1812;CA1711;CA1001;CA1819;CA1062;CA1024;CA1822;CA1861`. Add `CA1861` if missing (per memory).

### Task 10.5 — Run all tests + STAGED

- [ ] **Step 1:** `dotnet test` → all green
- [ ] **Step 2:** STAGED

```
test(plans): integration tests for itinerary endpoints + preferences-rebuild
```

---

## Slice 11: Verification + memory + plan deletion

### Task 11.1 — `superpowers:verification-before-completion`

- [ ] **Step 1:** Invoke the skill explicitly.
- [ ] **Step 2: Full verification**

```bash
dotnet build
dotnet test
```

Both must be green with no new warnings. Note any pre-existing warnings (e.g. the AutoMapper CVE — per memory, ignore).

- [ ] **Step 3: Manual smoke test** via API client:
  1. POST `/api/v1/auth/register`, capture access token
  2. POST `/api/v1/itinerary/generate` with the example payload from user's first message
  3. GET `/api/v1/preferences` → confirm 12-dim completion + extracted fields
  4. GET `/api/v1/itinerary` → confirm 6 sections, top-5 activities + restaurants populated from seeded VectorDb data
  5. PUT `/api/v1/preferences` with `VibeTags=["chill"]` → confirm `GET /itinerary` reflects the change without an LLM call (check log for absence of `PreferencesExtraction started`)

### Task 11.2 — Dispatch review agents

- [ ] **Step 1: Code Reviewer** with directive: "Review the diff for the itinerary-redesign branch. **Verify CODE.md compliance against the changed files.** Focus on: DTO shapes, EF migration safety, DI ordering, validator coverage, fail-soft section error handling, transaction boundaries."

- [ ] **Step 2: Security Engineer** with directive: "Review the new `ItineraryController` + `PreferencesController` snapshot-rebuild wiring. **Verify CODE.md compliance.** Focus on: JWT claim extraction, rate-limit bypass risks, jsonb injection via ContentJson, cross-user data leakage, LLM prompt-injection resilience."

- [ ] **Step 3: Address findings** with new commits (NOT amends).

### Task 11.3 — Memory promotion

- [ ] **Step 1: `/si:remember`** for every non-obvious fact this slice produced:
  - "`IItinerarySection` pipeline pattern in Plans — add new section = new class + DI line; orchestrator runs parallel via `Task.WhenAll` + per-section try/catch fail-soft"
  - "`Plan.ContentJson` jsonb now stores `ItineraryDto`; deserialize via `LlmJsonOptions.Default` (camelCase + DefaultJsonTypeInfoResolver)"
  - "Plans csproj now references VectorDb directly; `IEventLookupService` lives in VectorDb with `EF.Functions.ILike` for case-insensitive title match"
  - "Itinerary integration tests seed VectorDb with hand-crafted Activity.all + Food.all + Music.all tagged rows; `FakeEmbeddingService` returns deterministic 1536-dim vectors"
  - "PUT/PATCH /preferences now triggers `IItineraryService.RebuildAfterPrefsChangeAsync` synchronously inside the same controller call"
- [ ] **Step 2: Update PROJECT.md** if any new commands or test-pattern changes warrant durable documentation.
- [ ] **Step 3: Update CODE.md** ONLY if any code rule emerged worth enforcing (e.g. "sections always serialize via `LlmJsonOptions.Default` to keep camelCase consistent on wire"). Run `/si:remember` first per project rule, then direct-edit.

### Task 11.4 — Delete plan file

- [ ] **Step 1: Delete `.claude/plans/itinerary-redesign.md`** — last step per CLAUDE.md Phase 10.
- [ ] **Step 2: Delete spec file** `docs/superpowers/specs/2026-05-23-itinerary-redesign-design.md` (optional — kept for history if useful; ask user).
- [ ] **Step 3: STAGED — request user commit**

```
chore: remove itinerary-redesign plan + spec (work complete)
```

---

## Post-implementation FE coordination checklist (out of scope of this plan, but noted)

- FE must remove calls to `POST /api/v1/plans/generate` (replaced by `POST /api/v1/itinerary/generate`)
- FE must remove calls to `GET/PUT/PATCH /api/v1/groups/{id}/preferences`
- FE must adapt to new `PreferencesDto` shape (name/origin/crew/vibeTags + new suggested* nested objects)
- FE switches itinerary rendering to read `sections[]` by `key`
- Coordinate with EC data seeder owner: add `Activity.*` + `Food.*` tagged documents to enable the new sections in dev/staging
