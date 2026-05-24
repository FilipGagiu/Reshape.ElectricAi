# 2026-05-24 — User preferences: allow skipping sections (relax null checks)

## Context

User-facing preference DTOs (`PreferencesReplaceRequest`, `PreferencesPatchRequest`) already let the user omit a whole section by passing `null` at the top level (e.g. `Crew: null`). However, when a nested section IS provided, its required enum fields (`CrewDto.Kind`, `TransportSuggestionDto.Mode`, `AccommodationSuggestionDto.Type`) are non-nullable C# enums, so the user is forced to supply them. List items in `VibeTags` and `MustSeeArtists` are also currently rejected if null/empty.

The product intent is: a user may skip any section of the preferences form. Validation should still enforce **type integrity** (enum value is in range when present) and **length caps** (strings ≤ N characters), but should never reject a payload solely because a field is null/empty.

## Goal

Relax null-checks across user-preference DTOs and validators so a user can skip any section, while keeping type + length validation intact.

## Non-goals

- Changing AI-extracted DTOs (`AiExtractedCrew`, `AiExtractedTransportSuggestion`, `AiExtractedAccommodationSuggestion`). Their non-nullable enums are an LLM-output contract and are not user-facing.
- Changing entity (`UserPreferences`) shape — fields are already nullable.
- Changing mapping behavior — `PreferencesMappingExtensions` already accepts `Crew?.Kind` style and works with nullable nested fields.
- Changing top-level nullable behavior — already correct.

## Design

### 1. DTO records (Core)

Make nested-section enum fields nullable:

| File | Before | After |
|---|---|---|
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/CrewDto.cs` | `record CrewDto(CrewKind Kind, int? EstimatedSize)` | `record CrewDto(CrewKind? Kind, int? EstimatedSize)` |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/TransportSuggestionDto.cs` | `record TransportSuggestionDto(TransportMode Mode, string? Note)` | `record TransportSuggestionDto(TransportMode? Mode, string? Note)` |
| `src/Reshape.ElectricAi.Core/Dtos/Preferences/AccommodationSuggestionDto.cs` | `record AccommodationSuggestionDto(Accommodation Type, string? Note)` | `record AccommodationSuggestionDto(Accommodation? Type, string? Note)` |

### 2. Validators

`PreferencesReplaceRequestValidator` and `PreferencesPatchRequestValidator` (identical bodies — change in lockstep):

**Nested enum gating** — replace unconditional `IsInEnum()` with conditional:

```csharp
// Before
RuleFor(x => x.Crew!.Kind).IsInEnum().WithMessage("Crew.Kind must be a valid value.");

// After
RuleFor(x => x.Crew!.Kind)
    .IsInEnum().When(x => x.Crew!.Kind is not null)
    .WithMessage("Crew.Kind must be a valid value.");
```

Same pattern for `SuggestedTransport!.Mode` and `SuggestedAccommodation!.Type`.

**List item rules** — `VibeTags` and `MustSeeArtists`:

```csharp
// Before
RuleForEach(x => x.VibeTags!)
    .NotEmpty().WithMessage("VibeTag values must not be empty.")
    .Must(value => value is not null && value.Trim().Length is >= 1 and <= 60)
    .WithMessage("VibeTag values must be 1 to 60 characters.");

// After
RuleForEach(x => x.VibeTags!)
    .Must(value => value is null || value.Length <= 60)
    .WithMessage("VibeTag values must be 60 characters or fewer.");
```

Same shape for `MustSeeArtists` with limit `200`. Keep max-count + duplicate guards as-is (duplicate guards already filter `IsNullOrWhiteSpace`).

### 3. Mapping behavior (no code change)

`PreferencesMappingExtensions` already uses null-conditional access:

- `entity.CrewKind = request.Crew?.Kind;` — already returns `CrewKind?`. After making `Kind` itself nullable, the expression type is still `CrewKind?`. Compiles + behaves identically.
- Empty/whitespace list items pass through `NormalizeText`, which returns `null` and the foreach `continue`s, so no DB row is written for empty strings.

PATCH semantics: if the user sends `{ Crew: { Kind: null, EstimatedSize: 5 } }`, the entity's `CrewKind` is cleared and `CrewEstimatedSize` set to 5. To skip the section without clearing existing data, the user sends no `Crew` key at all (omitted from JSON → `Crew == null`).

### 4. Tests

Likely affected test files (verify in plan phase):

- `tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/FeedTargetingViaUserPrefsTests.cs`
- `tests/Reshape.ElectricAi.Plans.Tests/Unit/Services/Itinerary/PreferencesExtractorTests.cs`
- Any direct validator-unit tests for `PreferencesReplaceRequestValidator` / `PreferencesPatchRequestValidator`

Plan phase will grep + adjust. New tests to add:

1. `PreferencesReplaceRequest` with `Crew = { Kind = null, EstimatedSize = 5 }` passes validation.
2. `PreferencesReplaceRequest` with `VibeTags = [""]` passes validation (and mapping drops the empty entry).
3. `PreferencesReplaceRequest` with `SuggestedTransport = { Mode = null, Note = "x" }` passes validation.
4. Length cap still enforced: `VibeTags = ["a".repeat(61)]` fails.
5. Out-of-range enum still rejected: `Crew = { Kind = (CrewKind)999 }` fails.

## Risks

- Frontend may rely on the enum being non-nullable when reading from `PreferencesDto` responses. Mitigation: `PreferencesDto` itself is not changed; only request DTOs are. Response shape stays compatible (entity already nullable, mapping already projects nullable).
- AI-extracted DTOs unchanged — if you later want LLM output to skip enums too, separate change.

## Out-of-scope follow-ups

- Loosening `IsInEnum` to also coerce out-of-range to null instead of rejecting.
- Adding telemetry for "section skipped" rates.
