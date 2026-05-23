# LiveFeed SSE Infrastructure — Implementation Plan (rev 2, master-aligned)

> **CLAUDE.md non-negotiable phase list (restated verbatim per Phase 5 mandate):**
>
> 1. **Invoke task-specific superpowers skill(s)** — match the task to a skill from §7. Fire BEFORE entering plan mode.
> 2. **Enter plan mode** (`EnterPlanMode`) — before ANY file edit.
> 3. **Inventory / explore** — gather facts via Explore agents or direct reads. Do not guess.
> 4. **Design** — propose specific custom agents for review, exploration, or design feedback (NOT implementation). Review-agent dispatches MUST include "verify CODE.md compliance against the changed files" as an explicit directive.
> 5. **Write the plan** to `.claude/plans/<slug>.md`. Every plan MUST start by restating this phase list verbatim.
> 6. **`ExitPlanMode`** — single approval gate.
> 7. **Execute** — YOU edit the files; only dispatch agents for review or parallel exploration. **Re-read [CODE.md](../../CODE.md) before each code edit** and verify the change honors every rule there.
> 8. **Verify** — build + tests + visible evidence. No "trust me" claims.
> 9. **Promote learnings to memory** — `/si:remember` for facts; direct-edit CODE.md (code rules), CLAUDE.md (workflow), or PROJECT.md (project context) for enforced rules.
> 10. **Delete the plan file** — last step. Code + commit history is the source of truth after.

**Spec (source of truth):** [`docs/superpowers/specs/2026-05-23-livefeed-sse-design.md`](../../docs/superpowers/specs/2026-05-23-livefeed-sse-design.md) rev 3.

**Goal:** Ship LiveFeed SSE end-to-end on top of the master state (JWT live, FluentValidation 12.1.1 + global filter, Repository+Specification pattern, existing exception middleware). Includes promoting `EfRepository<TContext,T>` to a new `Infrastructure` project (PROJECT.md follow-up #4 trigger).

**Architecture:** Singleton `FeedBroadcaster` (per-connection `Channel<>` cap 100 DropOldest) fed by scoped `FeedService` writes (broadcast AFTER `SaveChangesAsync`). `FeedController` reads JWT `sub` claim. CRUD `[Authorize]`/`[Authorize(Roles="Organizer")]`. SSE stream `[AllowAnonymous]` per user direction (`?userId=` query is identity placeholder for stream only).

**Tech Stack (master-aligned):** .NET 10 / C# 13, ASP.NET Core 10 controllers + `[Authorize]`, EF Core 10 + Npgsql 10, FluentValidation **12.1.1** + hand-rolled `FluentValidationFilter` (global), xUnit + FluentAssertions 6.12.2 + Testcontainers.PostgreSql 4.12.0 + `Microsoft.AspNetCore.Mvc.Testing 10.0.8`.

---

## Pre-flight

**Custom-agent review dispatches (advisory, CLAUDE.md §2) for Task 30:**
- `Code Reviewer`, `Security Engineer`, `Backend Architect` — all with directive: *"verify CODE.md compliance against the changed files. Focus: SSE channel discipline (drop-oldest, cap 100, 25s heartbeat), Repository+Specification pattern (not direct DbContext access in FeedService), Infrastructure project bounds (referenced by both Plans + LiveFeed, no cycles), JWT claim reading (no header placeholder), CRUD auth attributes per CODE.md ## Auth, SSE stream intentionally [AllowAnonymous] per user direction, FluentValidation 12.1.1 + global filter, broadcast-after-commit ordering, SemaphoreSlim disposed, PeriodicTimer owned inside heartbeat loop."*

**Packages user must install (CODE.md §6a — `bash-guard.sh` blocks Claude). Run from repo root:**

```bash
# Create Infrastructure project FIRST via dotnet new (Task 2 instructions), then:
dotnet add src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 10.0.*

# LiveFeed gets FluentValidation:
dotnet add src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj package FluentValidation --version 12.1.1

# Test project — created in Task 23, install packages then (mirror Plans.Tests versions):
dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj package coverlet.collector --version 6.0.4
dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj package FluentAssertions --version 6.12.2
dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.8
dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj package Microsoft.NET.Test.Sdk --version 17.14.1
dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj package Testcontainers.PostgreSql --version 4.12.0
dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj package xunit --version 2.9.3
dotnet add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj package xunit.runner.visualstudio --version 3.1.4
```

STOP at any task whose dependency is not yet installed. Ask the user.

---

## Task 1: Update CODE.md + PROJECT.md for Infrastructure promotion

**Spec ref:** §5.1, §15.8.

**Files:**
- Modify: `CODE.md` — `## Persistence layer (Repository + Specification)` — note Infrastructure now exists
- Modify: `PROJECT.md` — strike follow-up #4, update dependency graph, update solution layout

- [ ] **Step 1.1: Update CODE.md `## Persistence layer` section**

Replace the last paragraph of that section (currently starts "Today the EF base lives in `Reshape.ElectricAi.Plans`...") with:

```markdown
- **The EF base lives in `Reshape.ElectricAi.Infrastructure`** (referenced by every feature lib that needs EF persistence). It holds `EfRepository<TContext, T>` + `SpecificationEvaluator`. Each consuming lib provides its own closing class (`PlansRepository<T>`, `FeedRepository<T>`, etc.) and registers it via `services.AddScoped(typeof(IRepository<>), typeof(XxxRepository<>))` inside its module. Plans + LiveFeed currently consume; AiChat + VectorDb will follow the same pattern when they add EF persistence.
```

- [ ] **Step 1.2: Update PROJECT.md solution layout block**

Add to the `src/` tree (alphabetical after `Reshape.ElectricAi.Core/`):
```
│   ├── Reshape.ElectricAi.Infrastructure/      (exists) EfRepository<TContext,T> + SpecificationEvaluator
```

Update dependency graph in PROJECT.md:
```
Presentation     →  Plans, VectorDb, LiveFeed, AiChat, Core, Infrastructure
Plans            →  Core, Infrastructure
LiveFeed         →  Core, Infrastructure, VectorDb
AiChat           →  Core, VectorDb
VectorDb         →  Core
Infrastructure   →  Core
Core             →  (nothing)
```

Replace the "Persistence layer location (interim)" paragraph with: *"Persistence layer lives in `Reshape.ElectricAi.Infrastructure` (referenced by Plans + LiveFeed today)."*

Strike PROJECT.md follow-up #4 — add `~~` strikethrough markers + append `— DONE (Infrastructure promoted alongside LiveFeed initial slice)`.

- [ ] **Step 1.3: Commit docs (no code yet, no build needed)**

```bash
git add CODE.md PROJECT.md
git commit -m "docs: promote EfRepository to Infrastructure project (PROJECT.md follow-up #4)"
```

---

## Task 2: Scaffold `Reshape.ElectricAi.Infrastructure` project

**Spec ref:** §5.1, §15.1.

**Files:**
- Create: `src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj`

- [ ] **Step 2.1: Create the project via dotnet new**

Run from repo root:
```bash
dotnet new classlib -n Reshape.ElectricAi.Infrastructure -o src/Reshape.ElectricAi.Infrastructure --framework net10.0
```

Then **delete** the auto-generated `Class1.cs`:
```bash
rm src/Reshape.ElectricAi.Infrastructure/Class1.cs
```

- [ ] **Step 2.2: Overwrite csproj with project defaults**

Replace contents of `src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

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

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Reshape.ElectricAi.Core\Reshape.ElectricAi.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2.3: Add project to solution**

```bash
dotnet sln ElectricCastle.slnx add src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj
```

- [ ] **Step 2.4: STOP for user to install EF Core package (or verify present)**

User runs (from Pre-flight):
```bash
dotnet add src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 10.0.*
```

May be unnecessary if csproj `<PackageReference>` above resolves on restore. Try `dotnet restore src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj` first; only ask if restore fails.

- [ ] **Step 2.5: Build**

```bash
dotnet build src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj
```
Expected: succeed, 0 warnings.

- [ ] **Step 2.6: Commit (empty project shell — content arrives in Task 3)**

```bash
git add ElectricCastle.slnx src/Reshape.ElectricAi.Infrastructure/
git commit -m "feat(infrastructure): scaffold Reshape.ElectricAi.Infrastructure project (shell)"
```

---

## Task 3: Move `EfRepository` + `SpecificationEvaluator` from Plans to Infrastructure

**Spec ref:** §5.1, §15.1, §15.2.

**Files:**
- Create: `src/Reshape.ElectricAi.Infrastructure/Persistence/EfRepository.cs`
- Create: `src/Reshape.ElectricAi.Infrastructure/Persistence/SpecificationEvaluator.cs`
- Delete: `src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs`
- Delete: `src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs`

- [ ] **Step 3.1: Read existing Plans EfRepository + Spec evaluator content**

(Already inspected during planning — verbatim copy with namespace change.)

- [ ] **Step 3.2: Write `Infrastructure/Persistence/EfRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Persistence;

namespace Reshape.ElectricAi.Infrastructure.Persistence;

public class EfRepository<TContext, T>(TContext context) : IRepository<T>
    where TContext : DbContext
    where T : class
{
    protected TContext Context { get; } = context;
    protected DbSet<T> Set { get; } = context.Set<T>();

    public ValueTask<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        Set.FindAsync([id], cancellationToken);

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        await SpecificationEvaluator.Apply(Set.AsQueryable(), specification).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.ToListAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).CountAsync(cancellationToken);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).AnyAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Context.SaveChangesAsync(cancellationToken);
}
```

- [ ] **Step 3.3: Write `Infrastructure/Persistence/SpecificationEvaluator.cs`**

First read the existing `src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs` to get the verbatim content. Copy it to the new path with namespace `Reshape.ElectricAi.Infrastructure.Persistence`.

- [ ] **Step 3.4: Delete the old Plans copies**

```bash
rm src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs
rm src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs
```

- [ ] **Step 3.5: Build Infrastructure (Plans will still be broken — fixed in Task 4)**

```bash
dotnet build src/Reshape.ElectricAi.Infrastructure/Reshape.ElectricAi.Infrastructure.csproj
```
Expected: succeed.

- [ ] **Step 3.6: Commit (atomic move — Plans still broken at this point but compile-isolated)**

```bash
git add src/Reshape.ElectricAi.Infrastructure/Persistence/ src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs
git commit -m "refactor: move EfRepository + SpecificationEvaluator from Plans to Infrastructure"
```

---

## Task 4: Wire Plans → Infrastructure (csproj ref + PlansRepository using-statement)

**Spec ref:** §5.1, §15.2.

**Files:**
- Modify: `src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj`
- Modify: `src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs`

- [ ] **Step 4.1: Add project ref to Plans.csproj**

In `src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj`, inside the `<ItemGroup>` that has the Core project ref, append:

```xml
<ProjectReference Include="..\Reshape.ElectricAi.Infrastructure\Reshape.ElectricAi.Infrastructure.csproj" />
```

- [ ] **Step 4.2: Update `PlansRepository.cs` using-statement**

Read the current file (one line content). Update to:

```csharp
using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.Plans.Persistence;

public sealed class PlansRepository<T>(PlansDbContext context)
    : EfRepository<PlansDbContext, T>(context)
    where T : class;
