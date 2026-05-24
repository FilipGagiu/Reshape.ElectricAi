# Conversations endpoint — multi-turn chat with persistence + gpt-5-mini swap

## CLAUDE.md non-negotiable phase list (restated verbatim per CLAUDE.md mandate)

> 1. **Invoke task-specific superpowers skill(s)** — match the task to a skill from §7. Fire BEFORE entering plan mode. Named mappings:
>    - New feature / behavior change → `superpowers:brainstorming`
>    - Bug, test failure, unexpected behavior, build failure → `superpowers:systematic-debugging`
>    - Implementation that admits unit tests → `superpowers:test-driven-development`
>    - About to claim "done" / "fixed" / "passing" → `superpowers:verification-before-completion`
>
>    If none of the named mappings fit, scan the full installed superpowers skill list for any skill that might help. If still nothing fits, proceed without one — that's an acceptable outcome, but document the full-list-scan result in the plan's Phase 1 application note. Silent skipping is not acceptable.
> 2. **Enter plan mode** (`EnterPlanMode`) — before ANY file edit. No exceptions for "small" or "trivial".
> 3. **Inventory / explore** — gather facts via Explore agents (parallel where useful) or direct reads. Do not guess.
> 4. **Design** — propose specific custom agents for review, exploration, or design feedback (NOT implementation — see §2). Review-agent dispatches MUST include "verify CODE.md compliance against the changed files" as an explicit directive. Recommend; do not decide unilaterally.
> 5. **Write the plan** to `.claude/plans/<slug>.md`. **Every plan MUST start by restating this phase list verbatim** so no phase is silently skipped.
> 6. **`ExitPlanMode`** — the single approval gate. Wait for explicit user approval.
> 7. **Execute** — YOU edit the files; only dispatch agents for review or parallel exploration. **Re-read [CODE.md](CODE.md) before each code edit** and verify the change honors every rule there. After approval.
> 8. **Verify** — build + tests + visible evidence. No "trust me" claims.
> 9. **Promote learnings to memory** — `/si:remember` for facts; direct-edit CODE.md (code rules), CLAUDE.md (workflow), or PROJECT.md (project context) for enforced rules. Penultimate step.
> 10. **Delete the plan file** — last step. Code + commit history is the source of truth after.

### Phase 1 application note

- `superpowers:brainstorming` invoked at session start (matches "new feature/behavior change"). Brainstorm output captured below in the Decisions section. User explicitly approved entity/endpoint design in chat, then issued `/goal Complete full implementation`.
- `superpowers:test-driven-development` partially applicable — will be applied to the new domain code (ConversationService logic + new validators). Endpoint integration tests added post-implementation matching the existing `ConversationControllerTests` pattern. xUnit + Testcontainers.PostgreSql infra reused from `Plans.Tests`.
- `superpowers:verification-before-completion` will run at Phase 8 before claiming done.

---

## Context

`Reshape.ElectricAi.Presentation/Controllers/ConversationController.cs` is currently a **stateless one-shot RAG endpoint** at `POST /api/v1/conversation` (anonymous, max 500 chars). The product wants persistent multi-turn conversations tied to authenticated users, with a 20-message cap per conversation and a concurrency lock so a user cannot fire a second message while the bot is still generating.

In the same change, the team is swapping the OpenAI model for both the renamed one-shot endpoint and the new multi-turn endpoint to **`gpt-5-mini`**.

Naming collision: `ConversationController` name + plural-vs-singular route fragility. Decision: rename the existing one-shot to `Ask*` (`/api/v1/ask`), free up the `Conversation*` namespace for the multi-turn slice.

---

## Decisions (brainstorm output — all locked with the user)

