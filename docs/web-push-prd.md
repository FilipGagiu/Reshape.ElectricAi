# PRD — Web Push notifications (backend completion)

Self-contained handoff. Receiving Claude session has zero prior context. Read top to bottom before touching code.

---

## Goal

Real Web Push for hackathon PWA demo. Operator picks preset from hidden `/admin/notify` page in Angular frontend, BE fans out push to all subscribed devices via VAPID. Locked-screen notifications on iOS Safari PWAs (16.4+). Tap-to-open routes to preset `url`.

Frontend is separate work, not in scope here. BE must expose contract below — FE Claude session consumes it.

---

## Status — what's already done vs left

### Done (code committed in working tree, unbuilt)

| File | Purpose |
|---|---|
| `src/Reshape.ElectricAi.Core/Dtos/Notifications/SubscribeRequest.cs` | DTO: `{endpoint, p256dh, auth, userAgent?}` |
| `src/Reshape.ElectricAi.Core/Dtos/Notifications/UnsubscribeRequest.cs` | DTO: `{endpoint}` |
| `src/Reshape.ElectricAi.Core/Dtos/Notifications/SendRequest.cs` | DTO: `{title, body, icon?, badge?, url?}` |
| `src/Reshape.ElectricAi.Core/Dtos/Notifications/PushPayload.cs` | Internal payload type, JSON-serialized to push body |
| `src/Reshape.ElectricAi.Core/Dtos/Notifications/SendResult.cs` | Response: `{delivered, pruned, failed}` |
| `src/Reshape.ElectricAi.Core/Dtos/Notifications/VapidPublicKeyResponse.cs` | Response: `{publicKey}` |
| `src/Reshape.ElectricAi.Core/Configuration/PushOptions.cs` | Options binder for `Push:` config section |
| `src/Reshape.ElectricAi.Core/Services/IPushService.cs` | Service contract |
| `src/Reshape.ElectricAi.Plans/Entities/PushSubscription.cs` | EF entity |
| `src/Reshape.ElectricAi.Plans/Persistence/Configurations/PushSubscriptionConfiguration.cs` | EF mapping. Table `plans."PushSubscriptions"`. Unique index on `Endpoint`. |
| `src/Reshape.ElectricAi.Plans/Persistence/Specifications/PushSubscriptionByEndpointSpec.cs` | Spec for endpoint lookup |
| `src/Reshape.ElectricAi.Plans/Services/PushService.cs` | Implementation. Uses `WebPush` NuGet (NOT installed yet). |
| `src/Reshape.ElectricAi.Plans/Validators/SubscribeRequestValidator.cs` | https-only endpoint, key length caps |
| `src/Reshape.ElectricAi.Plans/Validators/SendRequestValidator.cs` | Required title/body, URI sanity |
| `src/Reshape.ElectricAi.Plans/Validators/UnsubscribeRequestValidator.cs` | Endpoint non-empty |
| `src/Reshape.ElectricAi.Presentation/Controllers/PushController.cs` | Routes under `api/v1/push` |
| `src/Reshape.ElectricAi.Plans/Persistence/PlansDbContext.cs` | Added `DbSet<PushSubscription> PushSubscriptions` |
| `src/Reshape.ElectricAi.Plans/PlansModule.cs` | Binds `PushOptions`, registers `IPushService` |
| `src/Reshape.ElectricAi.Presentation/appsettings.json` | Added empty `Push` section + `localhost:4200` to CORS |

### Not done — your job

1. Install `WebPush` NuGet in Plans project. Build currently fails on missing `WebPush` namespace.
2. Generate VAPID key pair, persist via `dotnet user-secrets` on Presentation project.
3. Generate EF Core migration `AddPushSubscriptions` in Plans, verify it applies.
4. `dotnet build` clean across solution.
5. Smoke test: `curl https://localhost:7137/api/v1/push/public-key` returns the key.
6. Optional: write integration test for subscribe round-trip with Testcontainers.PostgreSql (matches existing pattern — see CODE.md §Tests).

---

## Hard constraints — from CODE.md

Re-read `/Users/solomonpaul/Projects/Reshape.ElectricAi/CODE.md` before any code edit. Hot rules for this scope:

