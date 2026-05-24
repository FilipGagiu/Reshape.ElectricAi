# CODE.md — Code Rulebook

> **This file is the authoritative rulebook for all code in `Reshape.ElectricAi.*`. Claude re-reads it before every code edit (CLAUDE.md Phase 7). The `code-edit-guard.sh` hook enforces the re-read. Every review-agent dispatch MUST include "verify CODE.md compliance against the changed files" as an explicit directive.**

If a rule below conflicts with what feels convenient at the keyboard, the rule wins. If a rule turns out to be wrong, fix the rule here in a dedicated commit, then change the code — never the other way around.

---

## Stack (pinned)

| Concern | Choice |
|---|---|
| Runtime | .NET 10 GA (SDK 10.0.300+) |
| Language | C# 13, `<LangVersion>latest</LangVersion>` |
| Web framework | ASP.NET Core 10, **Controllers only** — no Minimal APIs, no MVC views, no Razor |
| ORM | EF Core 10 (`10.0.8` pinned) + `Npgsql.EntityFrameworkCore.PostgreSQL` (`10.0.*`) + `Pgvector.EntityFrameworkCore` `0.3.0` (NOT `.PostgreSQL` suffix — that package id does not exist on nuget.org) |
| Database | PostgreSQL 16+ with `pgvector` extension enabled |
| LLM | OpenAI .NET SDK (`OpenAI` official package, 2.10.0) |
| Embeddings | OpenAI `text-embedding-3-large` with `EmbeddingDimensions=1536` (truncated via API `dimensions` param) — hnsw limit is 2000 dims, do not exceed; **do not change model or dims without a migration plan** |
| Default chat model | `gpt-4o-mini` (overridable via `Chat:DefaultModel`) |
| Password hashing | `BCrypt.Net-Next` `4.2.0` |
| JWT | `Microsoft.AspNetCore.Authentication.JwtBearer` (`10.0.*`) + `System.IdentityModel.Tokens.Jwt` `8.18.0` + `Microsoft.IdentityModel.Tokens` `8.18.0` |
| Validation | `FluentValidation` `12.1.1` only — v12 removed the MVC auto-validation package (`FluentValidation.AspNetCore` and `FluentValidation.DependencyInjectionExtensions.AddFluentValidationAutoValidation()` are gone). Use the hand-rolled `Presentation/Filters/FluentValidationFilter.cs` (`IAsyncActionFilter`) instead. `FluentValidation.DependencyInjectionExtensions` `12.1.1` is on Presentation only (for any future use); Plans registers its validators via a reflection scan in `PlansModule.RegisterValidators` to avoid adding the package there. |
| Logging | `Serilog.AspNetCore` `10.0.0` + `Serilog.Sinks.Console` `6.1.1` (+ `Seq` later if useful) |
| OpenAPI | `Swashbuckle.AspNetCore` `10.1.7` + `Scalar.AspNetCore` `2.14.14` UI |
| Tokenizer | `Microsoft.ML.Tokenizers` `2.0.0` + `Microsoft.ML.Tokenizers.Data.Cl100kBase` `2.0.0` (cl100k_base data package required at runtime) |
| Tests | `xUnit` + `FluentAssertions` + `Testcontainers.PostgreSql` (added when first test project is scaffolded) |
| Solution file | `ElectricCastle.slnx` — XML solution format (new .NET 10 default from `dotnet new sln`, replaces classic `.sln`) |
| NuGet feed | project-local `nuget.config` clears global sources + adds nuget.org only (works around the user's authenticated `devops.iceportal.com` global feed that returns 401 here) |

NuGet additions are governed by **CLAUDE.md §6a** — devs install packages themselves. Claude lists what is needed and waits.

---

## Project structure

```
src/
├── Reshape.ElectricAi.Presentation/   API host — controllers, Program.cs, middleware, appsettings
├── Reshape.ElectricAi.Core/           Shared entities, DTOs, interfaces, enums, exceptions
├── Reshape.ElectricAi.Plans/          Users + Auth + Preferences + Plan generation
├── Reshape.ElectricAi.VectorDb/       pgvector access, ingest, retrieval
├── Reshape.ElectricAi.LiveFeed/       Organizer feed + SSE channel state
└── Reshape.ElectricAi.AiChat/         Chat orchestration, RAG, budget enforcement
tests/
└── Reshape.ElectricAi.<Lib>.Tests/    one per feature lib
```

**Dependency direction (acyclic, enforced by reviewers):**
```
Presentation     →  Plans, VectorDb, LiveFeed, AiChat, Core, Infrastructure
Plans            →  Core, Infrastructure
AiChat           →  Core, VectorDb
LiveFeed         →  Core, Infrastructure, VectorDb
VectorDb         →  Core
Infrastructure   →  Core
Core             →  (nothing except Microsoft.Extensions.* abstractions)
```

**Hard rules:**

1. **Controllers live ONLY in `Reshape.ElectricAi.Presentation`.** Feature libs MUST NOT reference `Microsoft.AspNetCore.Mvc.*`. The only ASP.NET Core packages a feature lib may take are `Microsoft.AspNetCore.Authentication.JwtBearer` (Plans, for issuance helpers) and `Microsoft.Extensions.*` abstractions.
2. **Core MUST NOT reference any feature lib.** No exceptions. Shared types live here; feature libs depend on Core, not the reverse.
3. **Each lib exposes exactly one DI entry-point:** `public static class XxxModule { public static IServiceCollection AddXxxModule(this IServiceCollection services, IConfiguration configuration); }`. Presentation's `Program.cs` calls these and nothing else from feature libs.
4. **Namespace = folder path under `src/<Project>/`.** File-scoped namespaces only. One public type per file.
5. **No circular project references — ever.** A reviewer who sees one rejects the PR.

---

## csproj defaults (every project)

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

Test projects additionally set `<IsPackable>false</IsPackable>`.

---

## Controllers

- Route: `[Route("api/v1/[controller]")]`. All endpoints under `/api/v1`.
- One controller per resource (`AuthController`, `PlansController`, `PreferencesController`, `GroupsController`, `ChatController`, `FeedController`, `HealthController`).
- **Thin only:** bind input → call service → return DTO. No business logic, no LINQ-to-EF, no OpenAI calls, no `DbContext` injection.
- Action signatures: `public async Task<ActionResult<TDto>> XxxAsync([FromBody] TRequest request, CancellationToken cancellationToken)`. CancellationToken is **mandatory** on every action.
- HTTP verbs match REST semantics. `POST` for create + non-idempotent commands, `PUT` for full replace, `PATCH` for partial update, `DELETE` for remove, `GET` for query.
- Status codes: `200` OK, `201` Created (with `Location` header), `204` No Content, `400` validation, `401` no/invalid auth, `403` wrong role, `404` not found, `409` conflict, `422` semantic failure (e.g. prefs too low), `402` over budget, `500` only via global exception middleware.
- Return DTOs from `Core` (cross-lib) or the feature lib (lib-private). **Never** return EF entities.

## Services

- Interface in `Reshape.ElectricAi.Core/Services/IXxxService.cs`.
- Implementation in the feature lib, `Services/XxxService.cs`.
- Constructor injection only. No service locator. No static state.
- All public async methods accept `CancellationToken`.
- Method naming: `VerbNounAsync`, return `Task<T>` or `Task`.
- Services that fail with domain errors throw `DomainException` (or a subtype) defined in Core. Don't return error tuples.

## DbContext + migrations

- **One `DbContext` per lib**, owned by that lib's dev.
- One Postgres schema per lib: `plans`, `vector`, `feed`, `chat`. Set via `modelBuilder.HasDefaultSchema("plans")` etc.
- Migrations history table per schema:
  ```csharp
  options.UseNpgsql(connectionString, npgsql =>
  {
      npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plans");
      npgsql.UseVector(); // VectorDb context only
  });
  ```
- **No cross-context navigation properties.** Cross-lib references are loose `Guid` IDs only (e.g. `chat.chat_sessions.UserId` is a `Guid`, not a navigation to `plans."Users"`).
- Migrations live in the lib that owns the context: `src/Reshape.ElectricAi.<Lib>/Migrations/`.
- `Program.cs` calls `await db.Database.MigrateAsync(cancellationToken)` per context on startup **only when** `ASPNETCORE_ENVIRONMENT=Development`. Prod migrations are explicit (`dotnet ef database update`).

## Persistence layer (Repository + Specification)

- Generic abstractions live in `Reshape.ElectricAi.Core/Persistence/`:
  - `IRepository<T>` — `GetByIdAsync` / `FirstOrDefaultAsync(spec)` / `ListAsync(spec)` / `CountAsync(spec)` / `AnyAsync(spec)` / `AddAsync` / `Update` / `Remove` / `SaveChangesAsync`. Generic over `T : class`.
  - `ISpecification<T>` + `Specification<T>` base — encodes Where / Includes / OrderBy / Take/Skip / AsNoTracking / AsSplitQuery. Concrete specs derive from `Specification<T>` and use fluent protected methods (`Where`, `AddInclude`, `ApplyOrderBy`, `EnableNoTracking`, etc.) in their constructor. Core stays EF-free.
- EF-aware impl lives in each feature lib that needs persistence:
  - `EfRepository<TContext, T> : IRepository<T>` — generic over the lib's `DbContext`. Constructor: `(TContext context)`. Two type params so DI cannot register it directly via open generics.
  - **Closing class per lib** (REQUIRED for DI): `public sealed class PlansRepository<T> : EfRepository<PlansDbContext, T>` — collapses to one type param so `services.AddScoped(typeof(IRepository<>), typeof(PlansRepository<>))` works. When LiveFeed/AiChat add EF persistence, each adds its own `XxxRepository<T>` and `AddScoped` line.
  - `SpecificationEvaluator.Apply<T>(IQueryable<T>, ISpecification<T>)` — static, applies the spec to a queryable. Order: AsNoTracking → AsSplitQuery → Where → Includes → IncludeStrings → OrderBy/OrderByDescending → Skip → Take.
- Specs live in `Plans/Persistence/Specifications/<Name>Spec.cs` (`UserByEmailSpec`, `ActiveRefreshTokenByHashSpec`, etc.). One spec per file, `sealed class`, name ends in `Spec`.
- Atomic multi-row operations (e.g. refresh-token rotation) MUST bypass the generic repo. Use `DbContext.<DbSet>.Where(...).ExecuteUpdateAsync(...)` in a dedicated service (e.g. `Plans/Services/RefreshTokenStore.cs` behind `IRefreshTokenStore`) — keeps `IRepository<T>` clean of one-off methods.
- **The EF base lives in `Reshape.ElectricAi.Infrastructure`** (referenced by every feature lib that needs EF persistence). It holds `EfRepository<TContext, T>` + `SpecificationEvaluator`. Each consuming lib provides its own closing class (`PlansRepository<T>`, `FeedRepository<T>`, etc.). Plans + LiveFeed currently consume; AiChat + VectorDb will follow the same pattern when they add EF persistence.
- **DI registration must avoid open-generic shadowing.** When multiple libs register `IRepository<>` open-generic at the same DI key, the last registration wins and replaces the earlier ones — `IRepository<User>` then resolves to a closing class wired to the wrong `DbContext`, throwing `Cannot create a DbSet for 'User' because this type is not included in the model for the context` at runtime. **Rule:** the FIRST lib in the registration order MAY use open-generic (`services.AddScoped(typeof(IRepository<>), typeof(XxxRepository<>))`); every subsequent lib MUST close per-entity for the types it owns (`services.AddScoped<IRepository<FeedEntry>, FeedRepository<FeedEntry>>()` etc.). MS.DI prefers the closed-specific registration over open-generic when both match, so per-entity closes from later libs win for their own types without disturbing the earlier open-generic for the first lib's types. Today: Plans uses open-generic; LiveFeed closes per-entity for `FeedEntry`, `FeedEntryArtist`, `FeedEntryGenre`. AiChat / VectorDb must also close per-entity.

## DTOs vs entities

- Entities (EF Core types) never cross the controller boundary.
- DTOs are immutable `record`s.
- Manual mapping (extension methods like `ToDto()` / `ToEntity()`) — **no AutoMapper** until the mapping count justifies it.
- Request DTOs (`XxxRequest`) and response DTOs (`XxxResponse` or `XxxDto`) are separate types when their shape differs.

## Validation

- `FluentValidation` validators live next to their DTOs in the feature lib (`Plans/Validators/PreferencesUpdateValidator.cs`).
- Auto-registered by the module via `services.AddValidatorsFromAssemblyContaining<PlansModule>()`.
- `services.AddFluentValidationAutoValidation()` in Presentation registers the MVC integration.
- Validation failure → 400 with the standard error envelope.

## Error envelope

Standard JSON shape for every non-2xx response:

```json
{ "error": { "code": "kebab-case-code", "message": "human-readable", "details": {} } }
```

- `code` is stable and machine-readable (used by FE for branching).
- `message` is human-readable English (i18n not in scope v1).
- `details` is optional and may contain field-level validation errors.

Domain exceptions in Core inherit `DomainException` and carry a `Code`. The global `ExceptionHandlerMiddleware` maps:
- `ValidationException` → 400
- `UnauthorizedException` → 401
- `ForbiddenException` → 403
- `NotFoundException` → 404
- `ConflictException` → 409
- `PreconditionFailedException` → 422
- `BudgetExceededException` → 402
- everything else → 500 with `code: "internal-error"` (the real message logged, not exposed). The exposed `message` is the project's standard "rain vibe" string defined as `ExceptionHandlerMiddleware.GenericInternalMessage` — do NOT inline a different generic string elsewhere.

**Model binding / JSON deserialization failures** (raised before any controller runs) → 400 with `code: "invalid-request"` and `message: "Invalid request body."` Configured via `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Program.cs`. The raw `ModelState` errors (which contain JSON paths, internal type names, and byte offsets) MUST be logged server-side at Warning and MUST NOT appear in the response body — they are an information disclosure risk. `details` is intentionally omitted for this code.

## Auth

- JWT (HS256). Signing key from `IConfiguration["Auth:JwtSigningKey"]`, **MUST be ≥ 32 bytes**. User-secrets in dev, environment variable in prod. **NEVER hardcode.** Decoded via `Reshape.ElectricAi.Core.Configuration.JwtSigningKey.Decode(string)` (base64 first, UTF-8 fallback, ≥ 32-byte gate). `JwtSigningKey.LooksLikeBase64ButTooShort(string)` companion rejects strings that look like base64 but decode to fewer than 32 bytes (catches accidental short keys).
- Access token lifetime: `Auth:AccessTokenMinutes` (default 15).
- Refresh token lifetime: `Auth:RefreshTokenDays` (default 7), stored as **SHA-256 hash** in `plans."RefreshTokens"`. Rotated on refresh, old token revoked. Rotation is atomic via `ExecuteUpdateAsync(Where(hash=X, RevokedUtc==null, ExpiresUtc>now)).SetProperty(RevokedUtc=now, ReplacedByHash=new)` in `Plans/Services/RefreshTokenStore.cs` — 0 rows updated → `UnauthorizedException("invalid-refresh-token")`. Postgres row-level lock serializes concurrent callers.
- Password hashing: `BCrypt.HashPassword(password + base64(salt), workFactor: 12)`. Per-user salt is 16 random bytes from `RandomNumberGenerator.GetBytes(16)`, stored as `byte[]` alongside the hash.
- Password policy: ≥ 8 characters, ≥ 1 digit, ≥ 1 symbol (enforced by FluentValidation).
- Login MUST always run BCrypt verify even when the user is not found (constant-time, prevents user enumeration). `IPasswordHasher.VerifyDummy()` is part of the interface contract; runs BCrypt against a precomputed dummy hash. `PasswordHasher` is registered as **Singleton** (no mutable state, no DI deps) so the dummy hash is computed once per process.
- JWT claims: `sub` (UserId), `email`, `role`, `jti` (random Guid per token, RFC 7519 §4.1.7 — required for uniqueness when tokens issue in the same epoch second), `iat`, `exp`, `iss=reshape-electric-ai`, `aud=reshape-electric-ai-api`.
- `TokenValidationParameters` MUST pin `ValidAlgorithms = [SecurityAlgorithms.HmacSha256]` (defense in depth — prevents algorithm-confusion attacks if the signing key type ever changes). `MapInboundClaims = false` to drop legacy SAML auto-mapping (`sub`→`ClaimTypes.NameIdentifier`). Controllers read `JwtRegisteredClaimNames.Sub` directly with `ClaimTypes.NameIdentifier` as a unit-test fallback.
- `JwtBearer` 401/403 responses MUST flow through the standard error envelope. Wire `JwtBearerEvents.OnChallenge` (write 401 + envelope, code derived from `context.AuthenticateFailure` type: `SecurityTokenExpiredException → token-expired`, invalid issuer/audience/signature → `invalid-token`, null failure → `missing-token`) and `OnForbidden` (write 403 + `code: forbidden`). Without this, the bearer middleware emits empty 401 bodies that bypass `ExceptionHandlerMiddleware`.
- Authorize attributes: `[Authorize]` (any valid token), `[Authorize(Roles = "Organizer")]` (feed publish/edit/delete). Anonymous endpoints use `[AllowAnonymous]` — written out, not aliased.
- **SSE query-string token (`?access_token=`) is accepted ONLY on `GET /api/v1/feed/stream`.** A middleware extracts it and rewrites the `Authorization` header before the JWT middleware runs. The middleware MUST reject query-string tokens on every other route.

## OpenAI + cost discipline

- All OpenAI calls go through `IOpenAiClient` in `Core`, implemented in `AiChat`.
- The wrapper enforces:
  - max prompt tokens (`OpenAi:Limits:MaxPromptTokens`)
  - max completion tokens (`OpenAi:Limits:MaxCompletionTokens`)
  - timeout (`OpenAi:Limits:TimeoutSeconds`, default 30)
  - 2-attempt retry with exponential backoff (1s, 2s) on transient HTTP errors
  - typed `LlmException` on terminal failure
- Model identifiers and **per-1K token prices in cents** live in `appsettings.json` under `OpenAi:Models:<modelName>` (`PromptCentsPer1K`, `CompletionCentsPer1K`). **NEVER hardcode prices** — they shift, and tier budgets depend on accuracy.
- Every call returns `LlmUsage { PromptTokens, CompletionTokens, CostCents }`. The caller MUST persist this alongside the message it produced.
- Chat endpoints MUST check `chat.chat_budgets` BEFORE issuing the call and decrement AFTER. Over budget → 402 with `code: "chat-budget-exceeded"`.
- Embedding calls also flow through the same wrapper (same retry + logging) but bypass the chat budget check — embeddings come out of an ingest-side budget tracked separately.

## Vector DB

- Embedding model fixed to `text-embedding-3-small` (1536 dims). Changing the model is a **migration event**: re-embed every chunk + drop and rebuild the HNSW index. Do not change casually.
- Chunker: token-aware via `Microsoft.ML.Tokenizers` cl100k_base. Target 400 tokens per chunk, 50-token overlap.
- Ingest is **idempotent** via `vector.documents.ContentHash` (SHA-256 of normalized content). If hash unchanged → skip re-embedding for that document.
- Retrieval: cosine similarity (`<=>` operator) on the HNSW index. Default top-K = 6. Caller passes a per-domain filter record with optional `CategoryValues` for GIN-based pre-filtering.
- `IVectorSearchService` exposes three domain-specific methods:
  - `SearchDocumentsAsync` → `RetrievedChunk { DocumentId, DocumentTitle, ChunkIndex, Content, Score }`
  - `SearchQuestionsAsync` → `RetrievedQA { QuestionText, Answers: RetrievedAnswer[], QuestionScore }`
  - `SearchEventsAsync` → `RetrievedEvent { FeedEntryId, Title, TextRepresentation, EventUtc, Score }`
- Score is `1 - cosine_distance` (higher = more similar).

## SSE (LiveFeed)

- Single endpoint: `GET /api/v1/feed/stream`. **No other SSE endpoints.**
- Response headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache, no-transform`, `Connection: keep-alive`, `X-Accel-Buffering: no`.
- Heartbeat comment `: keepalive\n\n` every **25 seconds** (under typical proxy idle timeouts).
- Per-user bounded `Channel<FeedEvent>` (capacity 100, mode `BoundedChannelFullMode.DropOldest`).
- On connect, replay the last 10 entries the user hasn't seen (`Last-Event-Id` header drives the catch-up cursor).
- Event format: `event: feed.created|feed.updated|feed.deleted\nid: <eventId>\ndata: <json>\n\n`.
- Disconnect (CancellationToken triggered) MUST unregister the channel from the broadcaster.

## Logging

- Serilog structured logging, console sink minimum, configured in `Program.cs` via `UseSerilog`.
- **Never log** secrets, JWT bodies, passwords, embeddings, or full prompt/completion strings at INFO. DEBUG is permitted behind `Logging:LogLlmPayloads=true`.
- Every request gets a request-id (middleware) — propagated via `Serilog.Context.LogContext.PushProperty("RequestId", id)`.
- Errors logged with full exception. Domain exceptions logged at Warning; unexpected exceptions at Error with stack.

## Async + cancellation

- Every public async method accepts `CancellationToken`.
- No `.Result`, no `.Wait()`, no `async void` outside event handlers.
- Long-running background work uses `IHostedService` or `BackgroundService`, not fire-and-forget `Task.Run`.

## Tests

- One xUnit test project per feature lib, naming `Reshape.ElectricAi.<Lib>.Tests`.
- Folder layout mirrors the production project.
- Use `FluentAssertions` for readable assertions.
- **Integration tests use `Testcontainers.PostgreSql`** — spin up a real Postgres with pgvector for any test that touches EF Core. Do not mock `DbContext`.
- Test class per type-under-test (`PlansServiceTests`), method-per-scenario named `Method_State_ExpectedOutcome`.
- Every controller action SHOULD have an integration test that hits the route through `WebApplicationFactory<Program>`.
- No coverage gate yet — quality of tests over count.

## Naming, style, file conventions

- `.editorconfig` is authoritative. Key non-default rules:
  - File-scoped namespaces (`namespace Foo;`).
  - `var` always for local variables. Even when the right-hand side type is non-obvious. Explicit types only when `var` is illegal (e.g. uninitialized locals, target-typed `new()` ambiguity, lambda parameter types). No exceptions for "readability" — consistency wins.
  - Private fields `_camelCase`. Locals + parameters `camelCase`. Types + methods + properties + constants `PascalCase`.
  - Braces on new lines (Allman) for types/methods; same-line OK for short property accessors.
  - One public type per file. File name == type name.
- Records preferred over classes for DTOs and value objects. Classes for services + entities.
- No `#region` blocks.

## Forbidden constructs

- Minimal APIs (`MapGet`, `MapPost`, `WebApplication.MapXxx`) — Controllers only.
- MVC views, Razor pages, static files served by the API.
- Legacy `Startup.ConfigureServices` / `Configure` split — use top-level `Program.cs`.
- `BinaryFormatter`, `JsonSerializer` from `Newtonsoft.Json` (use `System.Text.Json`).
- `AutoMapper` — manual mapping until justified, then revisit this rule.
- `MediatR` — Approach A (service-per-lib) is locked.
- `Task.Run` for fire-and-forget side effects in request handlers.
- Catching `Exception` without rethrow or specific handling.

## Permission boundary (CLAUDE.md §6a)

- **Packages added ONLY by humans.** Claude lists package + version + target project + reason and waits for a dev to run `dotnet add package`. The `bash-guard.sh` hook blocks `dotnet add package` from running automatically.
- **Namespace / using lookups** for external assemblies (when Grep returns zero hits in source): ASK in chat, do not spelunk `~/.nuget/packages/`.

## Re-read protocol

- Claude re-reads CODE.md before every code edit (CLAUDE.md Phase 7). The `code-edit-guard.sh` hook injects a reminder for `.cs / .csproj / .sln / .ts / .tsx / .js / .jsx / .py / .go / .rs / .java / .kt`. **Do not suppress this hook.**
- Review-agent dispatches MUST include "verify CODE.md compliance against the changed files" as an explicit directive (CLAUDE.md §2).

## When a rule blocks you

- A rule that obstructs real work is a bug in the rule. Fix CODE.md in a dedicated commit (with the reasoning in the commit message), then change the code.
- New rules graduate via `/si:remember` → direct edit here, per CLAUDE.md §3a.