| Topic | Decision |
|---|---|
| Streaming? | No. Full reply per POST. |
| Concurrency lock | DB column on `Conversation` row, atomic acquire via `ExecuteUpdateAsync`, stale-lock cleanup > 60s |
| Context per turn | Full prior history (User+Bot turns) + fresh RAG hits on the new message |
| Model | `gpt-5-mini` for both the renamed `/ask` and the new `/conversations` paths |
| Pricing | Hardcoded in `appsettings.json` (per goal — avoid secret edits). `OPENAI_API_KEY` user-secret stays. |
| Title | Auto-derived from first user message (first ~60 chars, word boundary) |
| 21st message | 409 `conversation-full` |
| Naming | Rename old `/api/v1/conversation` → `/api/v1/ask`; new endpoints under `/api/v1/conversations` |
| `/ask` auth | Stays `[AllowAnonymous]` (no change beyond rename + model swap) |
| `UserContext` field | Carried into both `StartConversationRequest` and `ContinueConversationRequest` (same shape, same RAG semantics) |
| Pagination on list | None — return all of the caller's conversations |
| Auth on new endpoints | `[Authorize]` (User role, JWT) |
| Per-message char cap | 1000 |
| New Postgres schema | `chat` (per PROJECT.md plan) |
| Test project | Tests added to existing `Reshape.ElectricAi.Plans.Tests` next to current integration tests (matches established cross-cutting pattern). No new test project. |

---

## Implementation

### File changes — overview

| Group | Change |
|---|---|
| Rename old one-shot to `Ask*` | Rename 7 files + adjust references everywhere. List below. |
| New Conversation entity layer | 2 new entities, 1 enum, 2 EF configurations, 1 new `ChatDbContext` + closing repo class, 1 migration. |
| New service + DTOs + validators | 1 service interface (Core) + impl (AiChat), 5 DTOs (Core), 2 validators (AiChat). |
| New controller | `ConversationsController` in Presentation. |
| OpenAI client extension | New `CompleteChatAsync(messages, model, maxCompletionTokens, ct)` overload on `IOpenAiClient`; refactor existing `CompleteFreeTextAsync` to call through it (no behavior change). |
| Config | `appsettings.json` adds `OpenAi:Models:gpt-5-mini`, `Ask` section, `Conversation` section. Old `Conversation` section content migrates to `Ask`. `Conversation` section repurposed for the multi-turn path. |
| Module wiring | `AiChatModule` registers new entities, new service, new validators, new `ChatDbContext` migration on startup. |
| Tests | New integration test class for `ConversationsController` + 2 new unit test classes (service + validators), updates to the existing `ConversationControllerTests` for rename. |

### Rename: existing one-shot → `Ask*`

| Old path | New path |
|---|---|
| `src/Reshape.ElectricAi.Presentation/Controllers/ConversationController.cs` | `src/Reshape.ElectricAi.Presentation/Controllers/AskController.cs` (explicit `[Route("api/v1/ask")]`, lowercase) |
| `src/Reshape.ElectricAi.Core/Services/IConversationService.cs` | `src/Reshape.ElectricAi.Core/Services/IAskService.cs` (method stays `AskAsync`) |
| `src/Reshape.ElectricAi.Core/Dtos/Conversation/ConversationRequest.cs` | `src/Reshape.ElectricAi.Core/Dtos/Ask/AskRequest.cs` (field `QuestionText` → `QuestionText` unchanged; same `UserContext` shape) |
| `src/Reshape.ElectricAi.Core/Dtos/Conversation/ConversationResponse.cs` | `src/Reshape.ElectricAi.Core/Dtos/Ask/AskResponse.cs` (field `Answer` unchanged) |
| `src/Reshape.ElectricAi.AiChat/Services/ConversationService.cs` | `src/Reshape.ElectricAi.AiChat/Services/AskService.cs` (logic unchanged) |
| `src/Reshape.ElectricAi.AiChat/Configuration/ConversationOptions.cs` | `src/Reshape.ElectricAi.AiChat/Configuration/AskOptions.cs` (section name `Ask`) |
| `src/Reshape.ElectricAi.AiChat/Validators/ConversationRequestValidator.cs` | `src/Reshape.ElectricAi.AiChat/Validators/AskRequestValidator.cs` |
| `tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/ConversationControllerTests.cs` | `tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/AskControllerTests.cs` (route → `/api/v1/ask`, DTOs → `AskRequest`/`AskResponse`) |
| `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/ConversationApiFactory.cs` | `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/AskApiFactory.cs` |
| `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/ConversationFakeOpenAiClient.cs` | `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/AskFakeOpenAiClient.cs` |