```

- [ ] **Step 4.3: Build entire solution to confirm Plans compiles + nothing else broke**

```bash
dotnet build
```
Expected: succeed, 0 warnings.

- [ ] **Step 4.4: Run Plans.Tests to confirm zero regression**

```bash
dotnet test tests/Reshape.ElectricAi.Plans.Tests
```
Expected: **32 tests pass** (matches PROJECT.md baseline).

- [ ] **Step 4.5: Commit**

```bash
git add src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs
git commit -m "refactor(plans): point PlansRepository at Reshape.ElectricAi.Infrastructure"
```

**Verification gate:** if Plans.Tests goes red, STOP and diagnose before proceeding. The Infrastructure move must be transparent to Plans.

---

## Task 5: Core LiveFeed DTOs + enum

**Spec ref:** §4.2, §15.3.

**Files:**
- Create: `src/Reshape.ElectricAi.Core/Enums/FeedEventKind.cs`
- Create: `src/Reshape.ElectricAi.Core/Dtos/UserFeedPrefs.cs`
- Create: `src/Reshape.ElectricAi.Core/Dtos/FeedEntryDto.cs`
- Create: `src/Reshape.ElectricAi.Core/Dtos/FeedEventEnvelope.cs`
- Create: `src/Reshape.ElectricAi.Core/Dtos/PublishFeedEntryCommand.cs`
- Create: `src/Reshape.ElectricAi.Core/Dtos/UpdateFeedEntryCommand.cs`

- [ ] **Step 5.1: Write `Core/Enums/FeedEventKind.cs`**

```csharp
namespace Reshape.ElectricAi.Core.Enums;

public enum FeedEventKind
{
    Created,
    Updated,
    Deleted
}
```

- [ ] **Step 5.2: Write `Core/Dtos/UserFeedPrefs.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record UserFeedPrefs(
    IReadOnlySet<string> Artists,
    IReadOnlySet<MusicGenre> Genres);
```

- [ ] **Step 5.3: Write `Core/Dtos/FeedEntryDto.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record FeedEntryDto(
    Guid Id,
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres,
    DateTime PublishedUtc,
    DateTime? UpdatedUtc);
```

- [ ] **Step 5.4: Write `Core/Dtos/FeedEventEnvelope.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record FeedEventEnvelope(
    FeedEventKind Kind,
    string EventId,
    FeedEntryDto Entry);
```

- [ ] **Step 5.5: Write `Core/Dtos/PublishFeedEntryCommand.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record PublishFeedEntryCommand(
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres);
```

- [ ] **Step 5.6: Write `Core/Dtos/UpdateFeedEntryCommand.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos;

public sealed record UpdateFeedEntryCommand(
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres);
```

- [ ] **Step 5.7: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.Core/Reshape.ElectricAi.Core.csproj
git add src/Reshape.ElectricAi.Core/Enums/FeedEventKind.cs src/Reshape.ElectricAi.Core/Dtos/
git commit -m "feat(core): add LiveFeed DTOs (FeedEntryDto, FeedEventEnvelope, UserFeedPrefs, Commands) and FeedEventKind"
```

---

## Task 6: Core LiveFeed service interfaces

**Spec ref:** §6.1, §6.3, §7.1, §15.3.

**Files:**
- Create: `src/Reshape.ElectricAi.Core/Services/IFeedService.cs`
- Create: `src/Reshape.ElectricAi.Core/Services/IFeedBroadcaster.cs`
- Create: `src/Reshape.ElectricAi.Core/Services/IUserPrefsProvider.cs`

- [ ] **Step 6.1: Write `IFeedService.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Services;

public interface IFeedService
{
    Task<FeedEntryDto> PublishEntryAsync(
        Guid organizerId, PublishFeedEntryCommand command, CancellationToken ct);

    Task<FeedEntryDto> UpdateEntryByIdAsync(
        Guid entryId, UpdateFeedEntryCommand command, CancellationToken ct);

    Task SoftDeleteEntryByIdAsync(Guid entryId, CancellationToken ct);

    Task<FeedEntryDto?> GetEntryByIdAsync(Guid entryId, CancellationToken ct);

    Task<IReadOnlyList<FeedEntryDto>> ListRecentEntriesMatchingPrefsAsync(
        UserFeedPrefs prefs, Category? categoryFilter, int take, CancellationToken ct);

    Task<IReadOnlyList<FeedEntryDto>> ListEntriesSinceEventIdMatchingPrefsAsync(
        string lastEventId, UserFeedPrefs prefs, int take, CancellationToken ct);
}
```

- [ ] **Step 6.2: Write `IFeedBroadcaster.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Services;

public interface IFeedBroadcaster
{
    IAsyncEnumerable<FeedEventEnvelope> SubscribeUserToStreamAsync(
        Guid userId, UserFeedPrefs prefs, string? lastEventId, CancellationToken ct);

    void BroadcastEventToMatchingSubscribers(FeedEventKind kind, FeedEntryDto entry);
}
```

- [ ] **Step 6.3: Write `IUserPrefsProvider.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;

namespace Reshape.ElectricAi.Core.Services;

public interface IUserPrefsProvider
{
    Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct);
}
```

- [ ] **Step 6.4: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.Core/Reshape.ElectricAi.Core.csproj
git add src/Reshape.ElectricAi.Core/Services/IFeedService.cs src/Reshape.ElectricAi.Core/Services/IFeedBroadcaster.cs src/Reshape.ElectricAi.Core/Services/IUserPrefsProvider.cs
git commit -m "feat(core): add IFeedService, IFeedBroadcaster, IUserPrefsProvider interfaces"
```

---

## Task 7: LiveFeed entities

**Spec ref:** §4.1, §15.4.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Entities/FeedEntry.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Entities/FeedEntryArtist.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Entities/FeedEntryGenre.cs`

- [ ] **Step 7.1: Write `FeedEntry.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.LiveFeed.Entities;

public class FeedEntry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public Category PrimaryCategory { get; set; }
    public bool IsGeneral { get; set; }
    public Guid PublishedByUserId { get; set; }
    public DateTime PublishedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public DateTime? DeletedUtc { get; set; }

    public List<FeedEntryArtist> TargetArtists { get; set; } = [];
    public List<FeedEntryGenre> TargetGenres { get; set; } = [];
}
```

- [ ] **Step 7.2: Write `FeedEntryArtist.cs`**

```csharp
namespace Reshape.ElectricAi.LiveFeed.Entities;

public class FeedEntryArtist
{
    public Guid FeedEntryId { get; set; }
    public string ArtistName { get; set; } = "";

    public FeedEntry? FeedEntry { get; set; }
}
```

- [ ] **Step 7.3: Write `FeedEntryGenre.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.LiveFeed.Entities;

public class FeedEntryGenre
{
    public Guid FeedEntryId { get; set; }
    public MusicGenre Genre { get; set; }

    public FeedEntry? FeedEntry { get; set; }
}
```

- [ ] **Step 7.4: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Entities/
git commit -m "feat(livefeed): add FeedEntry, FeedEntryArtist, FeedEntryGenre entities"
```

---

## Task 8: LiveFeed entity configurations + DbContext + factory + repository closing class

**Spec ref:** §5.2, §5.3, §15.4.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/Configurations/FeedEntryConfiguration.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/Configurations/FeedEntryArtistConfiguration.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/Configurations/FeedEntryGenreConfiguration.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/FeedDbContext.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/FeedDbContextFactory.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/FeedRepository.cs`
- Modify: `src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj` (add Infrastructure ref)

- [ ] **Step 8.1: Add Infrastructure ref to LiveFeed csproj**

In `src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj`, inside the `<ItemGroup>` with the Core + VectorDb refs, append:

```xml
<ProjectReference Include="..\Reshape.ElectricAi.Infrastructure\Reshape.ElectricAi.Infrastructure.csproj" />
```

- [ ] **Step 8.2: Write `FeedEntryConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Configurations;

internal sealed class FeedEntryConfiguration : IEntityTypeConfiguration<FeedEntry>
{
    public void Configure(EntityTypeBuilder<FeedEntry> builder)
    {
        builder.ToTable("feed_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Body).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.PrimaryCategory).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.IsGeneral).IsRequired();
        builder.Property(e => e.PublishedByUserId).IsRequired();
        builder.Property(e => e.PublishedUtc).IsRequired();

        builder.HasIndex(e => e.PublishedUtc).IsDescending();
        builder.HasIndex(e => new { e.DeletedUtc, e.PublishedUtc })
               .HasFilter("\"DeletedUtc\" IS NULL");

        builder.HasMany(e => e.TargetArtists)
               .WithOne(a => a.FeedEntry!)
               .HasForeignKey(a => a.FeedEntryId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TargetGenres)
               .WithOne(g => g.FeedEntry!)
               .HasForeignKey(g => g.FeedEntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 8.3: Write `FeedEntryArtistConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Configurations;

internal sealed class FeedEntryArtistConfiguration : IEntityTypeConfiguration<FeedEntryArtist>
{
    public void Configure(EntityTypeBuilder<FeedEntryArtist> builder)
    {
        builder.ToTable("feed_entry_artists");
        builder.HasKey(a => new { a.FeedEntryId, a.ArtistName });
        builder.Property(a => a.ArtistName).HasMaxLength(100);
    }
}
```

- [ ] **Step 8.4: Write `FeedEntryGenreConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Configurations;

internal sealed class FeedEntryGenreConfiguration : IEntityTypeConfiguration<FeedEntryGenre>
{
    public void Configure(EntityTypeBuilder<FeedEntryGenre> builder)
    {
        builder.ToTable("feed_entry_genres");
        builder.HasKey(g => new { g.FeedEntryId, g.Genre });
        builder.Property(g => g.Genre).HasConversion<string>().HasMaxLength(32);
    }
}
```

- [ ] **Step 8.5: Write `FeedDbContext.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence;

public class FeedDbContext(DbContextOptions<FeedDbContext> options) : DbContext(options)
{
    public DbSet<FeedEntry> FeedEntries => Set<FeedEntry>();
    public DbSet<FeedEntryArtist> FeedEntryArtists => Set<FeedEntryArtist>();
    public DbSet<FeedEntryGenre> FeedEntryGenres => Set<FeedEntryGenre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("feed");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeedDbContext).Assembly);
    }
}
```

- [ ] **Step 8.6: Write `FeedDbContextFactory.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reshape.ElectricAi.LiveFeed.Persistence;

public class FeedDbContextFactory : IDesignTimeDbContextFactory<FeedDbContext>
{
    public FeedDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RESHAPE_FEED_CONNECTION")
            ?? "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<FeedDbContext>()
            .UseNpgsql(connection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "feed"))
            .Options;

        return new FeedDbContext(options);
    }
}
```

- [ ] **Step 8.7: Write `FeedRepository.cs`**

```csharp
using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Persistence;

public sealed class FeedRepository<T>(FeedDbContext context)
    : EfRepository<FeedDbContext, T>(context)
    where T : class;
```

- [ ] **Step 8.8: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/
git commit -m "feat(livefeed): add FeedDbContext, entity configs, FeedRepository closing class"
```

---

## Task 9: LiveFeed migration `feed_initial`

**Spec ref:** §5.5.

- [ ] **Step 9.1: Generate migration**

```bash
dotnet ef migrations add feed_initial \
  -p src/Reshape.ElectricAi.LiveFeed \
  -s src/Reshape.ElectricAi.Presentation \
  -- --context FeedDbContext
```

- [ ] **Step 9.2: Inspect partial-index DDL**

Open generated `*_feed_initial.cs`. Find `CreateIndex` for `(DeletedUtc, PublishedUtc)`. Expected: `.Filter = "\"DeletedUtc\" IS NULL"` present. If missing, append manual SQL in `Up()`:
```csharp
migrationBuilder.Sql(
    "CREATE INDEX IF NOT EXISTS \"IX_feed_entries_DeletedUtc_PublishedUtc_partial\" " +
    "ON feed.feed_entries (\"DeletedUtc\", \"PublishedUtc\" DESC) " +
    "WHERE \"DeletedUtc\" IS NULL;");
```
Mirror `DROP INDEX IF EXISTS ...` in `Down()`.

