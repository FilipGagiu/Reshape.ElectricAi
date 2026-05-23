# Plan — Auth slice + Generic Repository

> **CLAUDE.md non-negotiable phase list — restated verbatim:**
>
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

---

## Phase 1 application note

- `superpowers:brainstorming` invoked — design refined across 5 user-approved sections (architecture, repo+spec interfaces, auth flows, presentation+config, testing).
- `superpowers:test-driven-development` applies during execution — unit tests for `PasswordHasher` / `TokenService` / validators, integration tests for endpoints + repository, both written alongside their production code.
- `superpowers:verification-before-completion` applies at Phase 8 — build + tests + manual `Scalar` round-trip before claiming done.

---

## Scope (locked)

### IN

- Generic repository abstraction (`IRepository<T>` + `ISpecification<T>` + base `Specification<T>` in `Core`).
- EF Core impl in Plans (`EfRepository<TContext,T>` + sealed `PlansRepository<T>` + `SpecificationEvaluator`).
- Two specs for auth (`UserByEmailSpec`, `ActiveRefreshTokenByHashSpec`).
- Domain exception base + 5 subtypes in Core (`DomainException`, `ConflictException`, `UnauthorizedException`, `NotFoundException`, `ForbiddenException`, `PreconditionFailedException`).
- Auth DTOs (`RegisterRequest`, `LoginRequest`, `RefreshRequest`, `AuthResponse`, `UserDto`, `TokenSubject`) + service interfaces (`IAuthService`, `ITokenService`, `IPasswordHasher`).
- `AuthOptions` + `AuthOptionsValidator` in Core (strongly typed config + fail-fast at startup).
- Service impls in Plans: `PasswordHasher` (BCrypt wf=12 + 16-byte salt), `TokenService` (JWT issuance + refresh token generator/hasher), `AuthService` (orchestrates all four flows).
- FluentValidation validators (`RegisterRequestValidator`, `LoginRequestValidator`, `RefreshRequestValidator`).
- `PlansModule.AddPlansModule(IServiceCollection, IConfiguration)` DI entry-point.
- Presentation wiring: `Program.cs` full replacement, `AuthController` (4 actions), `ExceptionHandlerMiddleware`, JWT bearer, Serilog, Scalar, FluentValidation auto-validation, dev-only auto-migrate.
- `appsettings.json` (Auth + Serilog + Cors + connection-string sections), user-secrets bootstrap doc snippet.
- Test project `tests/Reshape.ElectricAi.Plans.Tests` (xUnit + FluentAssertions + Testcontainers.PostgreSql + Mvc.Testing).
- Unit tests (hasher, token, validators) + integration tests (full HTTP for each endpoint, repo + spec round-trips).

### OUT (deferred)

- `Auth:SingleSession` runtime behavior — config key present, default `false`, no revoke-prior branch.
- Rate limiting on `/auth/login` — deferred to later middleware pass.
- Email verification, password reset — explicit v1 limitation in PROJECT.md.
- Promotion of `EfRepository` / `SpecificationEvaluator` to a shared `Infrastructure` project — left in Plans for now; team revisits when 2nd lib needs EF.
- Preferences / Groups / Plans endpoints — separate future slices.

---

## Architecture summary (approved Section 1)

```
Presentation (controllers)
        │  /api/v1/auth/{register|login|refresh|me}
        ▼
Plans (services)                  Core (abstractions)
  AuthService           ───uses──▶  IRepository<T>, ISpecification<T>
  TokenService                      IAuthService, ITokenService, IPasswordHasher
  PasswordHasher                    DomainException + subtypes
        │                           DTOs: Register/Login/Refresh/Auth/User/TokenSubject
        ▼                           AuthOptions + AuthOptionsValidator
EfRepository<TContext,T>
PlansRepository<T> : EfRepository<PlansDbContext,T>
SpecificationEvaluator
        │
        ▼
PlansDbContext  ──▶  plans schema (Users, RefreshTokens, …)
```

**Project placement** (no new csproj added):

