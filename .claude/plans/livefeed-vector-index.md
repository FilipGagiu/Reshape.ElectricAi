# LiveFeed → VectorDb publish-only indexing — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

---

## CLAUDE.md non-negotiable phase list (restated verbatim — DO NOT skip a phase)

1. **Invoke task-specific superpowers skill(s)** — match the task to a skill from §7. Fire BEFORE entering plan mode. Named mappings:
   - New feature / behavior change → `superpowers:brainstorming`
   - Bug, test failure, unexpected behavior, build failure → `superpowers:systematic-debugging`
   - Implementation that admits unit tests → `superpowers:test-driven-development`
   - About to claim "done" / "fixed" / "passing" → `superpowers:verification-before-completion`

   If none of the named mappings fit, scan the full installed superpowers skill list for any skill that might help. If still nothing fits, proceed without one — that's an acceptable outcome, but document the full-list-scan result in the plan's Phase 1 application note. Silent skipping is not acceptable.
2. **Enter plan mode** (`EnterPlanMode`) — before ANY file edit. No exceptions for "small" or "trivial".
3. **Inventory / explore** — gather facts via Explore agents (parallel where useful) or direct reads. Do not guess.
4. **Design** — propose specific custom agents for review, exploration, or design feedback (NOT implementation — see §2). Review-agent dispatches MUST include "verify CODE.md compliance against the changed files" as an explicit directive. Recommend; do not decide unilaterally.
5. **Write the plan** to `.claude/plans/<slug>.md`. **Every plan MUST start by restating this phase list verbatim** so no phase is silently skipped.
6. **`ExitPlanMode`** — the single approval gate. Wait for explicit user approval.
7. **Execute** — YOU edit the files; only dispatch agents for review or parallel exploration. **Re-read [CODE.md](CODE.md) before each code edit** and verify the change honors every rule there. After approval.
8. **Verify** — build + tests + visible evidence. No "trust me" claims.
9. **Promote learnings to memory** — `/si:remember` for facts; direct-edit CODE.md (code rules), CLAUDE.md (workflow), or PROJECT.md (project context) for enforced rules. Penultimate step.
10. **Delete the plan file** — last step. Code + commit history is the source of truth after.

If you catch yourself about to skip any phase, STOP. Re-read this list. The user has had to remind me of this repeatedly — that is the failure mode this section exists to prevent.

### Phase 1 application note (this plan)

- `superpowers:brainstorming` invoked (design spec at `docs/superpowers/specs/2026-05-23-livefeed-vector-index-design.md`).
- `superpowers:test-driven-development` applies to all source-code-modifying tasks below (Tasks 4–6).
- `superpowers:verification-before-completion` applies before claiming success at Task 7.

---

**Goal:** Wire LiveFeed's `FeedService.PublishEntryAsync` to emit a `vector.event_entries` row via `IIngestService.IngestEventAsync` after the SSE broadcast, with embedding failures swallowed so publish never depends on OpenAI.

**Architecture:** A single new step at the tail of `PublishEntryAsync`: `await SafeIngestEventAsync(entry, ct)`. Mapping helper `ToIngestEventRequest` builds the request from a `FeedEntry`. Embedding errors caught + logged, never propagated. Two new integration tests prove (a) the row lands in VectorDb and (b) publish stays green when the embedder throws.

**Tech Stack:** .NET 10, ASP.NET Core 10, EF Core 10, Npgsql + Pgvector, Testcontainers `pgvector/pgvector:pg16`, xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing.

**Scope guardrails (per user instruction):**
- Modify ONLY: `src/Reshape.ElectricAi.LiveFeed/`, `src/Reshape.ElectricAi.Presentation/` (no changes required), `src/Reshape.ElectricAi.Core/` (no changes required), `tests/Reshape.ElectricAi.LiveFeed.Tests/`.
- Touching `src/Reshape.ElectricAi.VectorDb/`, `tests/Reshape.ElectricAi.VectorDb.Tests/`, `src/Reshape.ElectricAi.Plans/`, or any other source REQUIRES asking the user first.
- No new NuGet packages (hook-enforced; CLAUDE.md §6a).
- No git commits in this plan — the user has disabled git operations for this session. After each "verify" step, the user reviews the diff manually and decides when to commit.

