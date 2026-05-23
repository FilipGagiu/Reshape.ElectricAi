# AI plan generation — design spec

> **Status:** draft for user review
> **Date:** 2026-05-23
> **Slice owner:** Dev 1 (`Reshape.ElectricAi.Plans` + `Reshape.ElectricAi.Core`)
> **Cross-team touch:** Dev 2 owns the new `IOpenAiClient` impl in `Reshape.ElectricAi.AiChat`

## 1. Summary

A new wizard-driven plan-generation flow that replaces manual preference entry with AI-inferred preferences. The user answers 4-5 short questions in free text (questions are FE-defined; backend treats them as opaque `{questionId, questionText, answer}` triples), optionally adds a free-text addendum, and posts the bundle to `POST /api/v1/plans/generate`. The backend makes a single structured-output OpenAI call that returns three things at once:

1. **Inferred `UserPreferences`** — written to the existing 9-dimension preferences table. The user can edit them afterwards via the existing `PUT/PATCH /api/v1/preferences` endpoints.
2. **`PlanDto`** — the per-day plan structure already documented in `README.md`.
3. **`Tip`** — a 1-3 sentence personalized note tied to something the user actually said.

The free-text wizard answers are **ephemeral** — used once to build the prompt, never persisted. Only the resulting preferences and plan survive.

## 2. Scope

### In scope

- Individual user plan generation
- New `POST /api/v1/plans/generate` endpoint accepting wizard answers + free text
- AI fills `UserPreferences` (upsert via existing `IPreferencesService` pattern)
- New `Tip` column on `plans."Plans"` table
- New `IOpenAiClient` interface in `Core` + impl in `AiChat`
- Per-user rate limiting on plan generation (in-memory sliding window)
- Tone-of-voice + copy-rule enforcement in system prompt and post-generation sanitizer

### Out of scope (separate specs)

- Group plan generation (depends on Groups CRUD slice)
- VectorDb knowledge retrieval injected into the system prompt (works without it; add when ingest lands)
- Plan re-generation from edited preferences (separate endpoint, `POST /plans/{id}/regenerate`)
- Plan export (PDF/MD — already planned separately)
- Asynchronous / streamed generation (current call is sync, ~5-15s, within `OpenAi:Limits:TimeoutSeconds`)
- Multi-instance horizontal scale for rate limiting (in-memory is fine while deployment is single-instance per `PROJECT.md` known limitations)

## 3. Architecture

```
FE wizard (4-5 Q + freetext)
        │  POST /api/v1/plans/generate
        ▼
PlansController.GenerateAsync
        │
        ▼
IPlanGenerator (Core)  ◄── interface
        │
PlanGenerator (Plans/Services)  ◄── impl
        ├─► IRepository<UserPreferences> — upsert with nav-collection Clear+Add (see §5)
        ├─► IRepository<Plan> — persist Plan row (ContentJson jsonb + Tip column)
        ├─► IRateLimiter — per-user sliding window
        └─► IOpenAiClient (Core interface, AiChat impl)
                ├─► token + timeout + retry wrapper (CODE.md "OpenAI + cost discipline")
                └─► OpenAI chat.completions with response_format = json_schema

Returns: PlanGenerationResult { plan: PlanDto, preferences: PreferencesDto, tip: string }
```

### Library ownership

| Type | Location |
|---|---|
| `IPlanGenerator` | `Core/Services/IPlanGenerator.cs` |
| `PlanGenerator` | `Plans/Services/PlanGenerator.cs` |
| `IOpenAiClient` | `Core/Services/IOpenAiClient.cs` |
| `OpenAiClient` | `AiChat/Services/OpenAiClient.cs` |
| `IRateLimiter` | `Core/Services/IRateLimiter.cs` |
| `InMemorySlidingWindowRateLimiter` | `Plans/Services/InMemorySlidingWindowRateLimiter.cs` |
| `PlanGenerationRequest` / `PlanGenerationResult` / `WizardAnswer` | `Core/Dtos/Plans/` |
| `PlanGenerationRequestValidator` | `Plans/Validators/` |
| Prompts (system + tone) | `Plans/Services/Prompts/` (embedded resources) |
| Migration `AddPlanTip` | `Plans/Migrations/` |