- [ ] **Step 9.3: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Migrations/
git commit -m "feat(livefeed): add feed_initial migration (schema feed, three tables, partial index)"
```

---

## Task 10: `FeedEventId` helper

**Spec ref:** §4.3.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedEventId.cs`

- [ ] **Step 10.1: Write `FeedEventId.cs`**

```csharp
using System.Globalization;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal static class FeedEventId
{
    public static string FormatForEntry(Guid entryId, DateTime publishedUtc)
    {
        // ISO-8601 round-trip + Guid hex. Tie-breaker semantics documented in spec §4.3
        // (Postgres uuid native byte order at read time via the SQL cursor predicate).
        return $"{publishedUtc.ToString("O", CultureInfo.InvariantCulture)}-{entryId:D}";
    }

    public static bool TryParseEntryIdFromEventId(
        string? eventId, out Guid entryId, out DateTime publishedUtc)
    {
        entryId = default;
        publishedUtc = default;
        if (string.IsNullOrWhiteSpace(eventId)) return false;
        if (eventId.Length < 37) return false;

        var guidStart = eventId.Length - 36;
        var separatorAt = guidStart - 1;
        if (eventId[separatorAt] != '-') return false;

        var tsPart = eventId[..separatorAt];
        var guidPart = eventId[guidStart..];

        if (!Guid.TryParseExact(guidPart, "D", out entryId)) return false;
        if (!DateTime.TryParse(tsPart, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out publishedUtc)) return false;

        return true;
    }
}
```

- [ ] **Step 10.2: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedEventId.cs
git commit -m "feat(livefeed): add FeedEventId format + parse helpers"
```

---

## Task 11: `FeedTargeting` predicate

**Spec ref:** §4.4.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedTargeting.cs`

- [ ] **Step 11.1: Write `FeedTargeting.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal static class FeedTargeting
{
    public static bool EntryMatchesUserPrefs(FeedEntryDto entry, UserFeedPrefs prefs)
    {
        if (entry.IsGeneral) return true;

        for (var i = 0; i < entry.TargetArtists.Count; i++)
            if (prefs.Artists.Contains(entry.TargetArtists[i])) return true;

        for (var i = 0; i < entry.TargetGenres.Count; i++)
            if (prefs.Genres.Contains(entry.TargetGenres[i])) return true;

        return false;
    }
}
```

- [ ] **Step 11.2: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedTargeting.cs
git commit -m "feat(livefeed): add FeedTargeting predicate (IsGeneral OR artist OR genre intersect)"
```

---

## Task 12: LiveFeed specifications

**Spec ref:** §5.4.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/Specifications/RecentFeedEntriesSpec.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/Specifications/FeedEntriesSinceCursorSpec.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Persistence/Specifications/FeedEntryByIdSpec.cs`

- [ ] **Step 12.1: Write `RecentFeedEntriesSpec.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

public sealed class RecentFeedEntriesSpec : Specification<FeedEntry>
{
    public RecentFeedEntriesSpec(Category? categoryFilter, int take)
    {
        if (categoryFilter is { } cat)
            Where(e => e.DeletedUtc == null && e.PrimaryCategory == cat);
        else
            Where(e => e.DeletedUtc == null);

        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        ApplyOrderByDescending(e => e.PublishedUtc);
        ApplyPaging(0, take);
        EnableNoTracking();
        EnableSplitQuery();
    }
}
```

- [ ] **Step 12.2: Write `FeedEntriesSinceCursorSpec.cs`**

```csharp
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

public sealed class FeedEntriesSinceCursorSpec : Specification<FeedEntry>
{
    public FeedEntriesSinceCursorSpec(DateTime cursorPublishedUtc, Guid cursorEntryId, int take)
    {
        Where(e => e.DeletedUtc == null
                && (e.PublishedUtc > cursorPublishedUtc
                    || (e.PublishedUtc == cursorPublishedUtc && e.Id > cursorEntryId)));

        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        ApplyOrderBy(e => e.PublishedUtc);
        ApplyPaging(0, take);
        EnableNoTracking();
        EnableSplitQuery();
    }
}
```

- [ ] **Step 12.3: Write `FeedEntryByIdSpec.cs`**

```csharp
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

public sealed class FeedEntryByIdSpec : Specification<FeedEntry>
{
    public FeedEntryByIdSpec(Guid entryId)
    {
        Where(e => e.Id == entryId);
        AddInclude(e => e.TargetArtists);
        AddInclude(e => e.TargetGenres);
        EnableSplitQuery();
    }
}
```

- [ ] **Step 12.4: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Persistence/Specifications/
git commit -m "feat(livefeed): add Recent/SinceCursor/ById specifications"
```

---

## Task 13: `FeedSubscription` + `FeedBroadcaster`

**Spec ref:** §7.2.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedSubscription.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedBroadcaster.cs`

- [ ] **Step 13.1: Write `FeedSubscription.cs`**

```csharp
using System.Threading.Channels;
using Reshape.ElectricAi.Core.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal sealed record FeedSubscription(
    Guid SubscriptionId,
    Guid UserId,
    UserFeedPrefs Prefs,
    Channel<FeedEventEnvelope> Channel);
```

- [ ] **Step 13.2: Write `FeedBroadcaster.cs`**

```csharp
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal sealed class FeedBroadcaster(IServiceScopeFactory scopeFactory) : IFeedBroadcaster
{
    private readonly ConcurrentDictionary<Guid, FeedSubscription> _subs = new();

    public async IAsyncEnumerable<FeedEventEnvelope> SubscribeUserToStreamAsync(
        Guid userId,
        UserFeedPrefs prefs,
        string? lastEventId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var sub = CreateSubscriptionForUser(userId, prefs);
        RegisterSubscription(sub);
        try
        {
            IReadOnlyList<FeedEntryDto> replay;
            using (var scope = scopeFactory.CreateScope())
            {
                var feed = scope.ServiceProvider.GetRequiredService<IFeedService>();
                replay = lastEventId is not null
                    ? await feed.ListEntriesSinceEventIdMatchingPrefsAsync(lastEventId, prefs, 10, ct)
                    : await feed.ListRecentEntriesMatchingPrefsAsync(prefs, null, 10, ct);
            }

            foreach (var entry in replay)
            {
                yield return new FeedEventEnvelope(
                    FeedEventKind.Created,
                    FeedEventId.FormatForEntry(entry.Id, entry.PublishedUtc),
                    entry);
            }

            await foreach (var env in sub.Channel.Reader.ReadAllAsync(ct))
                yield return env;
        }
        finally
        {
            RemoveSubscriptionById(sub.SubscriptionId);
            sub.Channel.Writer.TryComplete();
        }
    }

    public void BroadcastEventToMatchingSubscribers(FeedEventKind kind, FeedEntryDto entry)
    {
        var env = new FeedEventEnvelope(
            kind,
            FeedEventId.FormatForEntry(entry.Id, entry.PublishedUtc),
            entry);

        foreach (var sub in _subs.Values)
        {
            if (FeedTargeting.EntryMatchesUserPrefs(entry, sub.Prefs))
                sub.Channel.Writer.TryWrite(env);
        }
    }

    private void RegisterSubscription(FeedSubscription sub) => _subs[sub.SubscriptionId] = sub;
    private void RemoveSubscriptionById(Guid id) => _subs.TryRemove(id, out _);

    private static FeedSubscription CreateSubscriptionForUser(Guid userId, UserFeedPrefs prefs) =>
        new(
            Guid.NewGuid(),
            userId,
            prefs,
            Channel.CreateBounded<FeedEventEnvelope>(
                new BoundedChannelOptions(100)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                }));
}
```

- [ ] **Step 13.3: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedSubscription.cs src/Reshape.ElectricAi.LiveFeed/Broadcasting/FeedBroadcaster.cs
git commit -m "feat(livefeed): add FeedBroadcaster singleton + FeedSubscription record"
```

---

## Task 14: LiveFeed request DTOs + mapping

**Spec ref:** §4.2, §15.4.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Dtos/PublishFeedEntryRequest.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Dtos/UpdateFeedEntryRequest.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Dtos/Mapping/FeedEntryMapping.cs`

- [ ] **Step 14.1: Write `PublishFeedEntryRequest.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.LiveFeed.Dtos;

public sealed record PublishFeedEntryRequest(
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres);
```

- [ ] **Step 14.2: Write `UpdateFeedEntryRequest.cs`**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.LiveFeed.Dtos;

public sealed record UpdateFeedEntryRequest(
    string Title,
    string Body,
    Category PrimaryCategory,
    bool IsGeneral,
    IReadOnlyList<string> TargetArtists,
    IReadOnlyList<MusicGenre> TargetGenres);
```

- [ ] **Step 14.3: Write `FeedEntryMapping.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.LiveFeed.Entities;

namespace Reshape.ElectricAi.LiveFeed.Dtos.Mapping;

internal static class FeedEntryMapping
{
    public static FeedEntryDto ToDto(this FeedEntry entity) =>
        new(
            entity.Id,
            entity.Title,
            entity.Body,
            entity.PrimaryCategory,
            entity.IsGeneral,
            entity.TargetArtists.Select(a => a.ArtistName).ToList(),
            entity.TargetGenres.Select(g => g.Genre).ToList(),
            entity.PublishedUtc,
            entity.UpdatedUtc);

    public static PublishFeedEntryCommand ToCommand(this PublishFeedEntryRequest req) =>
        new(req.Title, req.Body, req.PrimaryCategory, req.IsGeneral, req.TargetArtists, req.TargetGenres);

    public static UpdateFeedEntryCommand ToCommand(this UpdateFeedEntryRequest req) =>
        new(req.Title, req.Body, req.PrimaryCategory, req.IsGeneral, req.TargetArtists, req.TargetGenres);

    public static FeedEntry ToNewEntity(this PublishFeedEntryCommand cmd, Guid organizerId)
    {
        var entry = new FeedEntry
        {
            Id = Guid.NewGuid(),
            Title = cmd.Title,
            Body = cmd.Body,
            PrimaryCategory = cmd.PrimaryCategory,
            IsGeneral = cmd.IsGeneral,
            PublishedByUserId = organizerId,
            PublishedUtc = DateTime.UtcNow,
            TargetArtists = cmd.TargetArtists
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(a => new FeedEntryArtist { ArtistName = a })
                .ToList(),
            TargetGenres = cmd.TargetGenres
                .Distinct()
                .Select(g => new FeedEntryGenre { Genre = g })
                .ToList()
        };
        foreach (var a in entry.TargetArtists) a.FeedEntryId = entry.Id;
        foreach (var g in entry.TargetGenres) g.FeedEntryId = entry.Id;
        return entry;
    }

    public static void ApplyUpdateTo(this UpdateFeedEntryCommand cmd, FeedEntry entity)
    {
        entity.Title = cmd.Title;
        entity.Body = cmd.Body;
        entity.PrimaryCategory = cmd.PrimaryCategory;
        entity.IsGeneral = cmd.IsGeneral;
        entity.UpdatedUtc = DateTime.UtcNow;

        entity.TargetArtists.Clear();
        foreach (var a in cmd.TargetArtists.Distinct(StringComparer.OrdinalIgnoreCase))
            entity.TargetArtists.Add(new FeedEntryArtist { FeedEntryId = entity.Id, ArtistName = a });

        entity.TargetGenres.Clear();
        foreach (var g in cmd.TargetGenres.Distinct())
            entity.TargetGenres.Add(new FeedEntryGenre { FeedEntryId = entity.Id, Genre = g });
    }
}
```

- [ ] **Step 14.4: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Dtos/
git commit -m "feat(livefeed): add request DTOs + entity/command mapping helpers"
```