`AskController` keeps `[AllowAnonymous]`. `AskService` keeps the existing system prompt + RAG flow verbatim. `AskOptions` keeps the existing defaults (`ScoreThreshold=0.4`, `TopKPerSource=4`, `TopKFinal=6`, `MaxCompletionTokens=512`) but updates `Model` default to `"gpt-5-mini"`.

### New entities (`Reshape.ElectricAi.AiChat/Entities/`)

```csharp
// chat.conversations
public class Conversation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime LastMessageUtc { get; set; }
    public int UserMessageCount { get; set; }
    public bool IsGenerating { get; set; }
    public DateTime? GeneratingStartedUtc { get; set; }
    public uint Xmin { get; set; }
    public List<ConversationMessage> Messages { get; set; } = [];
}

// chat.conversation_messages
public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ConversationActor Actor { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public int OrderIndex { get; set; }
    public Conversation Conversation { get; set; } = null!;
}

public enum ConversationActor : byte { User = 0, Bot = 1 }
```

EF configurations (`AiChat/Persistence/Configurations/`):

- `ConversationConfiguration` — table `conversations`; `Title` varchar(120) required; `LastMessageUtc` indexed desc with leading `UserId`; `IsGenerating` default false; `UserMessageCount` default 0; `Xmin` via `.IsRowVersion()` (Npgsql `xmin` system column convention used by Plans entities — confirm against the existing PlansDbContext entity mapping when implementing; if Plans uses a different attribute, mirror that exactly).
- `ConversationMessageConfiguration` — table `conversation_messages`; `Content` varchar(2000); `Actor` stored as `smallint` via `.HasConversion<byte>()`; index `(ConversationId, OrderIndex)`; FK back to `Conversation` with cascade delete.

`ChatDbContext` (new):

```csharp
public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("chat");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatDbContext).Assembly);
    }
}
```

`ChatDbContextFactory` (design-time, mirrors `FeedDbContextFactory`).

`ChatRepository<T>` closing class:

```csharp
public sealed class ChatRepository<T>(ChatDbContext context)
    : EfRepository<ChatDbContext, T>(context) where T : class;
```

Migration: `dotnet ef migrations add InitChatSchema -p src/Reshape.ElectricAi.AiChat -s src/Reshape.ElectricAi.Presentation -- --context ChatDbContext`. Apply via `Program.cs` startup migration (mirrors Plans/VectorDb/Feed pattern at `Program.cs:149-155`).

`AiChat.csproj` needs the EF packages — they are already declared (verified at `Reshape.ElectricAi.AiChat.csproj` per CODE.md follow-up item that mentioned them as unused; they become used by this slice).

### New DTOs (`Reshape.ElectricAi.Core/Dtos/Conversation/`)

```csharp
// Request to create a new conversation
public sealed record StartConversationRequest(
    string Message,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null);

// Request to continue an existing conversation
public sealed record ContinueConversationRequest(
    string Message,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null);

public sealed record ReplyDto(string Message, ConversationActor Actor, DateTime CreatedUtc);

public sealed record ConversationSummaryDto(
    Guid Id, string Title, DateTime CreatedUtc, DateTime LastMessageUtc, int UserMessageCount);

public sealed record ConversationDetailDto(
    Guid Id, string Title, DateTime CreatedUtc, IReadOnlyList<ReplyDto> Replies);

public sealed record StartConversationResponse(
    Guid Id, string Title, ReplyDto Reply);

public sealed record ContinueConversationResponse(ReplyDto Reply);
```

`ConversationActor` enum lives in `Reshape.ElectricAi.AiChat/Entities/` (per CODE.md "Core MUST NOT reference any feature lib" — but DTOs in Core need the enum value in their public surface). **Decision:** move the enum to `Reshape.ElectricAi.Core/Enums/ConversationActor.cs` (matches `UserRole`, `Category`, etc. — enums live in Core). Entity references `Core.Enums.ConversationActor`.