---

## File structure

**Created:**
- `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FakeEmbeddingService.cs` — deterministic embedder (32-dim, hash-seeded, unit-normalized). Mirrors the implementation already in `tests/Reshape.ElectricAi.VectorDb.Tests/FakeEmbeddingService.cs`.
- `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/ThrowingEmbeddingService.cs` — always throws `InvalidOperationException("simulated embed failure")`. Used by the failure-swallow test.
- `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/FeedVectorIndexTests.cs` — two integration tests.

**Modified:**
- `src/Reshape.ElectricAi.LiveFeed/Services/FeedService.cs` — add `IIngestService ingestService` and `ILogger<FeedService> logger` ctor params, add private `SafeIngestEventAsync`, call it as the final step of `PublishEntryAsync`.
- `src/Reshape.ElectricAi.LiveFeed/Dtos/Mapping/FeedEntryMapping.cs` — add `ToIngestEventRequest(this FeedEntry)` extension method.
- `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FeedApiFactory.cs` — register `FakeEmbeddingService` in place of `OpenAiEmbeddingService` in `ConfigureTestServices`; add `ResetVectorEventsAsync()` helper.

**Untouched:**
- `src/Reshape.ElectricAi.LiveFeed/LiveFeedModule.cs` — no change. `IIngestService` is registered by upstream `AddVectorDbModule`.
- `src/Reshape.ElectricAi.Presentation/Program.cs` — no change. VectorDb migration already runs in Development|Testing env (lines 132-141).
- `src/Reshape.ElectricAi.Core/**` — no change. `IIngestService` and `IngestEventRequest` already exist there.
- All VectorDb source — out of scope per user rule.

---

## Pre-task setup (one-time per session)

- [ ] **Re-read CODE.md fully.** Required by CLAUDE.md Phase 7 before each code edit. Re-read at the start of every task that touches `.cs` files.

- [ ] **Confirm tests baseline is green.** Required precondition for measuring regression after the change.

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests --nologo
  ```

  Expected: `Passed:  38` (the existing LiveFeed test count per the README §13). If the count differs, STOP and reconcile before continuing — the regression target is "no test that was passing now fails".

---

## Task 1: Add `FakeEmbeddingService` test fixture

**Goal:** Provide a deterministic, zero-cost `IEmbeddingService` for the test host so VectorDb-module DI resolution does not depend on OpenAI.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FakeEmbeddingService.cs`

- [ ] **Step 1: Re-read CODE.md (Phase 7 requirement).**

- [ ] **Step 2: Create the fixture file** with the content below.

```csharp
using System.Security.Cryptography;
using System.Text;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class FakeEmbeddingService(int dimensions) : IEmbeddingService
{
    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GenerateVector(text));

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(
            texts.Select(GenerateVector).ToList());

    private ReadOnlyMemory<float> GenerateVector(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var seed = BitConverter.ToInt32(hash, 0);
        var rng = new Random(seed);
        var floats = new float[dimensions];

        for (var i = 0; i < dimensions; i++)
            floats[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

        var magnitude = MathF.Sqrt(floats.Sum(f => f * f));
        for (var i = 0; i < dimensions; i++)
            floats[i] /= magnitude;

        return floats;
    }
}
```

- [ ] **Step 3: Build the test project to confirm the fixture compiles.**

  ```powershell
  dotnet build tests/Reshape.ElectricAi.LiveFeed.Tests --nologo
  ```

  Expected: `Build succeeded.` with zero warnings introduced in `LiveFeed.Tests`.

---

## Task 2: Add `ThrowingEmbeddingService` test fixture

**Goal:** Provide an embedder that always throws, for the swallow-error test.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/ThrowingEmbeddingService.cs`

- [ ] **Step 1: Re-read CODE.md.**

- [ ] **Step 2: Create the fixture file** with the content below.

```csharp
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class ThrowingEmbeddingService : IEmbeddingService
{
    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("simulated embed failure");

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("simulated embed failure");
}
```

- [ ] **Step 3: Build to confirm.**

  ```powershell
  dotnet build tests/Reshape.ElectricAi.LiveFeed.Tests --nologo
  ```

  Expected: success.

---

## Task 3: Extend `FeedApiFactory` — swap embedder + add `ResetVectorEventsAsync`

**Goal:** Make the test host resolve `FakeEmbeddingService` instead of `OpenAiEmbeddingService`, and give tests a per-test cleanup for `vector.event_entries`.

**Files:**
- Modify: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FeedApiFactory.cs`