---

## Task 15: `FeedService` (Repository pattern, broadcast-after-commit)

**Spec ref:** §6.2.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Services/FeedService.cs`

- [ ] **Step 15.1: Write `FeedService.cs`**

```csharp
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Broadcasting;
using Reshape.ElectricAi.LiveFeed.Dtos.Mapping;
using Reshape.ElectricAi.LiveFeed.Entities;
using Reshape.ElectricAi.LiveFeed.Persistence.Specifications;

namespace Reshape.ElectricAi.LiveFeed.Services;

internal sealed class FeedService(
    IRepository<FeedEntry> repository,
    IFeedBroadcaster broadcaster) : IFeedService
{
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

    public async Task<FeedEntryDto> UpdateEntryByIdAsync(
        Guid entryId, UpdateFeedEntryCommand command, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId), ct)
            ?? throw new NotFoundException("feed-entry-not-found", $"Feed entry {entryId} not found");

        if (entry.DeletedUtc is not null)
            throw new NotFoundException("feed-entry-not-found", $"Feed entry {entryId} is deleted");

        command.ApplyUpdateTo(entry);
        repository.Update(entry);
        await repository.SaveChangesAsync(ct);

        var dto = entry.ToDto();
        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Updated, dto);
        return dto;
    }

    public async Task SoftDeleteEntryByIdAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId), ct);
        if (entry is null || entry.DeletedUtc is not null)
            return; // idempotent: no-op, no broadcast

        entry.DeletedUtc = DateTime.UtcNow;
        repository.Update(entry);
        await repository.SaveChangesAsync(ct);

        broadcaster.BroadcastEventToMatchingSubscribers(FeedEventKind.Deleted, entry.ToDto());
    }

    public async Task<FeedEntryDto?> GetEntryByIdAsync(Guid entryId, CancellationToken ct)
    {
        var entry = await repository.FirstOrDefaultAsync(new FeedEntryByIdSpec(entryId), ct);
        if (entry is null || entry.DeletedUtc is not null) return null;
        return entry.ToDto();
    }

    public async Task<IReadOnlyList<FeedEntryDto>> ListRecentEntriesMatchingPrefsAsync(
        UserFeedPrefs prefs, Category? categoryFilter, int take, CancellationToken ct)
    {
        var entries = await repository.ListAsync(new RecentFeedEntriesSpec(categoryFilter, take), ct);
        return entries
            .Select(e => e.ToDto())
            .Where(dto => FeedTargeting.EntryMatchesUserPrefs(dto, prefs))
            .ToList();
    }

    public async Task<IReadOnlyList<FeedEntryDto>> ListEntriesSinceEventIdMatchingPrefsAsync(
        string lastEventId, UserFeedPrefs prefs, int take, CancellationToken ct)
    {
        if (!FeedEventId.TryParseEntryIdFromEventId(lastEventId, out var cursorId, out var cursorUtc))
            return await ListRecentEntriesMatchingPrefsAsync(prefs, null, take, ct);

        var entries = await repository.ListAsync(new FeedEntriesSinceCursorSpec(cursorUtc, cursorId, take), ct);
        return entries
            .Select(e => e.ToDto())
            .Where(dto => FeedTargeting.EntryMatchesUserPrefs(dto, prefs))
            .ToList();
    }
}
```

- [ ] **Step 15.2: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Services/FeedService.cs
git commit -m "feat(livefeed): add FeedService using IRepository<FeedEntry> + broadcast-after-commit"
```

---

## Task 16: `EmptyUserPrefsProvider`

**Spec ref:** §6.4.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Services/EmptyUserPrefsProvider.cs`

- [ ] **Step 16.1: Write `EmptyUserPrefsProvider.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Services;

internal sealed class EmptyUserPrefsProvider : IUserPrefsProvider
{
    private static readonly IReadOnlySet<string> _emptyArtists = new HashSet<string>();
    private static readonly IReadOnlySet<MusicGenre> _emptyGenres = new HashSet<MusicGenre>();
    private static readonly UserFeedPrefs _emptyPrefs = new(_emptyArtists, _emptyGenres);

    public Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(_emptyPrefs);
}
```

- [ ] **Step 16.2: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Services/EmptyUserPrefsProvider.cs
git commit -m "feat(livefeed): add EmptyUserPrefsProvider default (Plans overrides later)"
```

---

## Task 17: LiveFeed validators

**Spec ref:** §10.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/Validators/PublishFeedEntryRequestValidator.cs`
- Create: `src/Reshape.ElectricAi.LiveFeed/Validators/UpdateFeedEntryRequestValidator.cs`

- [ ] **Step 17.1: Verify FluentValidation 12.1.1 installed in LiveFeed csproj**

Open `src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj`. Confirm `<PackageReference Include="FluentValidation" Version="12.1.1" />` present (from Pre-flight). If missing, STOP and ask user to install.

- [ ] **Step 17.2: Write `PublishFeedEntryRequestValidator.cs`**

```csharp
using FluentValidation;
using Reshape.ElectricAi.LiveFeed.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Validators;

public sealed class PublishFeedEntryRequestValidator : AbstractValidator<PublishFeedEntryRequest>
{
    public PublishFeedEntryRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Body).NotEmpty().MaximumLength(4000);
        RuleFor(r => r.PrimaryCategory).IsInEnum();

        RuleFor(r => r.TargetArtists)
            .NotNull()
            .Must(list => list.Count <= 25).WithMessage("Too many target artists (max 25)")
            .Must(list => list.All(a => !string.IsNullOrWhiteSpace(a) && a.Length <= 100))
                .WithMessage("Each artist 1..100 chars")
            .Must(list => list.Distinct(StringComparer.OrdinalIgnoreCase).Count() == list.Count)
                .WithMessage("Duplicate artist names");

        RuleFor(r => r.TargetGenres)
            .NotNull()
            .Must(list => list.Count <= 12).WithMessage("Too many target genres (max 12)")
            .Must(list => list.All(g => Enum.IsDefined(g))).WithMessage("Unknown genre value")
            .Must(list => list.Distinct().Count() == list.Count).WithMessage("Duplicate genres");

        RuleFor(r => r)
            .Must(r => r.IsGeneral || r.TargetArtists.Count > 0 || r.TargetGenres.Count > 0)
            .WithErrorCode("no-targeting-and-not-general")
            .WithMessage("Entry must be general or target at least one artist/genre");
    }
}
```

- [ ] **Step 17.3: Write `UpdateFeedEntryRequestValidator.cs`**

Same body as PublishFeedEntryRequestValidator but typed `AbstractValidator<UpdateFeedEntryRequest>`.

```csharp
using FluentValidation;
using Reshape.ElectricAi.LiveFeed.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Validators;

public sealed class UpdateFeedEntryRequestValidator : AbstractValidator<UpdateFeedEntryRequest>
{
    public UpdateFeedEntryRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Body).NotEmpty().MaximumLength(4000);
        RuleFor(r => r.PrimaryCategory).IsInEnum();

        RuleFor(r => r.TargetArtists)
            .NotNull()
            .Must(list => list.Count <= 25)
            .Must(list => list.All(a => !string.IsNullOrWhiteSpace(a) && a.Length <= 100))
            .Must(list => list.Distinct(StringComparer.OrdinalIgnoreCase).Count() == list.Count);

        RuleFor(r => r.TargetGenres)
            .NotNull()
            .Must(list => list.Count <= 12)
            .Must(list => list.All(g => Enum.IsDefined(g)))
            .Must(list => list.Distinct().Count() == list.Count);

        RuleFor(r => r)
            .Must(r => r.IsGeneral || r.TargetArtists.Count > 0 || r.TargetGenres.Count > 0)
            .WithErrorCode("no-targeting-and-not-general")
            .WithMessage("Entry must be general or target at least one artist/genre");
    }
}
```

- [ ] **Step 17.4: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/Validators/
git commit -m "feat(livefeed): add Publish/Update FeedEntryRequest validators"
```

---

## Task 18: `LiveFeedModule` DI entry-point

**Spec ref:** §11.1.

**Files:**
- Create: `src/Reshape.ElectricAi.LiveFeed/LiveFeedModule.cs`

- [ ] **Step 18.1: Write `LiveFeedModule.cs`**

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Broadcasting;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.LiveFeed.Services;

namespace Reshape.ElectricAi.LiveFeed;

public static class LiveFeedModule
{
    public static IServiceCollection AddLiveFeedModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<FeedDbContext>(opts =>
            opts.UseNpgsql(connectionString, n =>
                n.MigrationsHistoryTable("__EFMigrationsHistory", "feed")));

        services.AddScoped(typeof(IRepository<>), typeof(FeedRepository<>));

        services.AddScoped<IFeedService, FeedService>();
        services.AddSingleton<IFeedBroadcaster, FeedBroadcaster>();
        services.TryAddScoped<IUserPrefsProvider, EmptyUserPrefsProvider>();

        RegisterValidators(services);

        return services;
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        var validatorInterface = typeof(IValidator<>);
        var registrations = typeof(LiveFeedModule).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsClass: true })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => new { Service = i, Implementation = t }));

        foreach (var r in registrations)
            services.TryAddScoped(r.Service, r.Implementation);
    }
}
```

- [ ] **Step 18.2: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git add src/Reshape.ElectricAi.LiveFeed/LiveFeedModule.cs
git commit -m "feat(livefeed): add LiveFeedModule DI entry-point (FeedRepository, validators reflection scan)"
```

**Note:** if `Microsoft.Extensions.DependencyInjection.Extensions` is not transitively present in LiveFeed, build will fail with `TryAddScoped` not found. Plans.csproj already has it transitively; LiveFeed's EF Core packages should too. If missing: STOP and ask user.

---

## Task 19: `FeedController` (CRUD actions with auth attributes)

**Spec ref:** §8, §9.1, §9.3.

**Files:**
- Create: `src/Reshape.ElectricAi.Presentation/Controllers/FeedController.cs` (CRUD only; stream in Task 20)
- Modify: `src/Reshape.ElectricAi.Presentation/Reshape.ElectricAi.Presentation.csproj` — add LiveFeed project ref if not present

- [ ] **Step 19.1: Verify Presentation has LiveFeed project ref**

Check `src/Reshape.ElectricAi.Presentation/Reshape.ElectricAi.Presentation.csproj`. If `<ProjectReference Include="..\Reshape.ElectricAi.LiveFeed\..."/>` absent, add it. (Master likely already has it from initial scaffold; verify.)

- [ ] **Step 19.2: Write controller with CRUD only**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reshape.ElectricAi.Core.Domain.Exceptions;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Dtos.Mapping;