`Plans` does not reference `AiChat` (CODE.md dependency graph). `IOpenAiClient` lives in `Core`, so DI bridges the two libraries at runtime via the Presentation host.

### Schema impact

Existing `plans."Plans"` table needs one new column:

| Column | Type | Null | Notes |
|---|---|---|---|
| `Tip` | `text` | yes | Personalized AI tip, plain text, 10..500 chars |

New migration `AddPlanTip` adds it. `ContentJson` (`jsonb`) and `xmin` are already present per `PlanConfiguration.cs` + memory.

## 4. Components

### `Core/Services/IPlanGenerator.cs`

```csharp
public interface IPlanGenerator
{
    Task<PlanGenerationResult> GenerateAsync(
        Guid userId,
        PlanGenerationRequest request,
        CancellationToken cancellationToken);
}
```

### `Core/Dtos/Plans/PlanGenerationRequest.cs`

```csharp
public sealed record PlanGenerationRequest(
    IReadOnlyList<WizardAnswer> Answers,
    string? FreeText);

public sealed record WizardAnswer(
    string QuestionId,
    string QuestionText,
    string Answer);
```

### `Core/Dtos/Plans/PlanGenerationResult.cs`

```csharp
public sealed record PlanGenerationResult(
    PlanDto Plan,
    PreferencesDto Preferences,
    string Tip);
```

`PlanDto`, `PlanDayDto`, `PlanFoodDto`, `PlanBudgetDto` match the shapes in `README.md` "Canonical JSON schemas — `POST /plans/generate`".

### `Core/Services/IOpenAiClient.cs`

```csharp
public interface IOpenAiClient
{
    // responseSchema is a JsonNode produced by System.Text.Json.Schema.JsonSchemaExporter
    // (or a hand-written const). The AiChat impl converts it to BinaryData for the
    // OpenAI SDK's ChatResponseFormat.CreateJsonSchemaFormat call. Keeping the Core
    // interface SDK-free preserves the dependency graph.
    Task<LlmStructuredResult<T>> CompleteStructuredAsync<T>(
        string systemPrompt,
        string userPrompt,
        System.Text.Json.Nodes.JsonNode responseSchema,
        string? model,
        CancellationToken cancellationToken)
        where T : class;
}

public sealed record LlmStructuredResult<T>(T Value, LlmUsage Usage);
public sealed record LlmUsage(int PromptTokens, int CompletionTokens, int CostCents);

public class LlmException : Exception { public string Code { get; } /* ... */ }
public sealed class LlmSchemaException : LlmException { /* ... */ }
```

### `AiChat/Services/OpenAiClient.cs`

- Wraps `OpenAI.Chat.ChatClient` from the official `OpenAI` 2.10.0 SDK.
- Enforces `OpenAi:Limits:MaxPromptTokens`, `MaxCompletionTokens`, `TimeoutSeconds`.
- 2-attempt retry with exponential backoff (1s, 2s) on transient HTTP errors.
- Pre-call tokenization via `Microsoft.ML.Tokenizers` cl100k_base; throws `LlmException("prompt-too-long")` if over the cap.
- Validates the deserialized envelope before returning: required fields populated, `plan.days.Count >= 1`, `tip.Length >= 10`. Schema violation on attempt 1 triggers one retry; attempt 2 also bad throws `LlmSchemaException`.
- Computes `CostCents` from `OpenAi:Models:<name>:PromptCentsPer1K` and `CompletionCentsPer1K`.
- Logs model, tokens, cost, latency at `Info` via `[LoggerMessage]` source-gen. No payload logging at `Info`; payloads only at `Debug` when `Logging:LogLlmPayloads=true`.

### `Plans/Services/PlanGenerator.cs`

```csharp
internal sealed partial class PlanGenerator : IPlanGenerator
{
    public PlanGenerator(
        IOpenAiClient openAi,
        IRepository<UserPreferences> prefsRepo,
        IRepository<Plan> planRepo,
        IRateLimiter rateLimiter,
        IConfiguration configuration,
        ILogger<PlanGenerator> logger);

    public async Task<PlanGenerationResult> GenerateAsync(
        Guid userId,
        PlanGenerationRequest request,
        CancellationToken cancellationToken);
}

internal sealed record AiPlanEnvelope(
    AiPreferences Preferences,
    AiPlan Plan,
    string Tip);
```