### New service interface (`Reshape.ElectricAi.Core/Services/IConversationService.cs`)

```csharp
public interface IConversationService
{
    Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<ConversationDetailDto> GetAsync(Guid userId, Guid conversationId, CancellationToken ct = default);
    Task<StartConversationResponse> StartAsync(Guid userId, StartConversationRequest request, CancellationToken ct = default);
    Task<ContinueConversationResponse> ContinueAsync(Guid userId, Guid conversationId, ContinueConversationRequest request, CancellationToken ct = default);
}
```

(The same interface name was used by the old one-shot. After the rename, `IConversationService` is free to be redefined here for the multi-turn flow.)

### Service implementation (`Reshape.ElectricAi.AiChat/Services/ConversationService.cs`)

Flow for `ContinueAsync`:

1. Atomic acquire: `ExecuteUpdateAsync` on `ChatDbContext.Conversations.Where(c => c.Id == convId && c.UserId == userId && c.UserMessageCount < 20 && (!c.IsGenerating || c.GeneratingStartedUtc < UtcNow - 60s))` — set `IsGenerating=true, GeneratingStartedUtc=UtcNow`.
2. If 0 rows updated, **re-read** the row to differentiate:
   - not found / wrong user → `NotFoundException("not-found", ...)`
   - `UserMessageCount >= 20` → `ConflictException("conversation-full", ...)`
   - `IsGenerating == true && GeneratingStartedUtc >= cutoff` → `ConflictException("conversation-busy", ...)`
3. `try` block:
   1. Append user `ConversationMessage` (Actor=User, Content=request.Message, OrderIndex = currentCount*2).
   2. Persist (SaveChanges) so the user message lands even if LLM fails.
   3. Run RAG: same three vector-search calls used by `AskService` (sequential, per the existing scoped-VectorDbContext constraint already captured in memory).
   4. Build `IReadOnlyList<ChatMessage>`: system message (reuse `AskService.SystemPrompt` constant — extract to a shared `internal static class ChatPrompts` in `AiChat/Services/`); then in order, every historical `ConversationMessage` as User/Assistant; final User message = context block + new question.
   5. Call `openAi.CompleteChatAsync(messages, opts.Model, opts.MaxCompletionTokens, ct)`.
   6. Append bot `ConversationMessage` (Actor=Bot, OrderIndex+1).
   7. Update `Conversation.LastMessageUtc = UtcNow`, `UserMessageCount++`.
   8. SaveChanges.
4. `finally`: release lock via `ExecuteUpdateAsync` setting `IsGenerating=false, GeneratingStartedUtc=null`. Runs even on exception. Use `CancellationToken.None` for the release update so caller-cancel doesn't leave a stuck lock.

`StartAsync` flow:

1. Build title: `Message.Trim().ReplaceLineEndings(" ")`, collapse runs of whitespace, truncate to 60 chars at word boundary (last space ≤ 60), append `…` if truncated. Pure function — easy to unit-test.
2. Create new `Conversation` with `IsGenerating=true, GeneratingStartedUtc=UtcNow, UserMessageCount=0, Title=<derived>`, save, then run the same protected flow as `ContinueAsync` from step 3.1 onward.
3. Return `StartConversationResponse(Id, Title, Reply)`.

`ListAsync`: `Where(UserId == userId).OrderByDescending(LastMessageUtc).Select(...)` to `ConversationSummaryDto`. No paging.

`GetAsync`: `Conversations.Include(Messages.OrderBy(OrderIndex)).Where(Id == id && UserId == userId).FirstOrDefaultAsync()` → 404 if null; else project to `ConversationDetailDto`. Use the closing repository where it fits; for projection-heavy reads, raw `DbContext` access inside the service is acceptable (matches `PreferencesService` precedent).

### OpenAI client extension

Adding to `Reshape.ElectricAi.Core/Services/IOpenAiClient.cs`:

```csharp
Task<LlmTextResult> CompleteChatAsync(
    IReadOnlyList<OpenAi.Chat.ChatMessage> messages,
    string? model,
    int? maxCompletionTokens,
    CancellationToken cancellationToken);
```

**Problem:** `OpenAi.Chat.ChatMessage` is from the OpenAI SDK and would force `Core` to take a dependency on the OpenAI package — violates CODE.md "Core MUST NOT reference any feature lib" and the spirit of keeping Core dependency-light. 

**Fix:** introduce a Core-level abstraction:

```csharp
// Core/Services/LlmChatMessage.cs
public sealed record LlmChatMessage(LlmChatRole Role, string Content);
public enum LlmChatRole { System, User, Assistant }
```

`IOpenAiClient.CompleteChatAsync(IReadOnlyList<LlmChatMessage>, ...)` accepts the Core type. The AiChat `OpenAiClient` translates `LlmChatMessage` → `OpenAI.Chat.ChatMessage` internally. `CompleteFreeTextAsync` becomes a 2-line wrapper that builds `[System, User]` and delegates to `CompleteChatAsync`. Cost-tracking, retry, timeout, transient-classification all stay in one place.

### Controller (`Reshape.ElectricAi.Presentation/Controllers/ConversationsController.cs`)

```csharp
[ApiController]
[Authorize]
[Route("api/v1/conversations")]
[Produces("application/json")]
public sealed class ConversationsController(IConversationService conv) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryDto>>> ListAsync(CancellationToken ct)
        => Ok(await conv.ListAsync(CurrentUserId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetailDto>> GetAsync(Guid id, CancellationToken ct)
        => Ok(await conv.GetAsync(CurrentUserId(), id, ct));

    [HttpPost]
    public async Task<ActionResult<StartConversationResponse>> StartAsync(
        [FromBody] StartConversationRequest req, CancellationToken ct)
    {
        var result = await conv.StartAsync(CurrentUserId(), req, ct);
        return CreatedAtAction(nameof(GetAsync), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}")]
    public async Task<ActionResult<ContinueConversationResponse>> ContinueAsync(
        Guid id, [FromBody] ContinueConversationRequest req, CancellationToken ct)
        => Ok(await conv.ContinueAsync(CurrentUserId(), id, req, ct));

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new UnauthorizedException("missing-sub", "Token missing subject.");
        return Guid.Parse(sub);
    }
}
```

The `CurrentUserId` pattern is borrowed from `PreferencesController`/`GroupsController`. If a shared extension already exists in the Presentation project, use it; otherwise inline as above.

### Validators (`Reshape.ElectricAi.AiChat/Validators/`)

```csharp
public sealed class StartConversationRequestValidator : AbstractValidator<StartConversationRequest>
{
    public StartConversationRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
    }
}

public sealed class ContinueConversationRequestValidator : AbstractValidator<ContinueConversationRequest>
{
    public ContinueConversationRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
    }
}
```

`AiChatModule.RegisterValidators` already scans the assembly — no manual wiring.

### Config (hardcoded — no user-secret changes)

`appsettings.json`:

```json
"OpenAi": {
  "Limits": {
    "MaxPromptTokens": 16000,   // raised from 8000 to fit 20 turns + RAG
    "MaxCompletionTokens": 1024,
    "TimeoutSeconds": 30
  },
  "Models": {
    "gpt-4o-mini": { "PromptCentsPer1K": 0.015, "CompletionCentsPer1K": 0.060 },
    "gpt-5-mini":  { "PromptCentsPer1K": 0.025, "CompletionCentsPer1K": 0.200 }
  }
},
"Ask": {
  "Model": "gpt-5-mini",
  "MaxCompletionTokens": 512,
  "ScoreThreshold": 0.4,
  "TopKPerSource": 4,
  "TopKFinal": 6
},
"Conversation": {
  "Model": "gpt-5-mini",
  "MaxCompletionTokens": 1024,
  "ScoreThreshold": 0.4,
  "TopKPerSource": 4,
  "TopKFinal": 6,
  "UserMessageCap": 20,
  "MaxMessageChars": 1000,
  "TitleMaxChars": 60,
  "LockTimeoutSeconds": 60
}
```