namespace Reshape.ElectricAi.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FeedController(
    IFeedService feed,
    IFeedBroadcaster broadcaster,
    IUserPrefsProvider prefsProvider) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<FeedEntryDto>>> ListRecentEntriesForCurrentUserAsync(
        [FromQuery] Category? category, CancellationToken ct)
    {
        var userId = GetCurrentUserId(User);
        var prefs = await prefsProvider.GetPrefsByUserIdAsync(userId, ct);
        var entries = await feed.ListRecentEntriesMatchingPrefsAsync(prefs, category, 100, ct);
        return Ok(entries);
    }

    [HttpPost]
    [Authorize(Roles = "Organizer")]
    public async Task<ActionResult<FeedEntryDto>> PublishEntryAsOrganizerAsync(
        [FromBody] PublishFeedEntryRequest request, CancellationToken ct)
    {
        var organizerId = GetCurrentUserId(User);
        var dto = await feed.PublishEntryAsync(organizerId, request.ToCommand(), ct);
        return CreatedAtAction(nameof(ListRecentEntriesForCurrentUserAsync), new { }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    public async Task<ActionResult<FeedEntryDto>> UpdateEntryByIdAsOrganizerAsync(
        [FromRoute] Guid id, [FromBody] UpdateFeedEntryRequest request, CancellationToken ct)
    {
        _ = GetCurrentUserId(User);
        var dto = await feed.UpdateEntryByIdAsync(id, request.ToCommand(), ct);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Organizer")]
    public async Task<IActionResult> SoftDeleteEntryByIdAsOrganizerAsync(
        [FromRoute] Guid id, CancellationToken ct)
    {
        _ = GetCurrentUserId(User);
        await feed.SoftDeleteEntryByIdAsync(id, ct);
        return NoContent();
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedException("missing-sub-claim", "Subject claim missing or invalid");
    }
}
```

- [ ] **Step 19.3: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.Presentation/Reshape.ElectricAi.Presentation.csproj
git add src/Reshape.ElectricAi.Presentation/Controllers/FeedController.cs src/Reshape.ElectricAi.Presentation/Reshape.ElectricAi.Presentation.csproj
git commit -m "feat(presentation): add FeedController CRUD with auth attributes and JWT sub claim reading"
```

---

## Task 20: `FeedController.StreamFeedToCurrentUserAsync` + SSE helpers

**Spec ref:** §9.1 stream, §9.2.

**Files:**
- Modify: `src/Reshape.ElectricAi.Presentation/Controllers/FeedController.cs` (append stream action + helpers)

- [ ] **Step 20.1: Append below `SoftDeleteEntryByIdAsOrganizerAsync`**

Edit the file. Add new `using` lines at top:
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Reshape.ElectricAi.Core.Dtos;   // (already there)
```

Add new action + helpers + `_jsonOpts` field:

```csharp
    [HttpGet("stream")]
    [AllowAnonymous]
    [Produces("text/event-stream")]
    public async Task StreamFeedToCurrentUserAsync(
        [FromQuery] Guid? userId, CancellationToken ct)
    {
        WriteSseResponseHeaders();

        var effectiveUserId = userId ?? Guid.Empty;
        var prefs = effectiveUserId == Guid.Empty
            ? new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>())
            : await prefsProvider.GetPrefsByUserIdAsync(effectiveUserId, ct);

        var lastEventId = Request.Headers["Last-Event-ID"].FirstOrDefault();

        using var writeLock = new SemaphoreSlim(1, 1);
        var heartbeatTask = RunHeartbeatLoopAsync(writeLock, ct);
        try
        {
            await foreach (var env in broadcaster.SubscribeUserToStreamAsync(effectiveUserId, prefs, lastEventId, ct))
                await WriteSseEventFrameAsync(env, writeLock, ct);
        }
        finally
        {
            try { await heartbeatTask; }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (IOException) { }
        }
    }

    private void WriteSseResponseHeaders()
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";
    }

    private async Task WriteSseEventFrameAsync(
        FeedEventEnvelope env, SemaphoreSlim writeLock, CancellationToken ct)
    {
        await writeLock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(env.Entry, _jsonOpts);
            var kindWire = env.Kind switch
            {
                FeedEventKind.Created => "created",
                FeedEventKind.Updated => "updated",
                FeedEventKind.Deleted => "deleted",
                _ => throw new InvalidOperationException("Unknown FeedEventKind")
            };
            await Response.WriteAsync($"event: feed.{kindWire}\n", ct);
            await Response.WriteAsync($"id: {env.EventId}\n", ct);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        finally { writeLock.Release(); }
    }

    private async Task RunHeartbeatLoopAsync(SemaphoreSlim writeLock, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(25));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await writeLock.WaitAsync(ct);
                try
                {
                    await Response.WriteAsync(": keepalive\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                }
                finally { writeLock.Release(); }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (IOException) { }
    }

    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
```

`AllowAnonymous` must be in `Microsoft.AspNetCore.Authorization` namespace (already imported by `[Authorize]`).

- [ ] **Step 20.2: Build + commit**

```bash
dotnet build src/Reshape.ElectricAi.Presentation/Reshape.ElectricAi.Presentation.csproj
git add src/Reshape.ElectricAi.Presentation/Controllers/FeedController.cs
git commit -m "feat(presentation): add SSE stream action ([AllowAnonymous]) with SemaphoreSlim-serialized heartbeat"
```

---

## Task 21: `Program.cs` wiring

**Spec ref:** §11.2.

**Files:**
- Modify: `src/Reshape.ElectricAi.Presentation/Program.cs`

- [ ] **Step 21.1: Add `LiveFeed` using + module registration**

Open `src/Reshape.ElectricAi.Presentation/Program.cs`. Add at top of usings:
```csharp
using Reshape.ElectricAi.LiveFeed;
using Reshape.ElectricAi.LiveFeed.Persistence;
```

Find the `builder.Services.AddPlansModule(builder.Configuration);` line. Append directly after:
```csharp
builder.Services.AddLiveFeedModule(builder.Configuration);
```

- [ ] **Step 21.2: Extend Development startup migration block**

Find the existing scope/migrate block (inside `if (app.Environment.IsDevelopment())`):
```csharp
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
await db.Database.MigrateAsync();
```

Replace with:
```csharp
using (var scope = app.Services.CreateScope())
{
    var plansDb = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
    await plansDb.Database.MigrateAsync();

    var feedDb = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
    await feedDb.Database.MigrateAsync();
}
```

(The original `using var` is converted to `using (...)` block to scope-control the multi-context migration cleanly.)

- [ ] **Step 21.3: Build full solution**

```bash
dotnet build
```
Expected: succeed, 0 warnings.

- [ ] **Step 21.4: Commit**

```bash
git add src/Reshape.ElectricAi.Presentation/Program.cs
git commit -m "feat(presentation): wire LiveFeedModule and extend Development migration block for FeedDbContext"
```

---

## Task 22: Tests project scaffolding (LiveFeed.Tests)

**Spec ref:** §13.1.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj`

- [ ] **Step 22.1: Scaffold test project**

```bash
dotnet new xunit -n Reshape.ElectricAi.LiveFeed.Tests -o tests/Reshape.ElectricAi.LiveFeed.Tests --framework net10.0
rm tests/Reshape.ElectricAi.LiveFeed.Tests/UnitTest1.cs
```

- [ ] **Step 22.2: Overwrite csproj to mirror Plans.Tests**

Replace contents of `tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>CS1591;CA1707;CA1515;CA2007;CA1812;CA1711;CA1001;CA1819;CA1062;CA1024;CA1822</WarningsNotAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="FluentAssertions" />
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Reshape.ElectricAi.Core\Reshape.ElectricAi.Core.csproj" />
    <ProjectReference Include="..\..\src\Reshape.ElectricAi.Infrastructure\Reshape.ElectricAi.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Reshape.ElectricAi.LiveFeed\Reshape.ElectricAi.LiveFeed.csproj" />
    <ProjectReference Include="..\..\src\Reshape.ElectricAi.Plans\Reshape.ElectricAi.Plans.csproj" />
    <ProjectReference Include="..\..\src\Reshape.ElectricAi.Presentation\Reshape.ElectricAi.Presentation.csproj" />
  </ItemGroup>

  <!-- Packages installed by user per Pre-flight section. -->
</Project>
```

(`Plans` ref is for `ITokenService` access in `FeedApiFactory`.)

- [ ] **Step 22.3: Add to solution**

```bash
dotnet sln ElectricCastle.slnx add tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj
```

- [ ] **Step 22.4: STOP for user to install test packages (Pre-flight)**

Confirm install succeeded. Then:

```bash
dotnet build tests/Reshape.ElectricAi.LiveFeed.Tests/Reshape.ElectricAi.LiveFeed.Tests.csproj
```

- [ ] **Step 22.5: Make LiveFeed internals visible to tests**

Add to `src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj` (new `<ItemGroup>`):

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Reshape.ElectricAi.LiveFeed.Tests" />
</ItemGroup>
```

Rebuild LiveFeed:
```bash
dotnet build src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
```

- [ ] **Step 22.6: Commit**

```bash
git add ElectricCastle.slnx tests/Reshape.ElectricAi.LiveFeed.Tests/ src/Reshape.ElectricAi.LiveFeed/Reshape.ElectricAi.LiveFeed.csproj
git commit -m "test(livefeed): scaffold Reshape.ElectricAi.LiveFeed.Tests project (mirrors Plans.Tests versions)"
```

---

## Task 23: Unit tests — targeting, EventId, broadcaster

**Spec ref:** §13.3.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Unit/FeedTargetingTests.cs`
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Unit/FeedEventIdTests.cs`
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Unit/FeedBroadcasterTests.cs`
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Unit/RecordingScopeFactory.cs`

- [ ] **Step 23.1: Write `FeedTargetingTests.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Broadcasting;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

public class FeedTargetingTests
{
    private static FeedEntryDto Entry(bool isGeneral, string[] artists, MusicGenre[] genres) =>
        new(Guid.NewGuid(), "t", "b", Category.General, isGeneral, artists, genres, DateTime.UtcNow, null);

    private static UserFeedPrefs Prefs(string[] artists, MusicGenre[] genres) =>
        new(new HashSet<string>(artists), new HashSet<MusicGenre>(genres));

    [Fact]
    public void EntryMatchesUserPrefs_WhenIsGeneralTrue_ReturnsTrueForAnyUser() =>
        FeedTargeting.EntryMatchesUserPrefs(Entry(true, [], []), Prefs([], [])).Should().BeTrue();

    [Fact]
    public void EntryMatchesUserPrefs_WhenArtistOverlapsUserPrefs_ReturnsTrue() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, ["Justin Timberlake"], []),
            Prefs(["Justin Timberlake", "Yungblud"], [])).Should().BeTrue();

    [Fact]
    public void EntryMatchesUserPrefs_WhenGenreOverlapsUserPrefs_ReturnsTrue() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, [], [MusicGenre.Techno]),
            Prefs([], [MusicGenre.Techno, MusicGenre.House])).Should().BeTrue();

    [Fact]
    public void EntryMatchesUserPrefs_WhenNoOverlapAndNotGeneral_ReturnsFalse() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, ["Other"], [MusicGenre.Folk]),
            Prefs(["Justin Timberlake"], [MusicGenre.Techno])).Should().BeFalse();

    [Fact]
    public void EntryMatchesUserPrefs_WhenArtistMatchIsCaseSensitive_DocumentsBehavior() =>
        FeedTargeting.EntryMatchesUserPrefs(
            Entry(false, ["justin timberlake"], []),
            Prefs(["Justin Timberlake"], [])).Should().BeFalse();
}
```

- [ ] **Step 23.2: Write `FeedEventIdTests.cs`**

```csharp
using Reshape.ElectricAi.LiveFeed.Broadcasting;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