The constructor builds and caches the response schema once via `JsonSchemaExporter.GetJsonSchemaAsNode(typeof(AiPlanEnvelope), JsonOptions.Default)` and stores it in a `private readonly JsonNode _responseSchema` field. The system prompt is also loaded from the embedded resource once.

Orchestration steps inside `GenerateAsync`:

1. `await rateLimiter.AcquireAsync($"plan-gen:{userId}", ct)` — throws `TooManyRequestsException` on exceed.
2. Build user prompt (numbered Q+A list + "Additional notes:" block). System prompt is already cached.
3. `var llm = await openAi.CompleteStructuredAsync<AiPlanEnvelope>(_systemPrompt, userPrompt, _responseSchema, model, ct);`.
4. Upsert `UserPreferences` from `llm.Value.Preferences` using the existing per-dimension nav `.Clear()` + `.Distinct()` add pattern.
5. Sanitize `llm.Value.Tip` (replace em-dashes with hyphens; log `PlanTipSanitized` warning if any replaced).
6. Insert a new `plans."Plans"` row: `Id = Guid.NewGuid()`, `UserId`, `Scope = Individual`, `ContentJson = JsonSerializer.Serialize(llm.Value.Plan)`, `Tip = sanitizedTip`, `GeneratedUtc = DateTimeOffset.UtcNow`, `ExportedUtc = null`.
7. `await prefsRepo.SaveChangesAsync(ct)` — single Postgres transaction for the upsert + insert.
8. Map and return `PlanGenerationResult`.

### `Plans/Validators/PlanGenerationRequestValidator.cs`

- `Answers`: 1..10 items, no nulls.
- `WizardAnswer.QuestionId`: 1..64 chars, regex `^[a-z][a-z0-9_-]*$`.
- `WizardAnswer.QuestionText`: 1..500 chars.
- `WizardAnswer.Answer`: 1..2000 chars.
- `FreeText`: optional, 0..2000 chars.

Auto-registered by the reflection scan in `PlansModule.RegisterValidators` (memory note).

### `Presentation/Controllers/PlansController.cs`

```csharp
[Route("api/v1/plans")]
public sealed class PlansController : ControllerBase
{
    [HttpPost("generate")]
    [Authorize]
    public async Task<ActionResult<PlanGenerationResult>> GenerateAsync(
        [FromBody] PlanGenerationRequest request,
        CancellationToken cancellationToken);
}
```

Resolves `Guid userId` from `JwtRegisteredClaimNames.Sub` with `ClaimTypes.NameIdentifier` fallback (CODE.md auth rules). Delegates to `IPlanGenerator`. Returns 200 with `PlanGenerationResult` on success; errors flow through `ExceptionHandlerMiddleware` to the standard envelope.

### DI wiring

- `PlansModule.AddPlansModule`:
  - `services.AddScoped<IPlanGenerator, PlanGenerator>();`
  - `services.AddSingleton<IRateLimiter, InMemorySlidingWindowRateLimiter>();`
- `AiChatModule.AddAiChatModule` (new file, scaffolded lib):
  - `services.AddSingleton<IOpenAiClient, OpenAiClient>();`
- `Presentation/Program.cs`:
  - `builder.Services.AddAiChatModule(builder.Configuration);` next to `AddPlansModule`.

## 5. Data flow

```
1. FE collects answers + freetext, POSTs to /api/v1/plans/generate
   Body: { answers: [{ questionId, questionText, answer }, ...], freeText }

2. JwtBearer middleware → 401 if no/bad token (envelope via OnChallenge)

3. FluentValidationFilter → 400 with field-level details if request invalid

4. PlansController.GenerateAsync
   - Resolves Guid userId from JWT sub claim
   - Calls IPlanGenerator.GenerateAsync(userId, request, ct)

5. PlanGenerator.GenerateAsync (see Components §4 for step list)

6. Controller returns 200 with PlanGenerationResult { plan, preferences, tip }

7. Errors propagate as exceptions and are mapped by ExceptionHandlerMiddleware
   (see §6 Error envelope mapping).
```

### System prompt (cached at construction)

Lives in `Plans/Services/Prompts/PlanGeneratorSystemPrompt.md` as an embedded resource. Anchor content:

```
You are the Electric Castle first-timer assistant. Tone of voice:
warm, practical, no jargon, EN+RO mix only if the user writes in RO.
Never use em-dashes. Avoid "it's not X, it's Y" phrasing.

Your job: read the user's wizard answers + free-form notes, and produce
THREE things in a single structured response:

1. preferences — populate the EC preference dimensions from what they said.
   Allowed values are strict enums (see schema). When the user is silent
   on a dimension, leave the list empty / scalar null. Do not invent.

2. plan — a per-day Electric Castle plan covering Wed through Sun of EC 2025
   (5 days). Each day has transport (outbound/return), concerts (artist +
   stage + start/end), activities, and weather notes. Pull from your
   internal knowledge of EC 2025 lineup; do not fabricate artists not on
   the festival.

3. tip — 1-3 sentences of warm, personalized advice tied to something
   they actually said. Reference one specific detail (e.g. "since you
   mentioned bringing your dog...").

Budget currency is RON-cents (multiply RON by 100). Be realistic.
```

### User prompt template

```
User answered the wizard like this:

1. {Q1.questionText}
   → {Q1.answer}

2. {Q2.questionText}
   → {Q2.answer}

[... up to 10]

Additional notes from the user:
{freeText or "(none)"}

Now produce the preferences, plan, and tip via the response tool.
```

### Response schema (OpenAI `response_format = json_schema`)

A single JSON Schema describes the `AiPlanEnvelope`: `{ preferences, plan, tip }`. Generated once at process start via `System.Text.Json.Schema.JsonSchemaExporter` (or hand-written const if the exporter is awkward for enums). Cached in `OpenAiClient` keyed by the .NET `Type`.

Key constraints (enums match `Core/Enums/`):

- `preferences.ticketType`: `null | Standard | Vip | UltraVip | Black`
- `preferences.accommodation`: `null | VillageRental | Camping | CarCamping | RvCamping | Glamping`
- `preferences.transport`: `null | RideShare | Car | EcTrain | EcBus | Helicopter`
- `preferences.ageGroup`: `null | Under18 | Adult18To24 | Adult25To34 | Adult35To44 | Adult45Plus`
- `preferences.musicGenres[]`: enum, `maxItems: 11`
- `preferences.foodRestrictions[]`: enum, `maxItems: 11`
- `preferences.activities[]`: enum, `maxItems: 7`
- `preferences.artists[]`: free-text 1..200 chars, `maxItems: 20`
- `preferences.cuisines[]`: enum, `maxItems: 15`
- `plan.scope`: `individual` (locked)
- `plan.ticketType`: same enum as `preferences.ticketType` minus null
- `plan.days[]`: `minItems: 1, maxItems: 5`
- `tip`: `minLength: 10, maxLength: 500`

Server-side post-validation re-checks the same invariants before persistence (defense against schema-mode bugs in newer model versions).

### Preferences upsert logic

The `PlanGenerator` uses `IRepository<UserPreferences>` directly rather than calling `IPreferencesService.ReplaceAsync`. Reason: `IPreferencesService` is shaped around external API DTOs and calls `SaveChangesAsync` internally, which would split the prefs upsert and Plan insert into two transactions. The Plan generator needs a single transaction so a Plan row never exists without its corresponding prefs row.

The upsert mirrors the existing `PreferencesService` pattern (memory note):

- Load `UserPreferences` via `UserPreferencesWithChildrenSpec` (5 nav includes + `SplitQuery`).
- If null, create a new row attached to the user (lazy-create).
- Scalars: overwrite directly (AI returns null = clear, AI returns enum = set).
- Lists: `entity.<NavCollection>.Clear()` then add from `.Distinct()` — EF cascade-delete is configured.
- Set `UpdatedUtc = DateTimeOffset.UtcNow`.
- Do not `SaveChangesAsync` yet — batched with the Plan insert.

### Plan persistence

The new `plans."Plans"` row carries:

- `Id`: `Guid.NewGuid()`
- `UserId`: from JWT
- `Scope`: `PlanScope.Individual`
- `ContentJson`: `JsonSerializer.Serialize(env.Plan, JsonOptions.Default)` — stored as `jsonb`
- `Tip`: sanitized AI tip
- `GeneratedUtc`: `DateTimeOffset.UtcNow`
- `ExportedUtc`: `null`

A single `SaveChangesAsync` commits both the preferences upsert and the plan insert in one Postgres transaction.

### Configuration additions