Pricing values for `gpt-5-mini` are illustrative hardcoded estimates (~$0.25/1M input, ~$2/1M output). Real-money accuracy here is not critical because (a) there is no budget enforcement on `/ask` or `/conversations` in v1, and (b) the user explicitly chose "hardcode the easiest values, even if they're unsafe". Cost tracking still logs through Serilog so spend can be reconciled later from logs.

`AiChatModule` adds:

```csharp
var connectionString = configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

services.AddDbContext<ChatDbContext>(opts =>
    opts.UseNpgsql(connectionString, n =>
        n.MigrationsHistoryTable("__EFMigrationsHistory", "chat")));

services.AddScoped<IRepository<Conversation>, ChatRepository<Conversation>>();
services.AddScoped<IRepository<ConversationMessage>, ChatRepository<ConversationMessage>>();

services.AddOptions<AskOptions>().Bind(configuration.GetSection("Ask"));
services.AddOptions<ConversationOptions>().Bind(configuration.GetSection("Conversation"));

services.AddScoped<IAskService, AskService>();
services.AddScoped<IConversationService, ConversationService>();
```

(Per CODE.md DI-shadow rule: per-entity closed registrations, not open-generic — Plans already owns the open-generic slot.)

`Program.cs` adds a `ChatDbContext` migration call alongside the existing three:

```csharp
var chatDb = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
await chatDb.Database.MigrateAsync();
```

### Tests (existing `Reshape.ElectricAi.Plans.Tests`)

New files under `Integration/`:

- `Integration/Fixtures/ConversationsApiFactory.cs` — mirrors `ConversationApiFactory` but registers `ConversationsFakeOpenAiClient` (which echoes a deterministic reply derived from the last user message so the integration test can assert prompt-flow correctness).
- `Integration/Fixtures/ConversationsFakeOpenAiClient.cs` — implements all three `IOpenAiClient` methods including the new `CompleteChatAsync`. Captures the last-seen messages list on a thread-safe field so tests can assert "history flowed in".
- `Integration/Endpoints/ConversationsControllerTests.cs`:
  - `Start_ValidMessage_Returns201WithGuidAndTitle`
  - `Start_MessageTooLong_Returns400`
  - `Start_EmptyMessage_Returns400`
  - `Start_Unauthenticated_Returns401`
  - `Continue_AppendsToHistory_AndReturnsReply`
  - `Continue_NonOwner_Returns404`
  - `Continue_ConcurrentRequest_Returns409Busy` (two parallel POSTs to the same id; one wins, one gets 409 `conversation-busy`)
  - `Continue_StaleLock_AcquiredBySecondRequest` (forces `GeneratingStartedUtc` 5 minutes ago via direct DB write; new POST succeeds)
  - `Continue_AfterTwentyUserMessages_Returns409Full` (drives 20 successful turns then asserts 21st returns 409 `conversation-full`) — heavy but valuable. May parameterize to 5 in dev and 20 only in CI flag if it's slow.
  - `List_ReturnsOnlyCallerConversations_OrderedByLastMessageDesc`
  - `Get_NonOwner_Returns404`
  - `Continue_LlmFailure_LeavesLockReleased` (`ConversationsFakeOpenAiClient` flag throws on next call; assert second request after the failure succeeds)
- `Unit/Services/ConversationTitleDerivationTests.cs` — pure function: whitespace, truncation, word boundary, emoji, empty trim.
- `Unit/Services/ConversationServiceTests.cs` — orchestration logic with fakes (`FakeRepo<Conversation>`, `FakeVectorSearchService` already exists in `Plans.Tests/Fakes/`, `ConversationsFakeOpenAiClient`). Asserts: messages persisted in correct order, prompt structure includes system+history+context+question, lock acquired before LLM call and released after.

Old `ConversationControllerTests` renamed to `AskControllerTests`, route + DTO names swapped.

### Verification

After implementation:

```bash
dotnet build                          # must succeed, no new warnings (TreatWarningsAsErrors=true)
dotnet test tests/Reshape.ElectricAi.Plans.Tests   # all green
```

Manual smoke (dev with real Postgres):

```bash
dotnet run --project src/Reshape.ElectricAi.Presentation
# register a user (via existing /auth/register), grab the JWT, then:
curl -X POST http://localhost:5217/api/v1/conversations \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"message":"What time do gates open?"}'
# returns 201 + {id, title, reply}
curl -H "Authorization: Bearer $TOKEN" http://localhost:5217/api/v1/conversations
# returns array of 1
# hit the renamed /ask
curl -X POST http://localhost:5217/api/v1/ask -H "Content-Type: application/json" \
  -d '{"questionText":"How do I get there?"}'
# returns answer (no auth)
```

If OpenAI key is not available in user-secrets, the unit/integration tests still pass (fake clients), but the manual smoke step is skipped — flagged in the verification report.

### Review-agent dispatches (Phase 7 — after the work is done)

Three review passes, parallel, each one with **"verify CODE.md compliance against the changed files"** explicitly in the prompt per CLAUDE.md §2 + §6b:

1. `Code Reviewer` — overall correctness, naming, EF patterns, DI shadowing, lock atomicity, prompt safety. Scope: all changed files.
2. `Security Engineer` — auth correctness on new endpoints, no cross-user data leak in GET, JWT claim parsing, JSON body limits, log-payload scrubbing on prompt/completion content (CODE.md: never log secrets/full prompts at INFO).
3. `Backend Architect` — schema design (cross-context `UserId` as loose Guid per CODE.md), migration correctness, naming alignment with the rest of the codebase, lock pattern soundness.

No FE files change — `frontend/visual-design-language.md` / `frontend/text-copy-design-language.md` consultation **not required** for this slice.

---

## Risks + mitigations

1. **`OpenAI` SDK 2.10.0 model behavior with `gpt-5-mini`.** Possible: SDK rejects unknown model, or the chat-completions endpoint returns 404 for the model. Mitigation: keep `gpt-4o-mini` pricing entry in config so a hot-fix is one config-line swap if `gpt-5-mini` doesn't resolve. Tests use the fake client so they don't depend on the live model.
2. **Reasoning-model parameter incompatibilities.** `gpt-5` family is rumored to reject `temperature` outside `1.0`. Current `CompleteFreeTextAsync` / new `CompleteChatAsync` don't set `Temperature`, so we are fine. `CompleteStructuredAsync` is untouched.
3. **20-turn message-cap test runtime.** Hitting it for real means 20 LLM-fake-roundtrips. Acceptable; runs in seconds with the fake. If slow, parameterize cap via `IOptions<ConversationOptions>` and override to 3 in the test fixture.
4. **`Xmin` rowversion mapping.** `Xmin` shadow-prop convention varies by Plans precedent — I will verify the exact pattern (likely `.HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()` or `.IsRowVersion()` depending on what Plans does) when implementing the configuration file. If Plans does not have a precedent for the entity I'm adding, default to `.IsRowVersion()` per EF Core 10 Npgsql conventions.
5. **Stale-lock cleanup timing.** A user retry within 60s of an LLM hang gets 409. Acceptable for v1; documented behavior. Future: separate "session timed-out, retry" path.
6. **Title generation w/ a 1000-char message starting with non-Latin script.** Word-boundary truncation works on `char` index, not grapheme cluster — emoji and CJK split risk. Acceptable for v1 (no i18n in scope, per PROJECT.md known limitations).

---

## Out of scope (deliberate)

- Pagination on `GET /conversations` (per user — small N).
- PATCH for renaming a conversation's title.
- DELETE for a conversation (FE can simply stop polling; data retention policy is its own ticket).
- Streaming responses (SSE).
- Per-user budget enforcement on `/conversations` (no `chat_budgets` integration yet — that lives in the next AiChat slice).
- Removing/changing the existing `gpt-4o-mini` pricing entry.
- AutoMapper / MediatR (forbidden by CODE.md).