- [ ] **Step 1: Re-read CODE.md.**

- [ ] **Step 2: Add the embedding dimension constant** at the top of `FeedApiFactory` (just below `TestSigningKey`):

```csharp
public const int TestEmbeddingDimensions = 32;
```

- [ ] **Step 3: Override `IEmbeddingService` in `ConfigureTestServices`.** Locate the existing block:

```csharp
builder.ConfigureTestServices(services =>
{
    var descriptor = services.Single(d => d.ServiceType == typeof(IUserPrefsProvider));
    services.Remove(descriptor);
    services.AddScoped<IUserPrefsProvider>(_ => FakePrefs);
});
```

Replace with:

```csharp
builder.ConfigureTestServices(services =>
{
    var prefsDescriptor = services.Single(d => d.ServiceType == typeof(IUserPrefsProvider));
    services.Remove(prefsDescriptor);
    services.AddScoped<IUserPrefsProvider>(_ => FakePrefs);

    // Swap the OpenAI-backed embedder for a deterministic fake so the test host
    // does not need a real API key and so tests are reproducible.
    var embedDescriptor = services.Single(d => d.ServiceType == typeof(IEmbeddingService));
    services.Remove(embedDescriptor);
    services.AddScoped<IEmbeddingService>(_ => new FakeEmbeddingService(TestEmbeddingDimensions));
});
```

- [ ] **Step 4: Set `Chat__EmbeddingDimensions` so the VectorDb DbContext schema matches the fake vector width.** Inside `CreateHost`, add immediately after the existing `OpenAi__ApiKey` line:

```csharp
Environment.SetEnvironmentVariable("Chat__EmbeddingDimensions", TestEmbeddingDimensions.ToString());
```

  And add a matching unset in `DisposeAsync`:

```csharp
Environment.SetEnvironmentVariable("Chat__EmbeddingDimensions", null);
```

  Rationale: `VectorDbModule.BuildChatOptions` reads `Chat:EmbeddingDimensions` (default 1536). Pgvector columns are typed `vector(N)` at migration time — if the fake emits 32-dim and the column expects 1536, inserts fail with a dimension mismatch.

- [ ] **Step 5: (verified at plan-write time) Column width is configurable.** `src/Reshape.ElectricAi.VectorDb/Persistence/VectorDbContext.cs:26` builds the pgvector column type as `$"vector({_embeddingDimensions})"` from `ChatOptions.EmbeddingDimensions`. Setting `Chat__EmbeddingDimensions=32` in the test host (step 4) drives the column width to match `FakeEmbeddingService`. No further action needed on this step.

- [ ] **Step 6: Add `ResetVectorEventsAsync` helper** after the existing `ResetDatabaseAsync`:

```csharp
public async Task ResetVectorEventsAsync()
{
    using var scope = Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
    await db.Database.ExecuteSqlRawAsync(
        "TRUNCATE vector.event_entries RESTART IDENTITY CASCADE;");
}
```

  Add the using directives at the top of the file if not already present:

```csharp
using Reshape.ElectricAi.VectorDb.Persistence;
```

- [ ] **Step 7: Verify the LiveFeed.Tests project references VectorDb.** Run:

  ```powershell
  Select-String -Path tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj -Pattern "VectorDb"
  ```

  Decision:
  - **If a `ProjectReference` to `Reshape.ElectricAi.VectorDb` exists:** done.
  - **If NOT:** the test project cannot resolve `VectorDbContext`. Adding a project reference is editing the csproj inside the tests project, which is in scope — proceed and add it manually with:

    ```powershell
    dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj `
      reference src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj
    ```

    Note: `dotnet add reference` is NOT blocked by the package-install guard (it's a project reference, not a NuGet package). Confirm with the user before running if uncertain.

- [ ] **Step 8: Build the test project and confirm no new warnings.**

  ```powershell
  dotnet build tests/Reshape.ElectricAi.LiveFeed.Tests --nologo
  ```

  Expected: `Build succeeded.`

- [ ] **Step 9: Run the full LiveFeed test suite to confirm no regression** from the fixture changes alone (no source change yet).

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests --nologo
  ```

  Expected: `Passed:  38` (same as baseline).