```json
{
  "Chat": {
    "PlanGeneration": {
      "Model": "gpt-4o-mini",
      "Temperature": 0.7,
      "MaxCompletionTokens": 2048,
      "RateLimit": { "PerHour": 5 }
    }
  }
}
```

`Chat:PlanGeneration:Model` falls back to `Chat:DefaultModel`. `Chat:PlanGeneration:MaxCompletionTokens` overrides `OpenAi:Limits:MaxCompletionTokens` for this single call (PlanDto is heavier than a typical chat reply).

## 6. Error handling, cost, observability

### Error envelope mapping

All non-2xx responses flow through `ExceptionHandlerMiddleware` and produce the standard `{ error: { code, message, details } }` envelope.

| Trigger | Exception | HTTP | `error.code` | Notes |
|---|---|---|---|---|
| FluentValidation fails | `ValidationException` | 400 | `validation-failed` | per-field errors in `details` |
| User JWT missing/expired/invalid | (JwtBearer events) | 401 | `missing-token` / `token-expired` / `invalid-token` | existing wiring |
| User row not found | `NotFoundException` | 404 | `user-not-found` | rare; only if user deleted mid-call |
| OpenAI 429/500/timeout after retries | `LlmException` | 502 | `llm-unavailable` | wrapper sets `code` |
| OpenAI returned schema-violating JSON | `LlmSchemaException` | 502 | `llm-malformed-response` | retry exhausted; full payload logged at `Error` |
| Per-user rate limit hit | `TooManyRequestsException` | 429 | `rate-limit-exceeded` | `details: { retryAfterSeconds }` |
| Concurrent prefs edit | `DbUpdateConcurrencyException` → `ConflictException` | 409 | `preferences-concurrent-edit` | FE retries |
| Unhandled | catch-all | 500 | `internal-error` | real message logged, not exposed |

### Cost containment

Plan generation is neither chat nor embedding and therefore bypasses `chat.chat_budgets`. Two guardrails apply instead:

1. **Per-call hard ceiling** via the existing `IOpenAiClient` wrapper:
   - `OpenAi:Limits:MaxPromptTokens` (default 8000)
   - `OpenAi:Limits:MaxCompletionTokens` (raised to 2048 for plan generation via `Chat:PlanGeneration:MaxCompletionTokens`)
   - `OpenAi:Limits:TimeoutSeconds` (default 30)

2. **Per-user rate limit** via `IRateLimiter`:
   - Key: `plan-gen:{userId}`
   - Default: 5 generations per hour per user (`Chat:PlanGeneration:RateLimit:PerHour=5`)
   - On exceed → `TooManyRequestsException` carrying `retryAfterSeconds` (time until window reset)
   - In-memory only; matches the single-instance deployment limitation already documented in `PROJECT.md`

Per-call cost is logged via `LlmUsage`. The demo can show judges the exact dollar figure.

### Tone-of-voice + copy enforcement

The system prompt explicitly bans em-dashes and "it's not X, it's Y" phrasing (per `frontend/text-copy-design-language.md`). Post-generation sanitizer guards against model slip:

- `result.Tip = result.Tip.Replace("—", "-")` (replace em-dash with hyphen).
- If a replacement happens, log `PlanTipSanitized` at `Warning` with the count — signal to revisit the prompt.

### Observability — `[LoggerMessage]` source-gen events

| Event | Level | Fields |
|---|---|---|
| `PlanGenerationStarted` | Info | UserId, AnswerCount, FreeTextLength |
| `PlanGenerationCompleted` | Info | UserId, PlanId, PromptTokens, CompletionTokens, CostCents, ElapsedMs |
| `PlanGenerationLlmRetry` | Warning | UserId, Attempt, Reason |
| `PlanGenerationLlmFailed` | Error | UserId, ErrorCode, ElapsedMs |
| `PlanGenerationSchemaViolation` | Error | UserId, MissingField, RawResponseHash (payload only at `Debug` if `Logging:LogLlmPayloads=true`) |
| `PlanTipSanitized` | Warning | UserId, Replacements |
| `PlanGenerationRateLimited` | Warning | UserId, WindowResetUtc |

Request id propagated via the existing `Serilog.Context.LogContext` push.

**Never logged at `Info`:** raw answer text, raw freetext, raw AI response body, preference values. `Debug` only behind `Logging:LogLlmPayloads=true`.