public class FeedEventIdTests
{
    [Fact]
    public void FormatForEntry_ProducesIso8601WithGuidSuffix()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var utc = new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc);
        var s = FeedEventId.FormatForEntry(id, utc);
        s.Should().StartWith("2026-05-23T10:00:00");
        s.Should().EndWith("-00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public void TryParseEntryIdFromEventId_RoundTripsCleanly()
    {
        var id = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        var s = FeedEventId.FormatForEntry(id, utc);
        FeedEventId.TryParseEntryIdFromEventId(s, out var parsedId, out var parsedUtc).Should().BeTrue();
        parsedId.Should().Be(id);
        parsedUtc.Should().BeCloseTo(utc, TimeSpan.FromMilliseconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("garbage")]
    [InlineData("2026-05-23T10:00:00Z-not-a-guid")]
    [InlineData("not-a-date-00000000-0000-0000-0000-000000000001")]
    public void TryParseEntryIdFromEventId_WhenInputIsMalformed_ReturnsFalse(string? input) =>
        FeedEventId.TryParseEntryIdFromEventId(input, out _, out _).Should().BeFalse();
}
```

- [ ] **Step 23.3: Write `RecordingScopeFactory.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

internal sealed class RecordingScopeFactory : IServiceScopeFactory
{
    public int ScopeCreatedCount { get; private set; }
    public IReadOnlyList<FeedEntryDto> ReplayResult { get; set; } = Array.Empty<FeedEntryDto>();

    public IServiceScope CreateScope()
    {
        ScopeCreatedCount++;
        return new RecordingScope(this);
    }

    private sealed class RecordingScope(RecordingScopeFactory parent) : IServiceScope, IServiceProvider
    {
        public IServiceProvider ServiceProvider => this;
        public void Dispose() { }
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IFeedService))
                return new StubFeedService(parent.ReplayResult);
            return null;
        }
    }

    private sealed class StubFeedService(IReadOnlyList<FeedEntryDto> result) : IFeedService
    {
        public Task<FeedEntryDto> PublishEntryAsync(Guid o, PublishFeedEntryCommand c, CancellationToken ct) => throw new NotSupportedException();
        public Task<FeedEntryDto> UpdateEntryByIdAsync(Guid id, UpdateFeedEntryCommand c, CancellationToken ct) => throw new NotSupportedException();
        public Task SoftDeleteEntryByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<FeedEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<FeedEntryDto>> ListRecentEntriesMatchingPrefsAsync(UserFeedPrefs p, Category? c, int take, CancellationToken ct) => Task.FromResult(result);
        public Task<IReadOnlyList<FeedEntryDto>> ListEntriesSinceEventIdMatchingPrefsAsync(string lastId, UserFeedPrefs p, int take, CancellationToken ct) => Task.FromResult(result);
    }
}
```

- [ ] **Step 23.4: Write `FeedBroadcasterTests.cs`**

```csharp
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Broadcasting;

namespace Reshape.ElectricAi.LiveFeed.Tests.Unit;

public class FeedBroadcasterTests
{
    private static UserFeedPrefs Prefs(string[] a) => new(new HashSet<string>(a), new HashSet<MusicGenre>());
    private static FeedEntryDto Entry(bool general, string[] artists) =>
        new(Guid.NewGuid(), "t", "b", Category.General, general, artists, [], DateTime.UtcNow, null);

    [Fact]
    public async Task BroadcastEventToMatchingSubscribers_WhenSubscriberMatches_WritesEnvelopeToChannel()
    {
        var bc = new FeedBroadcaster(new RecordingScopeFactory());
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var received = new List<FeedEventEnvelope>();
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs(["JT"]), null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(150, cts.Token);
        bc.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, Entry(false, ["JT"]));
        await Task.Delay(250, cts.Token);
        cts.Cancel();
        await consume;

        received.Should().Contain(e => e.Kind == FeedEventKind.Created);
    }

    [Fact]
    public async Task BroadcastEventToMatchingSubscribers_WhenSubscriberDoesNotMatch_DoesNotWrite()
    {
        var bc = new FeedBroadcaster(new RecordingScopeFactory());
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var received = new List<FeedEventEnvelope>();
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs(["JT"]), null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(150, cts.Token);
        bc.BroadcastEventToMatchingSubscribers(FeedEventKind.Created, Entry(false, ["Other"]));
        await Task.Delay(250, cts.Token);
        cts.Cancel();
        await consume;

        received.Should().BeEmpty();
    }

    [Fact]
    public async Task SubscribeUserToStreamAsync_WhenLastEventIdNullAndNoHistory_YieldsZeroReplayEntries()
    {
        var factory = new RecordingScopeFactory { ReplayResult = Array.Empty<FeedEntryDto>() };
        var bc = new FeedBroadcaster(factory);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));

        var received = new List<FeedEventEnvelope>();
        try
        {
            await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs([]), null, cts.Token))
                received.Add(env);
        }
        catch (OperationCanceledException) { }

        received.Should().BeEmpty();
        factory.ScopeCreatedCount.Should().Be(1);
    }

    [Fact]
    public async Task SubscribeUserToStreamAsync_OnReplay_ResolvesFreshIFeedServiceScopePerCall()
    {
        var factory = new RecordingScopeFactory { ReplayResult = Array.Empty<FeedEntryDto>() };
        var bc = new FeedBroadcaster(factory);
        using var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        async Task Drain(CancellationToken ct)
        {
            try
            {
                await foreach (var _ in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), Prefs([]), null, ct)) { }
            }
            catch (OperationCanceledException) { }
        }

        await Task.WhenAll(Drain(cts1.Token), Drain(cts2.Token));
        factory.ScopeCreatedCount.Should().Be(2);
    }
}
```

- [ ] **Step 23.5: Run unit tests + commit**

```bash
dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests --filter "FullyQualifiedName~Unit"
```
Expected: all green.

```bash
git add tests/Reshape.ElectricAi.LiveFeed.Tests/Unit/
git commit -m "test(livefeed): add unit tests for FeedTargeting, FeedEventId, FeedBroadcaster"
```

---

## Task 24: Integration fixtures (Postgres + WebFactory + FakeUserPrefsProvider)

**Spec ref:** §13.2, §13.5.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/PostgresFixture.cs`
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/PostgresCollection.cs`
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FakeUserPrefsProvider.cs`
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/FeedApiFactory.cs`

- [ ] **Step 24.1: Read existing Plans.Tests fixtures for pattern alignment**

Read these files (no code changes):
- `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/AuthApiFactory.cs`
- `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/PostgresCollection.cs`
- `tests/Reshape.ElectricAi.Plans.Tests/Integration/Fixtures/PostgresFixture.cs`

Mirror their structure. In particular, `AuthApiFactory` shows how the Plans devs override config + ConfigureTestServices + how they get JWT issuance.

- [ ] **Step 24.2: Write `PostgresFixture.cs`** (mirror Plans verbatim if compatible)

```csharp
using Testcontainers.PostgreSql;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("electric_ai_test_feed")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();
    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
```

- [ ] **Step 24.3: Write `PostgresCollection.cs`**

```csharp
namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
```

- [ ] **Step 24.4: Write `FakeUserPrefsProvider.cs`**

```csharp
using System.Collections.Concurrent;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class FakeUserPrefsProvider : IUserPrefsProvider
{
    private readonly ConcurrentDictionary<Guid, UserFeedPrefs> _map = new();

    public void Set(Guid userId, string[] artists, MusicGenre[] genres) =>
        _map[userId] = new UserFeedPrefs(new HashSet<string>(artists), new HashSet<MusicGenre>(genres));

    public Task<UserFeedPrefs> GetPrefsByUserIdAsync(Guid userId, CancellationToken ct)
    {
        if (_map.TryGetValue(userId, out var p)) return Task.FromResult(p);
        return Task.FromResult(new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>()));
    }
}
```

- [ ] **Step 24.5: Write `FeedApiFactory.cs`**

This is the big fixture — mirror `AuthApiFactory.cs` pattern. Needs to:
1. Override `Postgres` connection string with `PostgresFixture.ConnectionString`.
2. Replace `EmptyUserPrefsProvider` with `FakeUserPrefsProvider` (single instance, exposed as property for test setup).
3. Provide `CreateClientForUser(Guid userId, params string[] roles)` that uses the host's `ITokenService` to mint real JWTs.
4. Provide `ResetDatabaseAsync()` to drop + recreate `feed` schema between tests.

```csharp
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Persistence;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