---

## Task 4: Write the failing integration test #1 — happy-path indexing

**Goal:** Lock in the happy-path behavior with a failing test BEFORE writing production code (TDD per `superpowers:test-driven-development`).

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedVectorIndexTests.cs` (placed under `Endpoints/` to match the existing organisation: `FeedCrudTests.cs`, `FeedSseTests.cs`, `FeedServiceBroadcastOrderingTests.cs` all live there).

- [ ] **Step 1: Re-read CODE.md.**

- [ ] **Step 2: Create the test file** with one test method initially:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection(PostgresCollection.Name)]
public sealed class FeedVectorIndexTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly FeedApiFactory _factory = new(postgres);

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        await _factory.ResetVectorEventsAsync();
    }

    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [Fact]
    public async Task Publishing_an_entry_persists_an_EventEntry_in_VectorDb()
    {
        var organizerId = Guid.NewGuid();
        var client = _factory.CreateClientForUser(organizerId, UserRole.Organizer);

        var request = new PublishFeedEntryRequest(
            Title: "Stage delay",
            Body: "Main Stage delayed 30 min",
            PrimaryCategory: Category.Music,
            IsGeneral: false,
            TargetArtists: [],
            TargetGenres: [MusicGenre.Rock, MusicGenre.Techno]);

        var response = await client.PostAsJsonAsync("/api/v1/feed", request, TestJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<FeedEntryDto>(TestJson.Options);
        dto.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
        var stored = await vectorDb.EventEntries
            .SingleAsync(e => e.FeedEntryId == dto!.Id);

        stored.Title.Should().Be("Stage delay");
        stored.TextRepresentation.Should().Be("Stage delay\n\nMain Stage delayed 30 min");
        stored.EventUtc.Should().Be(dto!.PublishedUtc);
        stored.Embedding.Memory.Length.Should().Be(FeedApiFactory.TestEmbeddingDimensions);
        stored.CategoryTags.Should().BeEquivalentTo(new[] { "Music.Rock", "Music.Techno" });
    }
}
```

  Verify the using directives compile. If `PublishFeedEntryRequest` or `FeedEntryDto` are in a different namespace than declared above, fix the using statements based on the actual code (look at `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/FeedCrudTests.cs` for the canonical pattern — copy the using block from there if needed).

- [ ] **Step 3: Run the test and confirm it FAILS** (production code does not yet write the vector row).

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests `
    --filter "FullyQualifiedName~FeedVectorIndexTests.Publishing_an_entry_persists_an_EventEntry_in_VectorDb" `
    --nologo
  ```

  Expected: `Failed: 1`. Failure reason should be `Sequence contains no elements` (from `SingleAsync`) — no `EventEntry` row exists because `FeedService.PublishEntryAsync` is not yet wired to call `IIngestService`. **If the test fails for a different reason (compilation error, DI exception, dimension mismatch), STOP and diagnose before continuing.**

---

## Task 5: Add `ToIngestEventRequest` mapping

**Goal:** Implement the pure mapping helper that the production code will call. No behavior change yet — the helper just exists.

**Files:**
- Modify: `src/Reshape.ElectricAi.LiveFeed/Dtos/Mapping/FeedEntryMapping.cs`

- [ ] **Step 1: Re-read CODE.md.**

- [ ] **Step 2: Add the using directives at the top of `FeedEntryMapping.cs`:**

```csharp
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;
```

  (The existing file already has `using Reshape.ElectricAi.Core.Dtos;` and `using Reshape.ElectricAi.LiveFeed.Entities;` — preserve those.)

- [ ] **Step 3: Append the new extension method** at the bottom of the `FeedEntryMapping` class, before the closing brace:

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

- [ ] **Step 4: Build the LiveFeed project to confirm the mapping compiles.**

  ```powershell
  dotnet build src/Reshape.ElectricAi.LiveFeed --nologo
  ```

  Expected: `Build succeeded.` with zero new warnings.

- [ ] **Step 5: Re-run the failing test from Task 4 to confirm it still fails** (the mapping is not yet called by anything).

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests `
    --filter "FullyQualifiedName~FeedVectorIndexTests.Publishing_an_entry_persists_an_EventEntry_in_VectorDb" `
    --nologo
  ```

  Expected: still `Failed: 1` for the same reason (`Sequence contains no elements`).