### Idempotency / concurrency

- No idempotency key in v1. A double-click creates two plan rows. Users delete via the existing `DELETE /plans/{id}`.
- `UserPreferences` upsert is concurrent-safe via the existing `xmin` token (memory pattern). Two parallel generations for the same user → one wins, the other throws `DbUpdateConcurrencyException`, mapped to 409 `preferences-concurrent-edit`. FE retries.

### Cancellation

- `CancellationToken` is propagated end-to-end (CODE.md mandate).
- Client disconnect (Kestrel cancels the token) cancels the OpenAI SDK call.
- Partial work is rolled back: nothing is persisted before the AI call returns successfully, so a cancellation mid-call leaves the DB untouched.

## 7. Testing

### Strategy

- Mock `IOpenAiClient` only — no real OpenAI calls in tests.
- Real Postgres via Testcontainers (CODE.md rule; existing `Plans.Tests` fixture is reused).
- Two layers: unit (`PlanGeneratorTests`) and integration (`PlansControllerGenerateTests` via `WebApplicationFactory<Program>`).
- New `FakeOpenAiClient : IOpenAiClient` in `Plans.Tests/Fakes/` — returns canned `AiPlanEnvelope` + `LlmUsage`, tracks invocation count, supports `WithEnvelope(env)`, `WithSequence(envs...)`, `WithExceptions(exs...)`.

### Unit tests — `PlanGeneratorTests`

| # | Method | Scenario |
|---|---|---|
| 1 | `GenerateAsync_ValidRequest_UpsertsPreferences` | Fake returns full prefs envelope → assert row in `UserPreferences` matches |
| 2 | `GenerateAsync_ValidRequest_InsertsPlanRow` | Assert `plans."Plans"` has 1 row with `Tip` populated, `ContentJson` deserializable to `PlanDto` |
| 3 | `GenerateAsync_PrefsListsContainDuplicates_DedupsBeforePersist` | Fake returns `["Rock","Rock","House"]` → persisted `["Rock","House"]` |
| 4 | `GenerateAsync_ExistingPrefs_ReplacesLists` | Pre-seed `["Techno"]`; fake returns `["House"]`; post-state is `["House"]` only |
| 5 | `GenerateAsync_TipContainsEmDash_Sanitized` | Fake returns `"Be ready — bring boots"` → persisted Tip = `"Be ready - bring boots"`; warning logged |
| 6 | `GenerateAsync_LlmThrowsTransient_RetriesAndSucceeds` | Throws on attempt 1, returns on attempt 2; final OK; call count == 2 |
| 7 | `GenerateAsync_LlmExhaustsRetries_ThrowsLlmException` | Fake always throws → `LlmException`; no DB writes |
| 8 | `GenerateAsync_SchemaInvalidResponse_ThrowsLlmSchemaException` | Envelope with `plan.days = []` → `LlmSchemaException`; no DB writes |
| 9 | `GenerateAsync_UserNotFound_ThrowsNotFound` | No user seeded → `NotFoundException` |
| 10 | `GenerateAsync_RateLimitExceeded_ThrowsTooManyRequests` | 5 successful gens, 6th throws with `retryAfterSeconds > 0` |
| 11 | `GenerateAsync_ConcurrentPrefsUpdate_PropagatesConcurrencyException` | Two parallel calls, same user, same prefs row pre-loaded → one succeeds, other throws |
| 12 | `GenerateAsync_CancellationRequested_ThrowsOperationCanceledException` | Cancel token after 1ms → propagates; no DB writes |

### Integration tests — `PlansControllerGenerateTests`

Per-test fresh DB (existing Testcontainers fixture). Register `FakeOpenAiClient` in `ConfigureWebHost` to override the real `IOpenAiClient`. Auth via existing test token helper.