| Core | Plans | Presentation |
|---|---|---|
| `Persistence/IRepository.cs` | `Persistence/EfRepository.cs` | `Controllers/AuthController.cs` |
| `Persistence/ISpecification.cs` | `Persistence/SpecificationEvaluator.cs` | `Middleware/ExceptionHandlerMiddleware.cs` |
| `Persistence/Specification.cs` | `Persistence/PlansRepository.cs` | `Program.cs` (full replace) |
| `Domain/Exceptions/DomainException.cs` + 5 subtypes | `Persistence/Specifications/UserByEmailSpec.cs` | `appsettings.json` (full replace) |
| `Configuration/AuthOptions.cs` | `Persistence/Specifications/ActiveRefreshTokenByHashSpec.cs` | `appsettings.Development.json` |
| `Configuration/AuthOptionsValidator.cs` | `Services/PasswordHasher.cs` | |
| `Services/IAuthService.cs` | `Services/TokenService.cs` | |
| `Services/ITokenService.cs` | `Services/AuthService.cs` | |
| `Services/IPasswordHasher.cs` | `Validators/RegisterRequestValidator.cs` | |
| `Dtos/Auth/RegisterRequest.cs` | `Validators/LoginRequestValidator.cs` | |
| `Dtos/Auth/LoginRequest.cs` | `Validators/RefreshRequestValidator.cs` | |
| `Dtos/Auth/RefreshRequest.cs` | `PlansModule.cs` | |
| `Dtos/Auth/AuthResponse.cs` | `Extensions/UserMappingExtensions.cs` | |
| `Dtos/Auth/UserDto.cs` | | |
| `Dtos/Auth/TokenSubject.cs` | | |

---

## Implementation order (file-by-file)

Execution proceeds top-down; each step's file content is detailed in the conversation history (Sections 1–5). Re-read [CODE.md](CODE.md) before every code edit (CLAUDE.md Phase 7 + the `code-edit-guard.sh` hook).

### Step 1 — Core abstractions (foundation, no dependencies)

1. `src/Reshape.ElectricAi.Core/Persistence/ISpecification.cs`
2. `src/Reshape.ElectricAi.Core/Persistence/Specification.cs`
3. `src/Reshape.ElectricAi.Core/Persistence/IRepository.cs`
4. `src/Reshape.ElectricAi.Core/Domain/Exceptions/DomainException.cs`
5. `src/Reshape.ElectricAi.Core/Domain/Exceptions/ConflictException.cs`
6. `src/Reshape.ElectricAi.Core/Domain/Exceptions/UnauthorizedException.cs`
7. `src/Reshape.ElectricAi.Core/Domain/Exceptions/NotFoundException.cs`
8. `src/Reshape.ElectricAi.Core/Domain/Exceptions/ForbiddenException.cs`
9. `src/Reshape.ElectricAi.Core/Domain/Exceptions/PreconditionFailedException.cs`
10. `src/Reshape.ElectricAi.Core/Configuration/AuthOptions.cs`
11. `src/Reshape.ElectricAi.Core/Configuration/AuthOptionsValidator.cs`
12. `src/Reshape.ElectricAi.Core/Dtos/Auth/UserDto.cs`
13. `src/Reshape.ElectricAi.Core/Dtos/Auth/TokenSubject.cs`
14. `src/Reshape.ElectricAi.Core/Dtos/Auth/AuthResponse.cs`
15. `src/Reshape.ElectricAi.Core/Dtos/Auth/RegisterRequest.cs`
16. `src/Reshape.ElectricAi.Core/Dtos/Auth/LoginRequest.cs`
17. `src/Reshape.ElectricAi.Core/Dtos/Auth/RefreshRequest.cs`
18. `src/Reshape.ElectricAi.Core/Services/IAuthService.cs`
19. `src/Reshape.ElectricAi.Core/Services/ITokenService.cs`
20. `src/Reshape.ElectricAi.Core/Services/IPasswordHasher.cs`
21. Add `Microsoft.Extensions.Options` + `Microsoft.IdentityModel.Tokens` (for `SecurityKey` type — referenced indirectly via `TokenSubject`? actually no; Core stays clean) — verify with build. If `AuthOptionsValidator` needs `IValidateOptions<T>`, add `Microsoft.Extensions.Options` to `Core.csproj`.