---

## Task 6: Wire `FeedService.PublishEntryAsync` to call `IIngestService`

**Goal:** Production code change. The failing test from Task 4 turns green.

**Files:**
- Modify: `src/Reshape.ElectricAi.LiveFeed/Services/FeedService.cs`

- [ ] **Step 1: Re-read CODE.md.**

- [ ] **Step 2: Update the class declaration** to inject `IIngestService` and `ILogger<FeedService>`. The current signature is:

```csharp
internal sealed class FeedService(
    IRepository<FeedEntry> repository,
    IFeedBroadcaster broadcaster) : IFeedService
```

  Replace with:

```csharp
internal sealed class FeedService(
    IRepository<FeedEntry> repository,
    IFeedBroadcaster broadcaster,
    IIngestService ingestService,
    ILogger<FeedService> logger) : IFeedService
```

- [ ] **Step 3: Add the `using Microsoft.Extensions.Logging;` directive** at the top of the file (after the existing usings).

- [ ] **Step 4: Add the `SafeIngestEventAsync` private method** at the bottom of the class, before the closing brace:

```csharp
private async Task SafeIngestEventAsync(FeedEntry entry, CancellationToken ct)
{
    try
    {
        await ingestService.IngestEventAsync(entry.ToIngestEventRequest(), ct);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "Vector indexing failed for FeedEntry {FeedEntryId} after publish; entry is committed and broadcast, vector index will be stale until a future re-ingest.",
            entry.Id);
    }
}
```

- [ ] **Step 5: Call `SafeIngestEventAsync` as the LAST step of `PublishEntryAsync`.** The current body is:

```csharp
public async Task<FeedEntryDto> PublishEntryAsync(
    Guid organizerId, PublishFeedEntryCommand command, CancellationToken ct)
{
    var entry = command.ToNewEntity(organizerId);
    await repository.AddAsync(entry, ct);
    await repository.SaveChangesAsync(ct);

    var dto = entry.ToDto();
    broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, dto);
    return dto;
}
```

  Replace with:

```csharp
public async Task<FeedEntryDto> PublishEntryAsync(
    Guid organizerId, PublishFeedEntryCommand command, CancellationToken ct)
{
    var entry = command.ToNewEntity(organizerId);
    await repository.AddAsync(entry, ct);
    await repository.SaveChangesAsync(ct);

    var dto = entry.ToDto();
    broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, dto);

    await SafeIngestEventAsync(entry, ct);

    return dto;
}
```

  Ordering rationale (matches spec §4 + §7): broadcast strictly before ingest. If ingest fails or is slow, subscribers are unaffected. The `return dto` happens after ingest so that a successful publish is fully "settled" from the caller's perspective, but if ingest throws cancellation, the caller sees the cancellation (not 201).

- [ ] **Step 6: Build the solution to catch any DI / signature errors.**

  ```powershell
  dotnet build --nologo
  ```

  Expected: `Build succeeded.` with zero new warnings in `Reshape.ElectricAi.LiveFeed.*`.

- [ ] **Step 7: Run the Task 4 test to confirm it now PASSES.**

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests `
    --filter "FullyQualifiedName~FeedVectorIndexTests.Publishing_an_entry_persists_an_EventEntry_in_VectorDb" `
    --nologo
  ```

  Expected: `Passed: 1`. If it fails on the embedding-dimension check or DI resolution, return to Task 3 step 5 (column width mismatch) before continuing.

---

## Task 7: Write + verify failing test #2 — embedding failure swallowed (publish + broadcast unaffected)

**Goal:** Prove that an embedding-service exception does NOT fail the publish request AND does NOT suppress the SSE broadcast (per spec §8 Test 2 — both assertions required).

**Files:**
- Modify: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedVectorIndexTests.cs` (append a second test + a small SSE-reader helper duplicated from `FeedSseTests.cs:26-58` — duplication accepted to avoid coupling test files)