public sealed class FeedApiFactory(string postgresConnection)
    : WebApplicationFactory<Program>
{
    public FakeUserPrefsProvider FakePrefs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = postgresConnection,
                // Plans auth options need a signing key in this env too:
                ["Auth:JwtSigningKey"] = "VGVzdGluZ1NpZ25pbmdLZXkzMmJ5dGVzbWluaW11bUFCQ0RFRkdISUprbA==", // 48-byte base64
                ["Auth:Issuer"] = "reshape-electric-ai",
                ["Auth:Audience"] = "reshape-electric-ai-api",
                ["Auth:AccessTokenMinutes"] = "15",
                ["Auth:RefreshTokenDays"] = "7"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace EmptyUserPrefsProvider with our fake.
            var descriptor = services.Single(d => d.ServiceType == typeof(IUserPrefsProvider));
            services.Remove(descriptor);
            services.AddScoped<IUserPrefsProvider>(_ => FakePrefs);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public HttpClient CreateClientForUser(Guid userId, string email = "tester@example.com", params string[] roles)
    {
        var client = CreateClient();
        using var scope = Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var role = roles.FirstOrDefault() ?? "User";
        // ITokenService API surface — confirm signature matches Plans implementation.
        // Plans' TokenService likely exposes IssueAccessToken(Guid userId, string email, string role) or similar.
        var token = tokenService.IssueAccessToken(userId, email, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    public HttpClient CreateAnonymousClient() => CreateClient();
}
```

**Stop and verify `ITokenService.IssueAccessToken` signature** in `src/Reshape.ElectricAi.Plans/Services/TokenService.cs` before committing. If the actual signature differs, adjust this helper accordingly. (Likely method names: `IssueAccessToken`, `CreateAccessToken`, or `Generate`.)

- [ ] **Step 24.6: Build + commit**

```bash
dotnet build tests/Reshape.ElectricAi.LiveFeed.Tests
git add tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Fixtures/
git commit -m "test(livefeed): add Postgres + FeedApiFactory + FakeUserPrefsProvider integration fixtures"
```

---

## Task 25: Integration CRUD tests

**Spec ref:** §13.4 `FeedCrudTests`.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedCrudTests.cs`

- [ ] **Step 25.1: Write the test class**

```csharp
using System.Net;
using System.Net.Http.Json;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection("postgres")]
public class FeedCrudTests(PostgresFixture pg) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;
    private readonly Guid _organizer = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    public async ValueTask InitializeAsync()
    {
        _factory = new FeedApiFactory(pg.ConnectionString);
        await _factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private HttpClient OrganizerClient() => _factory.CreateClientForUser(_organizer, roles: new[] { "Organizer" });
    private HttpClient UserClient() => _factory.CreateClientForUser(_user, roles: new[] { "User" });

    [Fact]
    public async Task PublishEntryAsOrganizer_WhenAuthenticatedAsOrganizer_Returns201AndDtoMatchingInput()
    {
        var resp = await OrganizerClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Rain", "Light shower 21:00", Category.Weather, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<FeedEntryDto>();
        dto!.Title.Should().Be("Rain");
        dto.IsGeneral.Should().BeTrue();
    }

    [Fact]
    public async Task PublishEntryAsOrganizer_WhenAuthenticatedAsUser_Returns403Envelope()
    {
        var resp = await UserClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("x", "y", Category.General, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PublishEntryAsOrganizer_WhenAnonymous_Returns401Envelope()
    {
        var resp = await _factory.CreateAnonymousClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("x", "y", Category.General, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListRecentEntries_WhenAuthenticated_ReturnsOrderedByPublishedDescending()
    {
        var org = OrganizerClient();
        for (var i = 0; i < 3; i++)
        {
            await org.PostAsJsonAsync("/api/v1/feed",
                new PublishFeedEntryRequest($"E{i}", "b", Category.General, true, [], []));
            await Task.Delay(20);
        }

        var resp = await UserClient().GetAsync("/api/v1/feed");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await resp.Content.ReadFromJsonAsync<List<FeedEntryDto>>();
        list!.Should().BeInDescendingOrder(e => e.PublishedUtc);
    }

    [Fact]
    public async Task ListRecentEntries_WhenAnonymous_Returns401Envelope() =>
        (await _factory.CreateAnonymousClient().GetAsync("/api/v1/feed")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

    [Fact]
    public async Task SoftDeleteEntryById_WhenEntryExistsAsOrganizer_RemovesFromList()
    {
        var org = OrganizerClient();
        var publish = await org.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Doomed", "b", Category.General, true, [], []));
        var dto = await publish.Content.ReadFromJsonAsync<FeedEntryDto>();

        var del = await org.DeleteAsync($"/api/v1/feed/{dto!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await UserClient().GetFromJsonAsync<List<FeedEntryDto>>("/api/v1/feed");
        list!.Any(e => e.Id == dto.Id).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEntryById_WhenEntryMissing_Returns404Envelope()
    {
        var resp = await OrganizerClient().PutAsJsonAsync($"/api/v1/feed/{Guid.NewGuid()}",
            new UpdateFeedEntryRequest("x", "y", Category.General, true, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("feed-entry-not-found");
    }

    [Fact]
    public async Task PublishEntry_WhenNotGeneralAndNoTargeting_Returns400ValidationEnvelope()
    {
        var resp = await OrganizerClient().PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("t", "b", Category.General, false, [], []));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Step 25.2: Run + commit**

```bash
dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests --filter "FullyQualifiedName~FeedCrud"
```
Expected: all green (Docker required).

```bash
git add tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedCrudTests.cs
git commit -m "test(livefeed): add FeedCrudTests (auth gates, role gates, validation, soft delete)"
```

---

## Task 26: Integration SSE tests (full wire-level coverage)

**Spec ref:** §13.4 `FeedSseTests`.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedSseTests.cs`

- [ ] **Step 26.1: Write SSE test class with helper**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.LiveFeed.Dtos;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection("postgres")]
public class FeedSseTests(PostgresFixture pg) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        _factory = new FeedApiFactory(pg.ConnectionString);
        await _factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private HttpClient OrganizerClient(Guid id) => _factory.CreateClientForUser(id, roles: new[] { "Organizer" });
    private HttpClient AnonClient() => _factory.CreateAnonymousClient();

    private static async Task<string> ReadStreamForAsync(
        HttpClient client, string url, CancellationToken ct, int maxBytes = 8192)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[maxBytes];
        var read = 0;
        try
        {
            while (read < maxBytes)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read, maxBytes - read), ct);
                if (n == 0) break;
                read += n;
            }
        }
        catch (OperationCanceledException) { }
        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    [Fact]
    public async Task StreamFeed_WhenOrganizerPublishesMatchingEntry_ClientReceivesCreatedFrame()
    {
        var user = Guid.NewGuid();
        _factory.FakePrefs.Set(user, ["Justin Timberlake"], []);

        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listenTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", listenCts.Token);

        await Task.Delay(300, listenCts.Token);
        await OrganizerClient(Guid.NewGuid()).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Delay", "30 min", Category.Music, false, ["Justin Timberlake"], []));

        await Task.Delay(800, listenCts.Token);
        listenCts.Cancel();
        var raw = await listenTask;
        raw.Should().Contain("event: feed.created");
        raw.Should().Contain("\"title\":\"Delay\"");
    }

    [Fact]
    public async Task StreamFeed_WhenOrganizerPublishesUnmatchedEntry_ClientReceivesNoFrameWithinOneSecond()
    {
        var user = Guid.NewGuid();
        _factory.FakePrefs.Set(user, ["Yungblud"], []);

        using var listenCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
        var listenTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", listenCts.Token);

        await Task.Delay(200);
        await OrganizerClient(Guid.NewGuid()).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Other", "x", Category.Music, false, ["Justin Timberlake"], []));

        var raw = await listenTask;
        raw.Should().NotContain("event: feed.created");
    }

    [Fact]
    public async Task StreamFeed_WhenIdleFor26Seconds_ClientReceivesKeepaliveComment()
    {
        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(28));
        var raw = await ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={Guid.NewGuid()}", listenCts.Token);
        raw.Should().Contain(": keepalive");
    }

    [Fact]
    public async Task StreamFeed_WhenLastEventIdHeaderPresent_ReplaysOnlyEntriesSinceCursor()
    {
        var organizer = Guid.NewGuid();
        var user = Guid.NewGuid();
        // publish two entries
        var first = await OrganizerClient(organizer).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("First", "b", Category.General, true, [], []));
        var firstDto = await first.Content.ReadFromJsonAsync<Core.Dtos.FeedEntryDto>();
        await Task.Delay(50);
        await OrganizerClient(organizer).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Second", "b", Category.General, true, [], []));

        var cursor = $"{firstDto!.PublishedUtc:O}-{firstDto.Id:D}";

        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/feed/stream?userId={user}");
        req.Headers.TryAddWithoutValidation("Last-Event-ID", cursor);
        using var resp = await AnonClient().SendAsync(req, HttpCompletionOption.ResponseHeadersRead, listenCts.Token);

        await using var stream = await resp.Content.ReadAsStreamAsync(listenCts.Token);
        var buf = new byte[8192];
        var n = 0;
        try
        {
            while (n < 4096)
            {
                var read = await stream.ReadAsync(buf.AsMemory(n, buf.Length - n), listenCts.Token);
                if (read == 0) break;
                n += read;
                if (Encoding.UTF8.GetString(buf, 0, n).Contains("Second")) break;
            }
        }
        catch (OperationCanceledException) { }

        var raw = Encoding.UTF8.GetString(buf, 0, n);
        raw.Should().Contain("\"title\":\"Second\"");
        raw.Should().NotContain("\"title\":\"First\"");
    }

    [Fact]
    public async Task StreamFeed_WhenLastEventIdHeaderMalformed_FallsThroughToRecentBatch()
    {
        var organizer = Guid.NewGuid();
        var user = Guid.NewGuid();
        await OrganizerClient(organizer).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Visible", "b", Category.General, true, [], []));

        using var listenCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/feed/stream?userId={user}");
        req.Headers.TryAddWithoutValidation("Last-Event-ID", "definitely-not-a-valid-cursor");
        using var resp = await AnonClient().SendAsync(req, HttpCompletionOption.ResponseHeadersRead, listenCts.Token);

        await using var stream = await resp.Content.ReadAsStreamAsync(listenCts.Token);
        var buf = new byte[4096];
        var read = 0;
        try { read = await stream.ReadAsync(buf.AsMemory(0, buf.Length), listenCts.Token); }
        catch (OperationCanceledException) { }
        var raw = Encoding.UTF8.GetString(buf, 0, read);
        raw.Should().Contain("\"title\":\"Visible\"");
    }

    [Fact]
    public async Task StreamFeed_WhenTwoUsersConnectedAndEntryTargetsOnlyOne_OnlyMatchingUserReceivesFrame()
    {
        var matchUser = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        _factory.FakePrefs.Set(matchUser, ["JT"], []);
        _factory.FakePrefs.Set(otherUser, [], []);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var matchTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={matchUser}", cts.Token);
        var otherTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={otherUser}", cts.Token);

        await Task.Delay(400);
        await OrganizerClient(Guid.NewGuid()).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Target", "x", Category.Music, false, ["JT"], []));

        await Task.Delay(800);
        cts.Cancel();
        var matchRaw = await matchTask;
        var otherRaw = await otherTask;
        matchRaw.Should().Contain("event: feed.created");
        otherRaw.Should().NotContain("event: feed.created");
    }

    [Fact]
    public async Task StreamFeed_WhenHeartbeatAndEventInterleave_ProducesNoCorruptFrame()
    {
        var user = Guid.NewGuid();
        _factory.FakePrefs.Set(user, [], []);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        var listenTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", cts.Token, maxBytes: 16384);

        await Task.Delay(300);
        var org = OrganizerClient(Guid.NewGuid());
        // 20 publishes in tight loop
        for (var i = 0; i < 20; i++)
        {
            await org.PostAsJsonAsync("/api/v1/feed",
                new PublishFeedEntryRequest($"Burst{i}", "b", Category.General, true, [], []));
        }

        await Task.Delay(800);
        cts.Cancel();
        var raw = await listenTask;

        // Every frame ends with \n\n; lines start with "event:", "id:", "data:", or ":".
        foreach (var frame in raw.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var first = frame.TrimStart().Split('\n')[0];
            (first.StartsWith("event:") || first.StartsWith("id:") || first.StartsWith("data:") || first.StartsWith(":"))
                .Should().BeTrue($"Frame starts with unexpected line: {first}");
        }
    }

    [Fact]
    public async Task StreamFeed_WhenConnected_ResponseHeadersAreSseCompliant()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/feed/stream?userId={Guid.NewGuid()}");
        using var resp = await AnonClient().SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
        resp.Headers.CacheControl!.NoCache.Should().BeTrue();
        resp.Headers.Connection.Should().Contain("keep-alive");
    }

    [Fact]
    public async Task StreamFeed_WhenAnonymousAndUserIdQuerySet_TargetingAppliesToThatUser()
    {
        var user = Guid.NewGuid();
        _factory.FakePrefs.Set(user, ["JT"], []);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var listenTask = ReadStreamForAsync(AnonClient(), $"/api/v1/feed/stream?userId={user}", cts.Token);

        await Task.Delay(300);
        await OrganizerClient(Guid.NewGuid()).PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("ForJT", "x", Category.Music, false, ["JT"], []));

        await Task.Delay(800);
        cts.Cancel();
        var raw = await listenTask;
        raw.Should().Contain("ForJT");
    }

    [Fact]
    public async Task StreamFeed_WhenAnonymousAndNoUserIdQuery_OnlyReceivesGeneralEntries()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var listenTask = ReadStreamForAsync(AnonClient(), "/api/v1/feed/stream", cts.Token);

        await Task.Delay(300);
        var org = OrganizerClient(Guid.NewGuid());
        await org.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Targeted", "x", Category.Music, false, ["JT"], []));
        await org.PostAsJsonAsync("/api/v1/feed",
            new PublishFeedEntryRequest("Everyone", "x", Category.General, true, [], []));

        await Task.Delay(800);
        cts.Cancel();
        var raw = await listenTask;
        raw.Should().NotContain("Targeted");
        raw.Should().Contain("Everyone");
    }
}
```

- [ ] **Step 26.2: Run + commit**

```bash
dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests --filter "FullyQualifiedName~FeedSse"
```
Expected: all green (Docker required; heartbeat test takes ~28s).

```bash
git add tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedSseTests.cs
git commit -m "test(livefeed): add full wire-level SSE integration tests (10 scenarios)"
```

---

## Task 27: Broadcast ordering + repository spec integration tests

**Spec ref:** §13.4 `FeedServiceBroadcastOrderingTests` + `FeedRepositorySpecificationTests`.

**Files:**
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedServiceBroadcastOrderingTests.cs`
- Create: `tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Persistence/FeedRepositorySpecificationTests.cs`