- **Controllers ONLY in `Reshape.ElectricAi.Presentation`.** Plans must not reference `Microsoft.AspNetCore.Mvc.*`.
- **One DI entry-point per lib:** `XxxModule.AddXxxModule`. Don't create new module class — push registrations live inside existing `PlansModule.AddPlansModule`.
- **Migrations live in `src/Reshape.ElectricAi.Plans/Migrations/`** — Plans owns its DbContext.
- **Specs in `Plans/Persistence/Specifications/<Name>Spec.cs`**, `sealed`, name ends in `Spec`.
- **DTOs are records in Core**, manual mapping, no AutoMapper.
- **Validators auto-registered via reflection scan** in `PlansModule.RegisterValidators` — no extra wiring needed.
- **Error envelope:** `{ error: { code, message, details? } }`. Domain exceptions inherit `DomainException(code, message)` and map via `ExceptionHandlerMiddleware`. Already handled — only flag if pushing new exception types.
- **CancellationToken mandatory** on every public async method + every controller action.
- **`var` always for locals.** File-scoped namespaces. One public type per file. `_camelCase` private fields.
- **No Minimal APIs**, no Newtonsoft.Json, no AutoMapper, no MediatR, no `Task.Run` fire-and-forget, no catching bare `Exception` without rethrow.
- **Packages added ONLY by humans.** `bash-guard.sh` blocks `dotnet add package` auto-runs. Claude lists package + version + project + reason and waits. The package install in §1 below is the human action — instruct the user to run it.
- **Plans hosts EF infrastructure today.** When second lib needs persistence, promote `EfRepository<TContext, T>` + `SpecificationEvaluator` + closing class to new `Reshape.ElectricAi.Infrastructure`. Not relevant here, just don't pull Plans into another feature lib in the interim.
- **Startup migrate only in Development:** `Program.cs` already does this for `PlansDbContext` — your new migration will auto-apply on Dev startup.

---

## Step-by-step execution

### 1. Install WebPush package

User must run (you cannot — hook blocks it):

```bash
dotnet add src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj package WebPush
```