- [ ] **Step 1: Re-read CODE.md.**

- [ ] **Step 2: Add a per-test factory subclass** at the bottom of `FeedVectorIndexTests.cs` (above the closing namespace brace if file-scoped):

```csharp
internal sealed class ThrowingEmbedFeedApiFactory(PostgresFixture postgres) : FeedApiFactory(postgres)
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(IEmbeddingService));
            services.Remove(descriptor);
            services.AddScoped<IEmbeddingService, ThrowingEmbeddingService>();
        });
    }
}
```

  Required using directives (add if not already present):

```csharp
using Microsoft.AspNetCore.TestHost;
using Reshape.ElectricAi.Core.Services;
```

- [ ] **Step 3: Add an SSE-reader helper inside `FeedVectorIndexTests`** (duplicated verbatim from `FeedSseTests.cs:26-58` — comment-link the source so future readers know the original):

```csharp
// Duplicated from FeedSseTests.ReadStreamForAsync (FeedSseTests.cs:26-58) to keep
// this file self-contained. Cancellation is the normal exit for an SSE consumer.
private static async Task<string> ReadStreamForAsync(
    HttpClient client, string url, CancellationToken ct, int maxBytes = 8192)
{
    using var req = new HttpRequestMessage(HttpMethod.Get, url);
    var buffer = new byte[maxBytes];
    var read = 0;
    try
    {
        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        while (read < maxBytes)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read, maxBytes - read), ct);
            if (n == 0) break;
            read += n;
        }
    }
    catch (OperationCanceledException) { }
    catch (IOException) { }
    return System.Text.Encoding.UTF8.GetString(buffer, 0, read);
}
```

- [ ] **Step 4: Append the second test method** inside `FeedVectorIndexTests`:

```csharp
[Fact]
public async Task Publishing_returns_201_and_broadcasts_even_when_embedder_throws()
{
    await using var throwingFactory = new ThrowingEmbedFeedApiFactory(postgres);
    await throwingFactory.ResetDatabaseAsync();
    await throwingFactory.ResetVectorEventsAsync();

    var subscriberUserId = Guid.NewGuid();
    throwingFactory.FakePrefs.Set(subscriberUserId, [], []); // IsGeneral=true entries reach everyone

    // Open an SSE subscriber on a parallel anonymous HttpClient.
    using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var listenTask = ReadStreamForAsync(
        throwingFactory.CreateAnonymousClient(),
        $"/api/v1/feed/stream?userId={subscriberUserId}",
        listenCts.Token);

    await Task.Delay(300, listenCts.Token); // let the subscriber register

    var organizerId = Guid.NewGuid();
    var client = throwingFactory.CreateClientForUser(organizerId, UserRole.Organizer);

    var request = new PublishFeedEntryRequest(
        Title: "Weather alert",
        Body: "Storm after 21:00",
        PrimaryCategory: Category.Weather,
        IsGeneral: true,
        TargetArtists: [],
        TargetGenres: []);

    var response = await client.PostAsJsonAsync("/api/v1/feed", request, TestJson.Options);
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var dto = await response.Content.ReadFromJsonAsync<FeedEntryDto>(TestJson.Options);
    dto.Should().NotBeNull();

    await Task.Delay(800, listenCts.Token); // give the broadcaster a moment
    listenCts.Cancel();
    var raw = await listenTask;

    // Spec §8 Test 2 assertions:
    raw.Should().Contain("event: feed.created");            // broadcast happened
    raw.Should().Contain("\"title\":\"Weather alert\"");    // correct payload

    using var scope = throwingFactory.Services.CreateScope();
    var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
    (await vectorDb.EventEntries.AnyAsync()).Should().BeFalse(); // embed threw → no vector row
}
```

  Notes:
  - The SSE subscriber proves the broadcast was NOT suppressed by the embed throw. This is the spec §8 Test 2 acceptance.
  - The `EventEntries.AnyAsync().Should().BeFalse()` proves the catch in `SafeIngestEventAsync` swallowed the exception (otherwise either the publish would have failed OR an EventEntry row would exist).