| # | Method | Scenario | Asserts |
|---|---|---|---|
| 1 | `Generate_NoToken_Returns401WithEnvelope` | No `Authorization` header | 401, `code: "missing-token"` |
| 2 | `Generate_ExpiredToken_Returns401TokenExpired` | Token issued in past | 401, `code: "token-expired"` |
| 3 | `Generate_EmptyAnswers_Returns400` | `answers: []` | 400, `code: "validation-failed"`, details contain `Answers` |
| 4 | `Generate_FreeTextTooLong_Returns400` | freetext 2001 chars | 400 |
| 5 | `Generate_HappyPath_Returns200WithFullResult` | Valid request, full envelope | 200 body has `plan`, `preferences`, `tip`; DB has Plan + Preferences rows |
| 6 | `Generate_HappyPath_PersistsTipColumnNotInsideJson` | Inspect raw row | `Tip` column populated; `ContentJson` does NOT contain `tip` key |
| 7 | `Generate_LlmThrows_Returns502LlmUnavailable` | Fake always throws `LlmException` | 502, `code: "llm-unavailable"` |
| 8 | `Generate_LlmReturnsBadShape_Returns502Malformed` | Fake returns envelope missing `plan` | 502, `code: "llm-malformed-response"` |
| 9 | `Generate_RateLimitHit_Returns429WithRetryAfter` | 6 calls in window | 6th = 429, `code: "rate-limit-exceeded"`, `details.retryAfterSeconds > 0` |
| 10 | `Generate_AfterSuccess_GetPreferencesReflectsAiFilled` | POST generate → GET `/preferences` | Response prefs match what fake returned |
| 11 | `Generate_SuccessfulGen_LogsLlmUsage` | `ITestLoggerProvider` captures | `PlanGenerationCompleted` event recorded with cost cents |

### Test fixtures + utilities

- Reuse `PlansTestFactory : WebApplicationFactory<Program>` (existing). Add `WithFakeOpenAi(canned)` extension.
- Reuse `TestUserSeed` helper (existing).
- New `Plans.Tests/Fakes/FakeOpenAiClient.cs`.
- New `Plans.Tests/Helpers/PlanAssertions.cs` (FluentAssertions custom extensions).

### Out of scope for this slice

- Actual OpenAI calls (manual smoke before demo).
- `OpenAiClient` itself (covered by `AiChat.Tests` when that lib gains tests).
- Performance under concurrent load.

### Coverage gate

None per CODE.md. Target: 12 unit + 11 integration = 23 tests.

## 8. NuGet packages

No new NuGet additions expected. Confirmed available:

- `OpenAI` 2.10.0 (already on AiChat or pending; verify `AiChat.csproj`)
- `Microsoft.ML.Tokenizers` + `Cl100kBase` (already in CODE.md stack table)
- `FluentValidation` 12.1.1 (already in Plans)
- `Testcontainers.PostgreSql` 4.x (already in Plans.Tests)

If `OpenAI` 2.10.0 is missing from `Reshape.ElectricAi.AiChat.csproj`, that is the only addition — flagged per CODE.md §6a and installed by a dev before implementation starts.

## 9. Migration order for implementation

1. Add `Tip text NULL` to `Plan` entity + `PlanConfiguration` → generate migration `AddPlanTip` → apply.
2. Add `IOpenAiClient` + DTOs in `Core`.
3. Add `OpenAiClient` impl + `AiChatModule.AddAiChatModule` in `AiChat`. Wire in `Program.cs`.
4. Add `IRateLimiter` + `InMemorySlidingWindowRateLimiter` in `Plans`.
5. Add `IPlanGenerator` + DTOs in `Core`.
6. Add `PlanGenerator` + prompts + validators in `Plans`.
7. Add `PlansController.GenerateAsync` in `Presentation`.
8. Add `LlmException` / `LlmSchemaException` mapping to `ExceptionHandlerMiddleware`.
9. Add `Chat:PlanGeneration:*` defaults to `appsettings.json`.
10. Add tests (unit + integration).
11. Smoke test against the real OpenAI key in dev.

## 10. Open questions / follow-ups

- VectorDb knowledge retrieval is deferred. When ingest lands, add an optional `IVectorSearchService` call to the `PlanGenerator` and inject the retrieved chunks into the system prompt. Token-budget-aware truncation lives in the wrapper.
- `IRateLimiter` is an in-memory sliding window for v1. If/when horizontal scale lands, swap to Redis or Postgres-backed (advisory lock + expiry). Interface stays the same.
- `Tip` column may be promoted to a structured `Tips[]` later if the brief evolves. The single-string shape is the minimal v1.
- A `POST /plans/{id}/regenerate` endpoint (re-run with edited prefs, no wizard) is a near-future follow-up. The `PlanGenerator` is structured so this is one extra method.