- [ ] **Step 27.1: Write `FeedServiceBroadcastOrderingTests.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Dtos;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Endpoints;

[Collection("postgres")]
public class FeedServiceBroadcastOrderingTests(PostgresFixture pg) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        _factory = new FeedApiFactory(pg.ConnectionString);
        await _factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task PublishEntry_AfterSaveChanges_BroadcastsCreatedEnvelope()
    {
        using var scope = _factory.Services.CreateScope();
        var feed = scope.ServiceProvider.GetRequiredService<IFeedService>();
        var bc = scope.ServiceProvider.GetRequiredService<IFeedBroadcaster>();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();

        var received = new List<FeedEventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var prefs = new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>());
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), prefs, null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(200, cts.Token);
        var dto = await feed.PublishEntryAsync(
            Guid.NewGuid(),
            new PublishFeedEntryCommand("OrderTest", "b", Category.General, true, [], []),
            cts.Token);

        await Task.Delay(300, cts.Token);
        cts.Cancel();
        await consume;

        // 1. Entry persisted in DB
        db.FeedEntries.Any(e => e.Id == dto.Id).Should().BeTrue();
        // 2. Subscriber received the envelope (general → matches everyone)
        received.Should().Contain(e => e.Entry.Id == dto.Id && e.Kind == FeedEventKind.Created);
    }

    [Fact]
    public async Task SoftDeleteEntryById_WhenAlreadyDeleted_DoesNotBroadcastAndDoesNotThrow()
    {
        using var scope = _factory.Services.CreateScope();
        var feed = scope.ServiceProvider.GetRequiredService<IFeedService>();
        var bc = scope.ServiceProvider.GetRequiredService<IFeedBroadcaster>();

        // Set up: publish + delete once
        var dto = await feed.PublishEntryAsync(
            Guid.NewGuid(),
            new PublishFeedEntryCommand("Doomed", "b", Category.General, true, [], []),
            CancellationToken.None);
        await feed.SoftDeleteEntryByIdAsync(dto.Id, CancellationToken.None);

        // Now subscribe + delete again — expect no second Deleted envelope
        var received = new List<FeedEventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var prefs = new UserFeedPrefs(new HashSet<string>(), new HashSet<MusicGenre>());
        var consume = Task.Run(async () =>
        {
            try
            {
                await foreach (var env in bc.SubscribeUserToStreamAsync(Guid.NewGuid(), prefs, null, cts.Token))
                    received.Add(env);
            }
            catch (OperationCanceledException) { }
        });

        await Task.Delay(200, cts.Token);
        var act = async () => await feed.SoftDeleteEntryByIdAsync(dto.Id, CancellationToken.None);
        await act.Should().NotThrowAsync();

        await Task.Delay(200, cts.Token);
        cts.Cancel();
        await consume;

        received.Should().NotContain(e => e.Kind == FeedEventKind.Deleted);
    }
}
```

- [ ] **Step 27.2: Write `FeedRepositorySpecificationTests.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Reshape.ElectricAi.Core.Enums;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.LiveFeed.Entities;
using Reshape.ElectricAi.LiveFeed.Persistence;
using Reshape.ElectricAi.LiveFeed.Persistence.Specifications;
using Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Persistence;

[Collection("postgres")]
public class FeedRepositorySpecificationTests(PostgresFixture pg) : IAsyncLifetime
{
    private FeedApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        _factory = new FeedApiFactory(pg.ConnectionString);
        await _factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private FeedEntry CreateEntry(string title, DateTime utc, bool deleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Body = "b",
            PrimaryCategory = Category.General,
            IsGeneral = true,
            PublishedByUserId = Guid.NewGuid(),
            PublishedUtc = utc,
            DeletedUtc = deleted ? DateTime.UtcNow : null
        };

    [Fact]
    public async Task ListAsync_WithRecentFeedEntriesSpec_ReturnsLatestFirstAndExcludesDeleted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<FeedEntry>>();

        db.FeedEntries.Add(CreateEntry("Older", DateTime.UtcNow.AddMinutes(-5)));
        db.FeedEntries.Add(CreateEntry("Newer", DateTime.UtcNow));
        db.FeedEntries.Add(CreateEntry("Gone", DateTime.UtcNow.AddMinutes(-1), deleted: true));
        await db.SaveChangesAsync();

        var list = await repo.ListAsync(new RecentFeedEntriesSpec(null, 10), CancellationToken.None);
        list.Select(e => e.Title).Should().ContainInOrder("Newer", "Older");
        list.Any(e => e.Title == "Gone").Should().BeFalse();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithFeedEntryByIdSpec_ReturnsEntryWithIncludedTargets()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<FeedEntry>>();

        var entry = CreateEntry("Included", DateTime.UtcNow);
        entry.TargetArtists.Add(new FeedEntryArtist { FeedEntryId = entry.Id, ArtistName = "JT" });
        db.FeedEntries.Add(entry);
        await db.SaveChangesAsync();

        var loaded = await repo.FirstOrDefaultAsync(new FeedEntryByIdSpec(entry.Id), CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.TargetArtists.Should().ContainSingle(a => a.ArtistName == "JT");
    }
}
```

- [ ] **Step 27.3: Run + commit**

```bash
dotnet test tests/Reshape.ElectricAi.LiveFeed.Tests --filter "FullyQualifiedName~Broadcast|FullyQualifiedName~Specification"
```
Expected: all green.

```bash
git add tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Endpoints/FeedServiceBroadcastOrderingTests.cs tests/Reshape.ElectricAi.LiveFeed.Tests/Integration/Persistence/FeedRepositorySpecificationTests.cs
git commit -m "test(livefeed): add broadcast-ordering + repository specification integration tests"
```

---

## Task 28: Final full-solution build + test gate

**Spec ref:** §16.

- [ ] **Step 28.1: Full build clean**

```bash
dotnet build
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 28.2: All tests green (Plans + LiveFeed)**

```bash
dotnet test
```
Expected: 32 Plans tests pass + all LiveFeed tests pass.

- [ ] **Step 28.3: Manual SSE smoke**

Start the API:
```bash
dotnet run --project src/Reshape.ElectricAi.Presentation
```

Get an Organizer JWT (register → login via Plans `/api/v1/auth/`, ensure account has `Role=Organizer` in DB).

Terminal 1 (anonymous SSE with userId query):
```powershell
curl.exe -N "http://localhost:5217/api/v1/feed/stream?userId=00000000-0000-0000-0000-000000000001"
```

Terminal 2 (publish as Organizer):
```powershell
curl.exe -X POST -H "Authorization: Bearer <organizer-token>" `
         -H "Content-Type: application/json" `
         -d '{\"title\":\"Rain\",\"body\":\"Light shower\",\"primaryCategory\":\"Weather\",\"isGeneral\":true,\"targetArtists\":[],\"targetGenres\":[]}' `
         http://localhost:5217/api/v1/feed
```

Expected in Terminal 1: `event: feed.created` frame within 1s; `: keepalive` every 25s.

- [ ] **Step 28.4: Dispatch `Code Reviewer`, `Security Engineer`, `Backend Architect`**

Each with the directive from Pre-flight. Run in parallel (single message, multiple Agent tool calls).

- [ ] **Step 28.5: Fold review findings**

For each finding: BLOCKING → fix now, MAJOR → fix now, MINOR → judge. Rebuild + retest. Commit fixes.

---

## Task 29: Memory promotion + plan deletion (Phases 9 + 10)

- [ ] **Step 29.1: Capture non-obvious learnings via `/si:remember`**

Examples:
- "Infrastructure project hosts EfRepository<TContext,T> + SpecificationEvaluator; each feature lib has its own closing class (PlansRepository, FeedRepository)."
- "FeedBroadcaster singleton uses IServiceScopeFactory to resolve scoped IFeedService for replay-on-connect — prevents captive scoped dependency bug."
- "Broadcast happens AFTER SaveChangesAsync — rollback safety. Tests in FeedServiceBroadcastOrderingTests."
- "SSE stream endpoint intentionally [AllowAnonymous] in v1 (LiveFeed-rev3); ?userId= query is identity placeholder. Future plan adds SseQueryStringTokenMiddleware per CODE.md ## Auth line 184."

- [ ] **Step 29.2: CODE.md + PROJECT.md already updated in Task 1**

Confirm Task 1 commits landed. No additional doc edits needed.

- [ ] **Step 29.3: Delete plan file (Phase 10)**

```bash
rm .claude/plans/livefeed-sse.md
git add -u .claude/plans/livefeed-sse.md
git commit -m "chore: remove livefeed-sse plan (work shipped, code+commits are source of truth)"
```

- [ ] **Step 29.4: Also delete the spec file**

```bash
rm docs/superpowers/specs/2026-05-23-livefeed-sse-design.md
git add -u docs/superpowers/specs/2026-05-23-livefeed-sse-design.md
git commit -m "chore: remove livefeed-sse design spec (shipped)"
```

---

## Self-review checklist (writing-plans skill mandate)

**Spec coverage:** every spec §1–§19 has a task:
- §1, §2 scope → captured in plan goal + pre-flight
- §3 architecture → tasks 2-21 collectively
- §4 domain → tasks 5, 7, 11, 14
- §5 persistence → tasks 2, 3, 4, 8, 9, 12
- §6 service layer → tasks 6, 15, 16
- §7 broadcaster → tasks 10, 13
- §8 identity (JWT claim) → task 19
- §9 controller + SSE → tasks 19, 20
- §10 validation → task 17
- §11 module + DI → tasks 18, 21
- §12 exception envelope → uses existing master middleware; no new task
- §13 tests → tasks 22, 23, 24, 25, 26, 27
- §14 packages → pre-flight
- §15 files → tasks 1-27
- §16 verification → task 28
- §17, §18, §19 → no implementation; tracked via Task 29 memory promotion

**Placeholder scan:** every code step has actual content. No "TBD". No "TODO" except `// TODO(auth):`-equivalent rationale comments are no longer needed (auth is wired). One conditional `STOP and ask user` in Tasks 22.4 and 24.5 — both gates documented with the question + the package/method that must be confirmed.

**Type consistency:** `IFeedService` / `IFeedBroadcaster` / `IUserPrefsProvider` consistently referenced from `Reshape.ElectricAi.Core.Services` (tasks 6, 13, 15, 18, 19, 23, 27). `FeedSubscription` / `FeedTargeting` / `FeedEventId` / `FeedBroadcaster` from `Reshape.ElectricAi.LiveFeed.Broadcasting` (tasks 10, 11, 13). `IRepository<T>` + `ISpecification<T>` from `Reshape.ElectricAi.Core.Persistence` (tasks 12, 15, 27). `FeedRepository<T>` closing class from `Reshape.ElectricAi.LiveFeed.Persistence` (tasks 8, 18). Exceptions from `Reshape.ElectricAi.Core.Domain.Exceptions` (tasks 15, 19). Method names match spec rev 3.

**User constraints honored:**
- ✅ Auth required on CRUD, deferred on SSE stream (`[AllowAnonymous]`).
- ✅ Integration tests cover full SSE wire-level (tasks 26, 27).
- ✅ Plans business logic untouched; only mechanical changes (csproj ref + one using-statement in Task 4).
- ✅ No location targeting anywhere.

---

## Execution handoff

Plan complete. Two execution options:

1. **Inline Execution (recommended, CLAUDE.md §2 compliant)** — execute task-by-task in this session via `superpowers:executing-plans`. Main loop edits, build/test checkpoints between tasks.
2. **Subagent-Driven** — only viable if subagents stay review/scaffold-search only (CLAUDE.md §2 forbids subagent implementation).

After approval, **STOP at Task 2.4** for user to install Infrastructure EF Core package, and again at **Task 22.4** for test packages, and again at **Task 24.5** to verify `ITokenService.IssueAccessToken` signature against the actual Plans implementation.