- [ ] **Step 5: Run the new test and confirm it PASSES immediately** (the swallow logic was already implemented in Task 6 step 4).

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests `
    --filter "FullyQualifiedName~FeedVectorIndexTests.Publishing_returns_201_and_broadcasts_even_when_embedder_throws" `
    --nologo
  ```

  Expected: `Passed: 1`. Common failure modes:
  - Publish returns 500 → `SafeIngestEventAsync` is not catching the exception. Return to Task 6 step 4.
  - SSE stream returns 401 → the test forgot to use `CreateAnonymousClient` (stream is `[AllowAnonymous]` in v1). Re-check step 4 of this task.
  - Subscriber receives nothing within 800 ms → broadcaster timing. Increase the delay to 1500 ms, do not change production code.

---

## Task 8: Full regression — every test, every project

**Goal:** Prove zero regression in the full suite. Required by spec §11 acceptance criteria.

- [ ] **Step 1: Run the full solution test suite.**

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test --nologo
  ```

  Expected:
  - LiveFeed.Tests: `Passed: 40` (38 baseline + 2 new).
  - Plans.Tests: `Passed: 32` (unchanged).
  - VectorDb.Tests: passes whatever count it had at baseline (Task 0 captured this implicitly via `dotnet test`).
  - Total: 70 baseline + 2 new = 72 expected.

  If any previously-passing test now fails, STOP and diagnose. The failure is almost certainly in `FeedService.PublishEntryAsync` — the new step changed the public method's behavior on cancellation and on subscribed-channel timing. Common failure modes to check first:
  - Existing publish tests now hang because they cancel mid-request and `IngestEventAsync` is still running.
  - SSE tests fail because the broadcast envelope ordering shifted (unlikely — broadcast still happens before the new step).

- [ ] **Step 2: Verify zero new build warnings.**

  ```powershell
  dotnet build --nologo /clp:NoSummary
  ```

  Compare warning count against the README's "0 build warnings in scope" claim. Any new warning in `Reshape.ElectricAi.LiveFeed.*` or `Reshape.ElectricAi.LiveFeed.Tests` is a regression — fix before completing.

- [ ] **Step 3: Spot-check the warning log.** Run the throwing-embedder test in verbose mode and confirm the warning line appears:

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests `
    --filter "FullyQualifiedName~FeedVectorIndexTests.Publishing_returns_201_and_indexes_zero_rows_when_embedder_throws" `
    --logger "console;verbosity=detailed" `
    --nologo
  ```

  Expected: the test output stream contains a line containing `"Vector indexing failed for FeedEntry"` (the log message text from `SafeIngestEventAsync`).

---

## Task 9: Code review by `Code Reviewer` agent

**Goal:** Required by CLAUDE.md §2 + §4 — every implementation gets an independent review pass with explicit "verify CODE.md compliance" directive.

- [ ] **Step 1: Dispatch the `Code Reviewer` agent** with the following prompt (copy verbatim):

  > Review the diff on branch `feature/live-feed-vector-integration` for the LiveFeed → VectorDb publish-only indexing change.
  >
  > Changed files:
  > - `src/Reshape.ElectricAi.LiveFeed/Services/FeedService.cs`
  > - `src/Reshape.ElectricAi.LiveFeed/Dtos/Mapping/FeedEntryMapping.cs`
  > - `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FeedApiFactory.cs`
  > - `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FakeEmbeddingService.cs` (new)
  > - `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/ThrowingEmbeddingService.cs` (new)
  > - `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/FeedVectorIndexTests.cs` (new)
  >
  > Verify CODE.md compliance against every changed file. Confirm: no Minimal APIs introduced, no new packages, no MVC views, controllers untouched. Specifically check `FeedService.SafeIngestEventAsync` for the cancellation-vs-error catch ordering (cancellation must rethrow, all other exceptions log + swallow). Check the mapping helper for null-safety and category-tag determinism. Flag anything that looks like it would survive a strict review.

- [ ] **Step 2: Apply any blocking findings.** Cosmetic suggestions can be deferred and noted in the final summary.

- [ ] **Step 3: Re-run the full suite** if any source change resulted from review:

  ```powershell
  $env:DOCKER_API_VERSION="1.43"
  dotnet test --nologo
  ```

  Expected: same pass count as Task 8.