Latest 1.x is fine (`1.0.x` series). Package id: `WebPush` (https://www.nuget.org/packages/WebPush). Pure C# port of the Node web-push library. Provides `WebPushClient`, `VapidDetails`, `WebPush.PushSubscription`, `WebPushException`, `VapidHelper.GenerateVapidKeys()`.

After install, expect `WebPush` PackageReference in `src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj`.

### 2. Generate VAPID keys + persist via user-secrets

Two options. Pick whichever is convenient.

**Option A — npm CLI (cross-platform, no .NET execution):**

```bash
npx --yes web-push generate-vapid-keys --json
```

Returns `{ "publicKey": "...", "privateKey": "..." }` (URL-safe base64).

**Option B — pure .NET one-shot (after WebPush package installed):**

Create a throwaway console snippet OR run via `dotnet-script`:

```csharp
var keys = WebPush.VapidHelper.GenerateVapidKeys();
Console.WriteLine($"Public:  {keys.PublicKey}");
Console.WriteLine($"Private: {keys.PrivateKey}");
```

Either way, persist into user-secrets on the **Presentation** project:

```bash
# only first time on this machine
dotnet user-secrets init --project src/Reshape.ElectricAi.Presentation

dotnet user-secrets set "Push:VapidPublicKey"  "<PUBLIC>"  --project src/Reshape.ElectricAi.Presentation
dotnet user-secrets set "Push:VapidPrivateKey" "<PRIVATE>" --project src/Reshape.ElectricAi.Presentation
dotnet user-secrets set "Push:Subject"         "mailto:paul@bringingweb.com" --project src/Reshape.ElectricAi.Presentation
```

`Push:Subject` is the contact mailto exposed in push request headers per VAPID spec. Required by push services (FCM/APNs/Mozilla autopush).

Verify:

```bash
dotnet user-secrets list --project src/Reshape.ElectricAi.Presentation
```

Expect three `Push:*` keys.

### 3. Create EF migration

From repo root:

```bash
dotnet ef migrations add AddPushSubscriptions \
  --project src/Reshape.ElectricAi.Plans \
  --startup-project src/Reshape.ElectricAi.Presentation \
  --output-dir Migrations
```

Inspect generated migration. Should create `plans."PushSubscriptions"` table with columns `Id` (uuid PK), `UserId` (uuid null), `Endpoint` (text, unique), `P256dh` (varchar 200), `Auth` (varchar 64), `UserAgent` (varchar 512 null), `CreatedUtc` (timestamp), `LastSeenUtc` (timestamp), plus index on `UserId`. No FK to Users — cross-context navs are forbidden by CODE.md, but `UserId` is in same context here so an FK would actually be allowed. Skipping the FK is fine and matches the loose-coupling style for a hackathon. If reviewer pushes for one, add it.

Migration auto-applies in Dev on startup via `Program.cs:122`. To apply manually:

```bash
dotnet ef database update \
  --project src/Reshape.ElectricAi.Plans \
  --startup-project src/Reshape.ElectricAi.Presentation
```

### 4. Build clean

```bash
dotnet build
```

Must finish 0 errors, 0 warnings (TreatWarningsAsErrors on every csproj).

### 5. Smoke test

```bash
dotnet run --project src/Reshape.ElectricAi.Presentation
```

In another shell:

```bash
curl -k https://localhost:7137/api/v1/push/public-key
# expect: {"publicKey":"BNc..."}
```

Subscribe round-trip (manual):

```bash
curl -k -X POST https://localhost:7137/api/v1/push/subscribe \
  -H "Content-Type: application/json" \
  -d '{"endpoint":"https://fake.example.com/abc","p256dh":"BPq...","auth":"K7g...","userAgent":"curl"}'
# expect: 204 No Content
```

Confirm row landed:

```sql
SELECT "Endpoint", "UserId", "CreatedUtc" FROM plans."PushSubscriptions";
```

Send fan-out (won't actually deliver — fake endpoint — but exercises the pipe):

```bash
curl -k -X POST https://localhost:7137/api/v1/push/send \
  -H "Content-Type: application/json" \
  -d '{"title":"Test","body":"Hello","url":"/"}'
# expect: {"delivered":0,"pruned":1,"failed":0}  -- pruned because fake endpoint returns non-2xx → treated as dead
```

The fake will likely throw a WebPushException with a non-Gone/404 status → end up in `failed`, not `pruned`. That's fine — the goal of the smoke test is the request reaching the service. Real delivery exercised once FE wires a real `pushManager.subscribe()` and pipes the resulting subscription in.

### 6. Optional integration test

Match `Reshape.ElectricAi.Plans.Tests` pattern (xUnit + FluentAssertions + Testcontainers.PostgreSql per CODE.md §Tests). Skip if test project doesn't exist yet — flag as follow-up.

---

## API contract (frozen — FE depends on this)

Base: `https://localhost:7137/api/v1/push` (dev) / `<prod-host>/api/v1/push`.

All requests/responses JSON, `application/json; charset=utf-8`. Error envelope: `{ error: { code, message, details? } }`.

### GET `/public-key`

- Auth: anonymous.
- Response 200: `{ "publicKey": "<urlsafe-base64>" }`. Caller passes this as the `applicationServerKey` to `PushManager.subscribe`.

### POST `/subscribe`

- Auth: anonymous (TODO: switch to `[Authorize]` once FE wires real JWT login).
- Body:
  ```json
  {
    "endpoint": "https://fcm.googleapis.com/...",
    "p256dh":   "BPq...",
    "auth":     "K7g...",
    "userAgent": "Mozilla/5.0 ..."
  }
  ```
- Idempotent on `endpoint` — re-subscribing updates keys + `LastSeenUtc`, no duplicate row.
- Response 204 on success.
- Response 400 on validation failure (envelope).

### POST `/unsubscribe`

- Auth: anonymous.
- Body: `{ "endpoint": "https://..." }`.
- Response 204. No-op if endpoint not stored.

### POST `/send`

- Auth: anonymous (TODO: gate before going public — see "Auth gate TODO" below).
- Body:
  ```json
  {
    "title": "...",       // required, ≤120 chars
    "body":  "...",       // required, ≤500 chars
    "icon":  "/icons/...", // optional, absolute URL or absolute path
    "badge": "/icons/...", // optional, same
    "url":   "/some-route" // optional, tap-to-open target
  }
  ```
- Fan-out to all stored subscriptions.
- Response 200: `{ "delivered": N, "pruned": N, "failed": N }`.
  - `pruned` = subs that returned 404/410 from the push service and were deleted.
  - `failed` = other push errors (5xx from push service, etc.) — left in DB for retry next time.

### Wire payload (what the SW receives)

`PushService` serializes `PushPayload` as JSON with camelCase keys:

```json
{ "title": "...", "body": "...", "icon": "...", "badge": "...", "url": "..." }
```

Service worker reads `event.data.json()`, calls `registration.showNotification(title, { body, icon, badge, data: { url } })`. FE owns this side — listed here so the contract is unambiguous.

---

## Architecture decisions + rationale

| Decision | Why |
|---|---|
| Subscriptions live in `plans` schema, not a new module | Hackathon scope. User-adjacent. Avoids new csproj + DbContext + module wiring. |
| `UserId` nullable on entity | FE auth is currently mock (localStorage). Real JWT wiring deferred. Once wired, controller pulls `sub` claim and writes it. Nullable absorbs both eras. |
| Anonymous on `/send` | Frontend has no real bearer token yet. Path obscurity ("we won't show that page") is the demo gate. Replace with `[Authorize(Roles="Organizer")]` when FE login lands. |
| Fan-out to all subs, no filtering | Hackathon. One pool. Filter later if needed. |
| Inline `WebPushClient` (no DI singleton) | `WebPushClient` is cheap, no state. PushService is scoped, one per request. If perf matters, register as singleton — not yet. |
| 404/410 → prune; other errors → leave + log | Matches Web Push spec semantics — these two codes mean "this endpoint is dead forever". 5xx from push service is transient. |
| `WebPushException` swallowed per subscription, loop continues | One dead device shouldn't break the fan-out for the rest. |
| Serilog source-generated loggers (`LoggerMessage`) | Matches existing pattern in `ExceptionHandlerMiddleware`. Zero-alloc structured logging. |
| Endpoint validator requires HTTPS | Push service endpoints are always https. Defense against garbage input. |
| No FK from `PushSubscriptions.UserId` to `Users.Id` | Loose coupling, hackathon. Adding the FK is fine if you want stricter integrity — both are in the same schema/context. |

---

## Caveats + known follow-ups

- **Single API instance assumed.** Multi-instance fan-out would double-deliver. Out of scope.
- **No retry on transient push failures.** `failed` count includes 5xx; manually re-send if needed.
- **Auth gate TODO** on `/send`. Add `[Authorize(Roles="Organizer")]` when FE real-login lands. Flag in TODO.md or wherever the team tracks tech debt.
- **iOS quirks (FE side, not yours):** PWA install required, iOS 16.4+, HTTPS-served. Document for the demo team.
- **VAPID keys are per environment.** Generate fresh pair for staging/prod. Never commit the private key.
- **`var` everywhere** — CODE.md is strict on this. `TreatWarningsAsErrors` will catch most style drift but `var` enforcement is `.editorconfig`.

---

## Verification checklist (mark done before handing back)

- [ ] `WebPush` package installed in Plans csproj, version pinned (record in PR description)
- [ ] User-secrets has `Push:VapidPublicKey`, `Push:VapidPrivateKey`, `Push:Subject`
- [ ] Migration `AddPushSubscriptions` generated, inspected, applied to local Postgres
- [ ] `dotnet build` 0 errors 0 warnings
- [ ] `curl /api/v1/push/public-key` returns key
- [ ] `curl /api/v1/push/subscribe` with bogus payload returns 204, row appears in Postgres
- [ ] `curl /api/v1/push/send` returns SendResult JSON (delivered=0 expected if only bogus sub stored)
- [ ] CODE.md re-read before each .cs edit (CLAUDE.md Phase 7)
- [ ] Code reviewer agent dispatched with "verify CODE.md compliance" directive
- [ ] Plan-mode workflow followed if scope creeps beyond this PRD (see `/Users/solomonpaul/Projects/Reshape.ElectricAi/CLAUDE.md`)

---

## Out of scope (explicit)

- Frontend changes. Separate session handles `PushApiService`, `AdminNotifyComponent`, preset library, SW message bridge, permission-prompt UX.
- Real JWT auth on FE. Coming later.
- Scheduled/delayed sends. Fire-now only.
- Notification action buttons. Tap-to-open only.
- Per-device targeting. Single pool fan-out.
- Production VAPID key management / rotation strategy. Document later.

---

## References

- CODE.md: `/Users/solomonpaul/Projects/Reshape.ElectricAi/CODE.md`
- CLAUDE.md (workflow): `/Users/solomonpaul/Projects/Reshape.ElectricAi/CLAUDE.md`
- PROJECT.md (layout + commands): `/Users/solomonpaul/Projects/Reshape.ElectricAi/PROJECT.md`
- WebPush package: https://www.nuget.org/packages/WebPush
- W3C Push API spec: https://www.w3.org/TR/push-api/
- VAPID RFC 8292: https://datatracker.ietf.org/doc/html/rfc8292