**Package addition required (Core.csproj):**

- `Microsoft.Extensions.Options` — minimal, abstractions only. Required for `IValidateOptions<T>` interface. **User installs per CLAUDE.md §6a; Claude lists and waits.**

### Step 2 — Plans persistence + specs

22. `src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs`
23. `src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs`
24. `src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs`
25. `src/Reshape.ElectricAi.Plans/Persistence/Specifications/UserByEmailSpec.cs`
26. `src/Reshape.ElectricAi.Plans/Persistence/Specifications/ActiveRefreshTokenByHashSpec.cs`

### Step 3 — Plans services + validators + DI

27. `src/Reshape.ElectricAi.Plans/Services/PasswordHasher.cs`
28. `src/Reshape.ElectricAi.Plans/Services/TokenService.cs`
29. `src/Reshape.ElectricAi.Plans/Services/AuthService.cs`
30. `src/Reshape.ElectricAi.Plans/Extensions/UserMappingExtensions.cs` (`User → UserDto`)
31. `src/Reshape.ElectricAi.Plans/Validators/RegisterRequestValidator.cs`
32. `src/Reshape.ElectricAi.Plans/Validators/LoginRequestValidator.cs`
33. `src/Reshape.ElectricAi.Plans/Validators/RefreshRequestValidator.cs`
34. `src/Reshape.ElectricAi.Plans/PlansModule.cs`

### Step 4 — Presentation wiring

35. `src/Reshape.ElectricAi.Presentation/Middleware/ExceptionHandlerMiddleware.cs`
36. `src/Reshape.ElectricAi.Presentation/Controllers/AuthController.cs`
37. `src/Reshape.ElectricAi.Presentation/appsettings.json` (replace)
38. `src/Reshape.ElectricAi.Presentation/appsettings.Development.json` (new — minimal overrides)
39. `src/Reshape.ElectricAi.Presentation/Program.cs` (replace)

### Step 5 — Test project

40. `tests/Reshape.ElectricAi.Plans.Tests/Reshape.ElectricAi.Plans.Tests.csproj` (new)
41. Add the test project to `ElectricCastle.slnx` (`dotnet sln add tests/Reshape.ElectricAi.Plans.Tests`).
42. `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/PostgresFixture.cs`
43. `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/PostgresCollection.cs`
44. `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/AuthApiFactory.cs`
45. `tests/Reshape.ElectricAi.Plans.Tests/Unit/Services/PasswordHasherTests.cs`
46. `tests/Reshape.ElectricAi.Plans.Tests/Unit/Services/TokenServiceTests.cs`
47. `tests/Reshape.ElectricAi.Plans.Tests/Unit/Validators/RegisterRequestValidatorTests.cs`
48. `tests/Reshape.ElectricAi.Plans.Tests/Integration/Persistence/EfRepositoryTests.cs`
49. `tests/Reshape.ElectricAi.Plans.Tests/Integration/Endpoints/AuthControllerTests.cs`

**Packages needed for the test csproj (user installs):**

- `Microsoft.NET.Test.Sdk` 17.x
- `xunit` 2.9.x
- `xunit.runner.visualstudio` 2.8.x
- `FluentAssertions` 6.12.x (note: 7.x+ is paid)
- `Testcontainers.PostgreSql` 4.x
- `Microsoft.AspNetCore.Mvc.Testing` (net10 compatible)
- Project references: `Reshape.ElectricAi.Plans`, `Reshape.ElectricAi.Core`, `Reshape.ElectricAi.Presentation`

---

## Phase 4 — Review agent dispatches

After Step 3 (services done) AND after Step 4 (presentation done), dispatch a `Code Reviewer` agent with this directive:

> "Review the diff in `src/Reshape.ElectricAi.Core/**` and `src/Reshape.ElectricAi.Plans/**` (or `src/Reshape.ElectricAi.Presentation/**`). **Verify CODE.md compliance against the changed files** (Stack pinning, Controllers in Presentation only, DTOs as records, file-scoped namespaces, `var`-always, one public type per file, no `MediatR`/`AutoMapper`/Minimal-APIs, JWT claims shape, BCrypt wf=12 + 16-byte salt, constant-time login). Flag any leaks of EF Core into Core."

After Step 5 (tests done), dispatch a `Security Engineer` agent:

> "Security-review the auth implementation. **Verify CODE.md compliance**. Specifically audit: (a) constant-time login behavior on missing user / wrong password, (b) refresh token rotation + revocation correctness (no replay window), (c) refresh token at-rest storage as SHA-256 hash (never plain), (d) JWT signing key length validation, (e) password policy enforcement, (f) user-enumeration resistance on `/auth/register` error responses, (g) no secret leakage in logs."

---

## Phase 8 — Verification checklist

Run from repo root after Step 5:

1. `dotnet restore`
2. `dotnet build` — zero errors, zero new warnings (existing `MSB3277` from `EFCore.Design 10.0.8` drift documented in MEMORY.md is pre-existing and acceptable).
3. `dotnet test tests/Reshape.ElectricAi.Plans.Tests` — all pass. Testcontainers requires Docker running locally.
4. `dotnet run --project src/Reshape.ElectricAi.Presentation` — boot, then:
   - GET `http://localhost:5217/scalar/v1` — page loads, AuthController endpoints listed.
   - `POST /api/v1/auth/register` `{"email":"alice@example.com","password":"P@ssw0rdLong"}` → 200, body has `accessToken`, `refreshToken`, `expiresIn`, `user.id`/`email`/`role`.
   - `POST /api/v1/auth/login` same credentials → 200, fresh pair.
   - `POST /api/v1/auth/login` wrong password → 401 envelope `{"error":{"code":"invalid-credentials",...}}`.
   - `POST /api/v1/auth/register` same email → 409 envelope `{"error":{"code":"email-in-use",...}}`.
   - `POST /api/v1/auth/refresh` `{"refreshToken":"..."}` → 200, new pair; calling again with the OLD refresh token → 401 `{"error":{"code":"invalid-refresh-token",...}}`.
   - `GET /api/v1/auth/me` with `Authorization: Bearer <access>` → 200 `{id, email, role}`.
   - `GET /api/v1/auth/me` no header → 401.

If any check fails: STOP and re-plan (CLAUDE.md §1 mid-execution rule). No "trust me" claims (Phase 8).

---

## Phase 9 — Promote learnings

Anticipated memory captures (run `/si:remember` for each discovered fact):

- `EfRepository<TContext,T>` open-generic DI requires a single-param closing class (`PlansRepository<T>`).
- `Microsoft.Extensions.Options` is the only EF-free package Core needs for `IValidateOptions<T>`.
- BCrypt-Next 4.2.0 is the installed version (memory says 4.0.3 in CODE.md — likely a drift to capture or reconcile).
- `FluentValidation` is 12.1.1, not 11.11.0 as CODE.md says (similar drift to capture).
- JWT identity packages installed at 8.18.0 (CODE.md says 8.5.0).

If these drifts are intentional (newer versions), update CODE.md to reflect them. If unintentional, capture as a known drift and align in a future cleanup.

Direct-edits expected post-execution:
- `PROJECT.md` — note that `EfRepository` lives in Plans pending decision to extract to a shared `Infrastructure` project.
- `CODE.md` — reconcile pinned package versions vs what's actually in `Reshape.ElectricAi.Plans.csproj` (BCrypt, FluentValidation, JWT).

---

## Phase 10 — Delete this plan file

Last step. `git rm .claude/plans/auth-and-generic-repo.md`. Source of truth is the commit history.