---

## Task 10: Spec self-review against the final code

**Goal:** Verify the shipped code matches every claim in `docs/superpowers/specs/2026-05-23-livefeed-vector-index-design.md`.

- [ ] **Step 1: Open the spec and the diff side-by-side.**

  ```powershell
  git diff main --stat
  ```

- [ ] **Step 2: Walk the spec's acceptance criteria (§11) one by one:**
  - "`PublishEntryAsync` writes one `vector.event_entries` row per non-duplicate FeedEntry." → proved by Task 4 test.
  - "Embedding-service exceptions never fail `POST /api/v1/feed` and never suppress the SSE broadcast." → proved by Task 7 test (publish 201, FeedEntry persisted). SSE broadcast non-suppression is implicit (broadcast happens BEFORE the catch boundary).
  - "Two new integration tests pass." → Tasks 4 and 7.
  - "All 70 existing tests still pass." → Task 8.
  - "Zero new build warnings." → Task 8 step 2.
  - "CODE.md compliance verified by review-agent dispatch." → Task 9.

  Tick each off. If any is unproven, return to the relevant task.

---

## Task 11: Promote learnings to memory

**Goal:** CLAUDE.md Phase 9 — capture the non-obvious facts surfaced during this work so future sessions don't rediscover them.

- [ ] **Step 1: Invoke `/si:review`** to surface what auto-memory caught during this plan execution. Manually decide which entries are durable rules vs ephemeral context.

- [ ] **Step 2: Run `/si:remember` for each fact below** (the bar: would the next session waste time rediscovering this?).

  Candidates (judge each — drop if it's already in `CLAUDE.md` / `CODE.md` / `PROJECT.md`):
  - "Cross-module Core abstractions: `IIngestService` and `IngestEventRequest` live in `Core.Services` and `Core.Dtos.VectorSearch`. LiveFeed can call into VectorDb behavior without referencing the VectorDb assembly — DI bridges them at the host."
  - "VectorDb migrations auto-apply in both Development and Testing environments (`Program.cs:132`). LiveFeed integration tests get the full vector schema for free."
  - "Test fixture: setting `Chat__EmbeddingDimensions` env var changes the pgvector column width used by `VectorDbContext`. Required when swapping `IEmbeddingService` for a fake of a different dim. (Or: column width is hard-coded — record the actual finding from Task 3 step 5.)"
  - "Docker Desktop 4.25 caps API at 1.43. Testcontainers' default Docker.DotNet client emits API 1.44, throws `client version 1.44 is too new`. Mitigation: `$env:DOCKER_API_VERSION='1.43'` per shell, or set User env var permanently."

- [ ] **Step 3: Decide if any candidate is a durable rule that belongs in `CLAUDE.md` / `CODE.md` / `PROJECT.md`.**

  Likely promotion: the Docker API-version note belongs in `PROJECT.md` under "Local dev setup" so the next dev does not hit the same wall. Use direct edit (per CLAUDE.md §3a) after `/si:remember` captures it.

  No automatic promotions — judge per item.

---

## Task 12: Delete this plan file

**Goal:** CLAUDE.md Phase 10 — code + commit history are the source of truth after this point. Keeping a stale plan file invites confusion.

- [ ] **Step 1: Remove the file.**

  ```powershell
  Remove-Item .claude/plans/livefeed-vector-index.md
  ```

- [ ] **Step 2: Confirm removal.**

  ```powershell
  if (Test-Path .claude/plans/livefeed-vector-index.md) { Write-Output "STILL PRESENT" } else { Write-Output "removed" }
  ```

  Expected: `removed`.

---

## Risks + reminders

- **Docker required.** Testcontainers spins up `pgvector/pgvector:pg16`. If Docker is not running or `DOCKER_API_VERSION` is not set to `1.43`, every integration test fails before reaching the assertion. The pre-task setup check catches this early.
- **No git operations in this plan.** Every "verify" task ends with the test pass count, not a commit. The user reviews the diff manually and decides when to commit. The CLAUDE.md "delete the plan file" step is still done at Task 12, but with `Remove-Item`, not `git rm`.
- **Spec vs. plan drift.** If during execution a step needs to deviate from the spec, STOP and update the spec first, then resume.

