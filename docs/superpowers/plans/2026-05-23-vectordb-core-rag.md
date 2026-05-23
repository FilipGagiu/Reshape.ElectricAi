# VectorDb + Core RAG Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add pgvector-backed RAG (document chunks, Q&A, events) with category-filtered KNN search, embedding ingestion via OpenAI, and chunk splitting via tiktoken — wired into the existing repository pattern.

**Architecture:** `EfRepository<TContext,T>` and `SpecificationEvaluator` move to Core so VectorDb can reuse them. `IEmbeddingService` in Core wraps OpenAI `EmbeddingClient` behind a seam for testability. `IngestService` handles chunking + embedding + persistence via `IRepository<T>` and `IEmbeddingService`. `VectorSearchService` injects `VectorDbContext` directly for KNN queries (not expressible as `ISpecification<T>`). Plans module's open-generic `IRepository<>` registration is replaced with per-entity registrations to avoid DI collision.

**Tech Stack:** EF Core 10 + Npgsql.EntityFrameworkCore.PostgreSQL 10 + Pgvector.EntityFrameworkCore 0.3.0, OpenAI SDK 2.10.0, Microsoft.ML.Tokenizers 2.0.0, PostgreSQL 16 (pgvector extension, HNSW indexes, GIN indexes on `text[]`).

---

## REQUIRED: Phase List (non-negotiable, restate verbatim per CLAUDE.md)

1. Invoke task-specific superpowers skill(s) — match the task to a skill from §7.
2. Enter plan mode (`EnterPlanMode`) — before ANY file edit.
3. Inventory / explore — gather facts via Explore agents or direct reads.
4. Design — propose agents for review/exploration (NOT implementation).
5. Write the plan to `.claude/plans/<slug>.md`.
6. `ExitPlanMode` — single approval gate.
7. Execute — YOU edit the files; re-read CODE.md before each code edit.
8. Verify — build + tests + visible evidence.
9. Promote learnings to memory.
10. Delete the plan file.

---

## Prerequisites — Human Actions Required

Before starting **Task 1**, a human must run these commands:

```bash
# Add EF Core to Core (bash-guard blocks this for Claude)
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.* --project src/Reshape.ElectricAi.Core

# Restore all packages (VectorDb.csproj already has its deps declared)
dotnet restore
```

VectorDb.csproj, solution file (`ElectricCastle.slnx`), and Presentation.csproj project references are already wired up.

---

## File Structure

### Core — modified
| Action | File |
|--------|------|
| Create | `src/Reshape.ElectricAi.Core/Persistence/EfRepository.cs` |
| Create | `src/Reshape.ElectricAi.Core/Persistence/SpecificationEvaluator.cs` |
| Delete | `src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs` |
| Delete | `src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs` |
| Modify | `src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs` |
| Modify | `src/Reshape.ElectricAi.Plans/PlansModule.cs` |
| Modify | `src/Reshape.ElectricAi.Core/Domain/ICategorizable.cs` |
| Create | `src/Reshape.ElectricAi.Core/Configuration/ChatOptions.cs` |
| Create | `src/Reshape.ElectricAi.Core/Services/IEmbeddingService.cs` |
| Create | `src/Reshape.ElectricAi.Core/Services/IVectorSearchService.cs` |
| Create | `src/Reshape.ElectricAi.Core/Services/IIngestService.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/DocumentSearchFilter.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/QuestionSearchFilter.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/EventSearchFilter.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/IngestDocumentRequest.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/IngestQARequest.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/IngestAnswerRequest.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/IngestEventRequest.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/RetrievedChunk.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/RetrievedQA.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/RetrievedAnswer.cs` |
| Create | `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/RetrievedEvent.cs` |

### VectorDb — new files
| Action | File |
|--------|------|
| Create | `src/Reshape.ElectricAi.VectorDb/Services/OpenAiEmbeddingService.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/CategoryTagsHelper.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Entities/Document.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Entities/DocumentChunk.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Entities/Question.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Entities/Answer.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Entities/EventEntry.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/DocumentConfiguration.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/DocumentChunkConfiguration.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/QuestionConfiguration.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/AnswerConfiguration.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/EventEntryConfiguration.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/VectorDbContext.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/VectorDbContextFactory.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/VectorRepository.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/DocumentByHashSpec.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/QuestionByHashSpec.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/AnswersByQuestionIdSpec.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/EventEntryByFeedEntryIdSpec.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Services/IngestService.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/Services/VectorSearchService.cs` |
| Create | `src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs` |

### Presentation — modified
| Action | File |
|--------|------|
| Modify | `src/Reshape.ElectricAi.Presentation/Program.cs` |
| Modify | `src/Reshape.ElectricAi.Presentation/appsettings.json` |

---

## Task 1: Move EfRepository + SpecificationEvaluator to Core

**Files:**
- Create: `src/Reshape.ElectricAi.Core/Persistence/EfRepository.cs`
- Create: `src/Reshape.ElectricAi.Core/Persistence/SpecificationEvaluator.cs`
- Delete: `src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs`
- Delete: `src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs`
- Modify: `src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs`

- [ ] **Step 1: Create Core/Persistence/EfRepository.cs**

```csharp
using Microsoft.EntityFrameworkCore;

namespace Reshape.ElectricAi.Core.Persistence;

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

- [ ] **Step 2: Create Core/Persistence/SpecificationEvaluator.cs**

```csharp
using Microsoft.EntityFrameworkCore;

namespace Reshape.ElectricAi.Core.Persistence;

public static class SpecificationEvaluator
{
    public static IQueryable<T> Apply<T>(IQueryable<T> source, ISpecification<T> specification)
        where T : class
    {
        var query = source;

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (specification.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
```

- [ ] **Step 3: Delete Plans/Persistence/EfRepository.cs**

```bash
rm src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs
```

- [ ] **Step 4: Delete Plans/Persistence/SpecificationEvaluator.cs**

```bash
rm src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs
```

- [ ] **Step 5: Update PlansRepository.cs to import Core namespace**

Replace `src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs` with:

```csharp
using Reshape.ElectricAi.Core.Persistence;

namespace Reshape.ElectricAi.Plans.Persistence;

public sealed class PlansRepository<T>(PlansDbContext context) : EfRepository<PlansDbContext, T>(context)
    where T : class;
```

- [ ] **Step 6: Verify build after move**

```bash
dotnet build src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add src/Reshape.ElectricAi.Core/Persistence/EfRepository.cs \
        src/Reshape.ElectricAi.Core/Persistence/SpecificationEvaluator.cs \
        src/Reshape.ElectricAi.Plans/Persistence/PlansRepository.cs
git rm src/Reshape.ElectricAi.Plans/Persistence/EfRepository.cs \
       src/Reshape.ElectricAi.Plans/Persistence/SpecificationEvaluator.cs
git commit -m "refactor: move EfRepository and SpecificationEvaluator to Core"
```

---

## Task 2: Fix PlansModule Open-Generic DI Registration

**Files:**
- Modify: `src/Reshape.ElectricAi.Plans/PlansModule.cs`

The current `services.AddScoped(typeof(IRepository<>), typeof(PlansRepository<>))` is a catch-all that would conflict with VectorDb's per-entity registrations (last registration wins). Replace with explicit per-entity bindings for every entity type managed by PlansDbContext.

- [ ] **Step 1: Replace the open-generic registration in PlansModule.cs**

Find the line:
```csharp
services.AddScoped(typeof(IRepository<>), typeof(PlansRepository<>));
```

Replace it with:
```csharp
services.AddScoped<IRepository<User>, PlansRepository<User>>();
services.AddScoped<IRepository<RefreshToken>, PlansRepository<RefreshToken>>();
services.AddScoped<IRepository<UserPreferences>, PlansRepository<UserPreferences>>();
services.AddScoped<IRepository<Plan>, PlansRepository<Plan>>();
services.AddScoped<IRepository<Group>, PlansRepository<Group>>();
services.AddScoped<IRepository<GroupMember>, PlansRepository<GroupMember>>();
services.AddScoped<IRepository<GroupPreferences>, PlansRepository<GroupPreferences>>();
services.AddScoped<IRepository<UserPreferenceActivity>, PlansRepository<UserPreferenceActivity>>();
services.AddScoped<IRepository<UserPreferenceArtist>, PlansRepository<UserPreferenceArtist>>();
services.AddScoped<IRepository<UserPreferenceFoodRestriction>, PlansRepository<UserPreferenceFoodRestriction>>();
services.AddScoped<IRepository<UserPreferenceGenre>, PlansRepository<UserPreferenceGenre>>();
services.AddScoped<IRepository<GroupPreferenceActivity>, PlansRepository<GroupPreferenceActivity>>();
services.AddScoped<IRepository<GroupPreferenceArtist>, PlansRepository<GroupPreferenceArtist>>();
services.AddScoped<IRepository<GroupPreferenceFoodRestriction>, PlansRepository<GroupPreferenceFoodRestriction>>();
services.AddScoped<IRepository<GroupPreferenceGenre>, PlansRepository<GroupPreferenceGenre>>();
```

The full updated `AddPlansModule` method after the change (add the missing using directives for all entity types at the top of PlansModule.cs):

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Plans.Services;

namespace Reshape.ElectricAi.Plans;

public static class PlansModule
{
    public static IServiceCollection AddPlansModule(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = BuildAuthOptions(configuration);
        ValidateAuthOptions(authOptions);
        services.AddSingleton(authOptions);
        services.AddSingleton<IOptions<AuthOptions>>(Options.Create(authOptions));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<PlansDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "plans")));

        services.AddScoped<IRepository<User>, PlansRepository<User>>();
        services.AddScoped<IRepository<RefreshToken>, PlansRepository<RefreshToken>>();
        services.AddScoped<IRepository<UserPreferences>, PlansRepository<UserPreferences>>();
        services.AddScoped<IRepository<Plan>, PlansRepository<Plan>>();
        services.AddScoped<IRepository<Group>, PlansRepository<Group>>();
        services.AddScoped<IRepository<GroupMember>, PlansRepository<GroupMember>>();
        services.AddScoped<IRepository<GroupPreferences>, PlansRepository<GroupPreferences>>();
        services.AddScoped<IRepository<UserPreferenceActivity>, PlansRepository<UserPreferenceActivity>>();
        services.AddScoped<IRepository<UserPreferenceArtist>, PlansRepository<UserPreferenceArtist>>();
        services.AddScoped<IRepository<UserPreferenceFoodRestriction>, PlansRepository<UserPreferenceFoodRestriction>>();
        services.AddScoped<IRepository<UserPreferenceGenre>, PlansRepository<UserPreferenceGenre>>();
        services.AddScoped<IRepository<GroupPreferenceActivity>, PlansRepository<GroupPreferenceActivity>>();
        services.AddScoped<IRepository<GroupPreferenceArtist>, PlansRepository<GroupPreferenceArtist>>();
        services.AddScoped<IRepository<GroupPreferenceFoodRestriction>, PlansRepository<GroupPreferenceFoodRestriction>>();
        services.AddScoped<IRepository<GroupPreferenceGenre>, PlansRepository<GroupPreferenceGenre>>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IAuthService, AuthService>();

        RegisterValidators(services);

        return services;
    }

    // ... rest unchanged
}
```

- [ ] **Step 2: Build Plans to verify no compile errors**

```bash
dotnet build src/Reshape.ElectricAi.Plans/Reshape.ElectricAi.Plans.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Reshape.ElectricAi.Plans/PlansModule.cs
git commit -m "fix: replace open-generic IRepository<> with per-entity Plans registrations"
```

---

## Task 3: Update Core — ICategorizable, ChatOptions, Interfaces, DTOs

**Files:**
- Modify: `src/Reshape.ElectricAi.Core/Domain/ICategorizable.cs`
- Create: `src/Reshape.ElectricAi.Core/Configuration/ChatOptions.cs`
- Create: `src/Reshape.ElectricAi.Core/Services/IVectorSearchService.cs`
- Create: `src/Reshape.ElectricAi.Core/Services/IIngestService.cs`
- Create: `src/Reshape.ElectricAi.Core/Dtos/VectorSearch/*.cs` (11 files)

- [ ] **Step 1: Update ICategorizable.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Domain;

public interface ICategorizable
{
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues { get; }
}
```

- [ ] **Step 2: Create ChatOptions.cs**

```csharp
namespace Reshape.ElectricAi.Core.Configuration;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; init; } = 1536;
}
```

- [ ] **Step 3: Create IEmbeddingService.cs**

`EmbeddingClient` is a sealed class — not mockable. This interface is the seam for integration tests.

```csharp
namespace Reshape.ElectricAi.Core.Services;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3b: Create IVectorSearchService.cs**

```csharp
using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.Core.Services;

public interface IVectorSearchService
{
    Task<IReadOnlyList<RetrievedChunk>> SearchDocumentsAsync(DocumentSearchFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievedQA>> SearchQuestionsAsync(QuestionSearchFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievedEvent>> SearchEventsAsync(EventSearchFilter filter, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Create IIngestService.cs**

```csharp
using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.Core.Services;

public interface IIngestService
{
    Task IngestDocumentAsync(IngestDocumentRequest request, CancellationToken cancellationToken = default);
    Task IngestQAAsync(IngestQARequest request, CancellationToken cancellationToken = default);
    Task IngestEventAsync(IngestEventRequest request, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Create DocumentSearchFilter.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record DocumentSearchFilter(
    string QueryText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 5);
```

- [ ] **Step 6: Create QuestionSearchFilter.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record QuestionSearchFilter(
    string QueryText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 5);
```

- [ ] **Step 7: Create EventSearchFilter.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record EventSearchFilter(
    string QueryText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 5);
```

- [ ] **Step 8: Create IngestDocumentRequest.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestDocumentRequest(
    string Title,
    string Content,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? CategoryValues = null);
```

- [ ] **Step 9: Create IngestAnswerRequest.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestAnswerRequest(
    string AnswerText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? CategoryValues = null);
```

- [ ] **Step 10: Create IngestQARequest.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestQARequest(
    string QuestionText,
    IReadOnlyList<IngestAnswerRequest> Answers,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? QuestionCategoryValues = null);
```

- [ ] **Step 11: Create IngestEventRequest.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record IngestEventRequest(
    Guid FeedEntryId,
    string Title,
    string TextRepresentation,
    DateTime EventUtc,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? CategoryValues = null);
```

- [ ] **Step 12: Create RetrievedChunk.cs**

```csharp
namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedChunk(
    Guid DocumentId,
    string DocumentTitle,
    string Content,
    float Score);
```

- [ ] **Step 13: Create RetrievedAnswer.cs**

```csharp
namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedAnswer(
    string AnswerText,
    float Score);
```

- [ ] **Step 14: Create RetrievedQA.cs**

```csharp
namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedQA(
    string QuestionText,
    IReadOnlyList<RetrievedAnswer> Answers,
    float QuestionScore);
```

- [ ] **Step 15: Create RetrievedEvent.cs**

```csharp
namespace Reshape.ElectricAi.Core.Dtos.VectorSearch;

public sealed record RetrievedEvent(
    Guid FeedEntryId,
    string Title,
    string TextRepresentation,
    DateTime EventUtc,
    float Score);
```

- [ ] **Step 16: Build Core**

```bash
dotnet build src/Reshape.ElectricAi.Core/Reshape.ElectricAi.Core.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 17: Commit**

```bash
git add src/Reshape.ElectricAi.Core/
git commit -m "feat: add ChatOptions, ICategorizable redesign, VectorSearch interfaces and DTOs to Core"
```

---

## Task 4: VectorDb — CategoryTagsHelper + Entities

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/CategoryTagsHelper.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Entities/Document.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Entities/DocumentChunk.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Entities/Question.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Entities/Answer.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Entities/EventEntry.cs`

Category tags are stored as `text[]` in the format `"Category.Value"` (e.g. `"Transport.EcBus"`, `"Accommodation.Camping"`). An empty array means "general — applies to all users". Filtering uses PostgreSQL `&&` overlap operator, which is efficiently indexed by GIN.

- [ ] **Step 1: Create CategoryTagsHelper.cs**

```csharp
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.VectorDb;

internal static class CategoryTagsHelper
{
    internal static string[] ToTags(IReadOnlyDictionary<Category, IReadOnlyList<string>> categoryValues) =>
        categoryValues
            .SelectMany(kvp => kvp.Value.Select(v => $"{kvp.Key}.{v}"))
            .ToArray();

    internal static IReadOnlyDictionary<Category, IReadOnlyList<string>> FromTags(string[] tags)
    {
        var result = new Dictionary<Category, List<string>>();
        foreach (var tag in tags)
        {
            var dot = tag.IndexOf('.');
            if (dot < 0) continue;
            if (!Enum.TryParse<Category>(tag[..dot], out var category)) continue;
            if (!result.TryGetValue(category, out var list))
            {
                list = [];
                result[category] = list;
            }
            list.Add(tag[(dot + 1)..]);
        }
        return result.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
    }
}
```

- [ ] **Step 2: Create Entities/Document.cs**

```csharp
namespace Reshape.ElectricAi.VectorDb.Entities;

public class Document
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string SourceHash { get; set; }
    public DateTime IngestedUtc { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
```

- [ ] **Step 3: Create Entities/DocumentChunk.cs**

```csharp
using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public required string Content { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public int ChunkIndex { get; set; }
    public Document Document { get; set; } = null!;
}
```

- [ ] **Step 4: Create Entities/Question.cs**

```csharp
using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class Question
{
    public Guid Id { get; set; }
    public required string Text { get; set; }
    public required string TextHash { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public DateTime IngestedUtc { get; set; }
    public ICollection<Answer> Answers { get; set; } = [];
}
```

- [ ] **Step 5: Create Entities/Answer.cs**

```csharp
using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class Answer
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public required string Text { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public DateTime IngestedUtc { get; set; }
    public Question Question { get; set; } = null!;
}
```

- [ ] **Step 6: Create Entities/EventEntry.cs**

```csharp
using Pgvector;

namespace Reshape.ElectricAi.VectorDb.Entities;

public class EventEntry
{
    public Guid Id { get; set; }
    public Guid FeedEntryId { get; set; }
    public required string Title { get; set; }
    public required string TextRepresentation { get; set; }
    public required Vector Embedding { get; set; }
    public string[] CategoryTags { get; set; } = [];
    public DateTime EventUtc { get; set; }
    public DateTime IngestedUtc { get; set; }
}
```

- [ ] **Step 7: Build VectorDb**

```bash
dotnet build src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/
git commit -m "feat: add CategoryTagsHelper and VectorDb entities"
```

---

## Task 5: EF Configurations + VectorDbContext + Factory + Repository

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/DocumentConfiguration.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/DocumentChunkConfiguration.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/QuestionConfiguration.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/AnswerConfiguration.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Configurations/EventEntryConfiguration.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/VectorDbContext.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/VectorDbContextFactory.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/VectorRepository.cs`

Notes:
- HNSW index uses `vector_cosine_ops` (matches `<=>` cosine distance operator used in search)
- GIN index on `category_tags` enables `&&` overlap queries
- `VectorDbContext` injects `ChatOptions` to supply embedding dimensions to `HasColumnType`
- `VectorDbContextFactory` (design-time only) uses default 1536 dimensions

- [ ] **Step 1: Create Configurations/DocumentConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.SourceHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(d => d.SourceHash)
            .IsUnique();

        builder.Property(d => d.IngestedUtc)
            .IsRequired();

        builder.HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Create Configurations/DocumentChunkConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .IsRequired();

        builder.Property(c => c.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(c => c.CategoryTags)
            .HasMethod("gin");

        builder.Property(c => c.ChunkIndex)
            .IsRequired();

        // Embedding column type is applied in VectorDbContext.OnModelCreating (config-driven dimensions)
    }
}
```

- [ ] **Step 3: Create Configurations/QuestionConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text)
            .IsRequired();

        builder.Property(q => q.TextHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(q => q.TextHash)
            .IsUnique();

        builder.Property(q => q.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(q => q.CategoryTags)
            .HasMethod("gin");

        builder.Property(q => q.IngestedUtc)
            .IsRequired();

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Embedding column type applied in VectorDbContext.OnModelCreating
    }
}
```

- [ ] **Step 4: Create Configurations/AnswerConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answers");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Text)
            .IsRequired();

        builder.Property(a => a.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(a => a.CategoryTags)
            .HasMethod("gin");

        builder.Property(a => a.IngestedUtc)
            .IsRequired();

        // Embedding column type applied in VectorDbContext.OnModelCreating
    }
}
```

- [ ] **Step 5: Create Configurations/EventEntryConfiguration.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Configurations;

internal sealed class EventEntryConfiguration : IEntityTypeConfiguration<EventEntry>
{
    public void Configure(EntityTypeBuilder<EventEntry> builder)
    {
        builder.ToTable("event_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.TextRepresentation)
            .IsRequired();

        builder.HasIndex(e => e.FeedEntryId)
            .IsUnique();

        builder.Property(e => e.CategoryTags)
            .HasColumnType("text[]");

        builder.HasIndex(e => e.CategoryTags)
            .HasMethod("gin");

        builder.Property(e => e.EventUtc)
            .IsRequired();

        builder.Property(e => e.IngestedUtc)
            .IsRequired();

        // Embedding column type applied in VectorDbContext.OnModelCreating
    }
}
```

- [ ] **Step 6: Create Persistence/VectorDbContext.cs**

The `ChatOptions` injection supplies the embedding dimensions so the `vector(N)` column type matches what the OpenAI model returns.

```csharp
using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence;

public class VectorDbContext(DbContextOptions<VectorDbContext> options, ChatOptions chatOptions) : DbContext(options)
{
    private readonly int _dimensions = chatOptions.EmbeddingDimensions;

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<EventEntry> EventEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("vector");
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VectorDbContext).Assembly);

        var vectorType = $"vector({_dimensions})";
        modelBuilder.Entity<DocumentChunk>().Property(c => c.Embedding).HasColumnType(vectorType);
        modelBuilder.Entity<Question>().Property(q => q.Embedding).HasColumnType(vectorType);
        modelBuilder.Entity<Answer>().Property(a => a.Embedding).HasColumnType(vectorType);
        modelBuilder.Entity<EventEntry>().Property(e => e.Embedding).HasColumnType(vectorType);

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        modelBuilder.Entity<Question>()
            .HasIndex(q => q.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        modelBuilder.Entity<Answer>()
            .HasIndex(a => a.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        modelBuilder.Entity<EventEntry>()
            .HasIndex(e => e.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
```

- [ ] **Step 7: Create Persistence/VectorDbContextFactory.cs**

Design-time factory used by `dotnet ef migrations`. Uses default 1536 dimensions because ChatOptions is not available at design time.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Reshape.ElectricAi.Core.Configuration;

namespace Reshape.ElectricAi.VectorDb.Persistence;

public class VectorDbContextFactory : IDesignTimeDbContextFactory<VectorDbContext>
{
    public VectorDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("RESHAPE_VECTOR_CONNECTION")
            ?? "Host=localhost;Database=electric_ai;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseNpgsql(connection, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "vector");
                npgsql.UseVector();
            })
            .Options;

        return new VectorDbContext(options, new ChatOptions());
    }
}
```

- [ ] **Step 8: Create Persistence/VectorRepository.cs**

```csharp
using Reshape.ElectricAi.Core.Persistence;

namespace Reshape.ElectricAi.VectorDb.Persistence;

public sealed class VectorRepository<T>(VectorDbContext context) : EfRepository<VectorDbContext, T>(context)
    where T : class;
```

- [ ] **Step 9: Build VectorDb**

```bash
dotnet build src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 10: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/Persistence/
git commit -m "feat: add VectorDbContext, configurations, factory, and VectorRepository"
```

---

## Task 6: Specification Classes

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/DocumentByHashSpec.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/QuestionByHashSpec.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/AnswersByQuestionIdSpec.cs`
- Create: `src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/EventEntryByFeedEntryIdSpec.cs`

- [ ] **Step 1: Create Specifications/DocumentByHashSpec.cs**

```csharp
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class DocumentByHashSpec : Specification<Document>
{
    public DocumentByHashSpec(string sourceHash)
    {
        Where(d => d.SourceHash == sourceHash);
    }
}
```

- [ ] **Step 2: Create Specifications/QuestionByHashSpec.cs**

```csharp
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class QuestionByHashSpec : Specification<Question>
{
    public QuestionByHashSpec(string textHash)
    {
        Where(q => q.TextHash == textHash);
    }
}
```

- [ ] **Step 3: Create Specifications/AnswersByQuestionIdSpec.cs**

```csharp
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class AnswersByQuestionIdSpec : Specification<Answer>
{
    public AnswersByQuestionIdSpec(Guid questionId)
    {
        Where(a => a.QuestionId == questionId);
        EnableNoTracking();
    }
}
```

- [ ] **Step 4: Create Specifications/EventEntryByFeedEntryIdSpec.cs**

```csharp
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class EventEntryByFeedEntryIdSpec : Specification<EventEntry>
{
    public EventEntryByFeedEntryIdSpec(Guid feedEntryId)
    {
        Where(e => e.FeedEntryId == feedEntryId);
    }
}
```

- [ ] **Step 5: Build**

```bash
dotnet build src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/Persistence/Specifications/
git commit -m "feat: add VectorDb specification classes"
```

---

## Task 7: IngestService

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/Services/IngestService.cs`

`IngestService` handles all three ingestion flows:
- **Documents**: SHA-256 idempotency guard → tiktoken chunking (512 tokens, 50 overlap) → batch embed all chunks → persist
- **Q&A**: SHA-256 idempotency guard on question text → embed question + each answer individually → persist
- **Events**: FeedEntryId idempotency guard → embed `TextRepresentation` → persist

- [ ] **Step 1: Create Services/IngestService.cs**

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.ML.Tokenizers;
using Pgvector;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence.Specifications;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class IngestService(
    IRepository<Document> documentRepository,
    IRepository<DocumentChunk> chunkRepository,
    IRepository<Question> questionRepository,
    IRepository<Answer> answerRepository,
    IRepository<EventEntry> eventEntryRepository,
    IEmbeddingService embeddingService) : IIngestService
{
    private const int ChunkTokens = 512;
    private const int ChunkOverlap = 50;

    private static readonly TiktokenTokenizer Tokenizer =
        TiktokenTokenizer.CreateForModel("gpt-4o");

    public async Task IngestDocumentAsync(IngestDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var sourceHash = ComputeHash(request.Content);

        var existing = await documentRepository.FirstOrDefaultAsync(
            new DocumentByHashSpec(sourceHash), cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            SourceHash = sourceHash,
            IngestedUtc = now
        };
        await documentRepository.AddAsync(document, cancellationToken);

        var chunks = ChunkText(request.Content);
        var categoryTags = request.CategoryValues is not null
            ? CategoryTagsHelper.ToTags(request.CategoryValues)
            : [];

        var embeddings = await embeddingService.GenerateEmbeddingsAsync(chunks, cancellationToken);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                Content = chunks[i],
                Embedding = new Vector(embeddings[i].ToArray()),
                CategoryTags = categoryTags,
                ChunkIndex = i
            };
            await chunkRepository.AddAsync(chunk, cancellationToken);
        }

        await documentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task IngestQAAsync(IngestQARequest request, CancellationToken cancellationToken = default)
    {
        var textHash = ComputeHash(request.QuestionText);

        var existing = await questionRepository.FirstOrDefaultAsync(
            new QuestionByHashSpec(textHash), cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var questionFloats = await embeddingService.GenerateEmbeddingAsync(request.QuestionText, cancellationToken);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Text = request.QuestionText,
            TextHash = textHash,
            Embedding = new Vector(questionFloats.ToArray()),
            CategoryTags = request.QuestionCategoryValues is not null
                ? CategoryTagsHelper.ToTags(request.QuestionCategoryValues)
                : [],
            IngestedUtc = now
        };
        await questionRepository.AddAsync(question, cancellationToken);

        foreach (var answerRequest in request.Answers)
        {
            var answerFloats = await embeddingService.GenerateEmbeddingAsync(answerRequest.AnswerText, cancellationToken);

            var answer = new Answer
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Text = answerRequest.AnswerText,
                Embedding = new Vector(answerFloats.ToArray()),
                CategoryTags = answerRequest.CategoryValues is not null
                    ? CategoryTagsHelper.ToTags(answerRequest.CategoryValues)
                    : [],
                IngestedUtc = now
            };
            await answerRepository.AddAsync(answer, cancellationToken);
        }

        await questionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task IngestEventAsync(IngestEventRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await eventEntryRepository.FirstOrDefaultAsync(
            new EventEntryByFeedEntryIdSpec(request.FeedEntryId), cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var floats = await embeddingService.GenerateEmbeddingAsync(request.TextRepresentation, cancellationToken);

        var eventEntry = new EventEntry
        {
            Id = Guid.NewGuid(),
            FeedEntryId = request.FeedEntryId,
            Title = request.Title,
            TextRepresentation = request.TextRepresentation,
            Embedding = new Vector(floats.ToArray()),
            CategoryTags = request.CategoryValues is not null
                ? CategoryTagsHelper.ToTags(request.CategoryValues)
                : [],
            EventUtc = request.EventUtc,
            IngestedUtc = DateTime.UtcNow
        };
        await eventEntryRepository.AddAsync(eventEntry, cancellationToken);
        await eventEntryRepository.SaveChangesAsync(cancellationToken);
    }

    private static List<string> ChunkText(string text)
    {
        var ids = Tokenizer.EncodeToIds(text);
        var chunks = new List<string>();
        var start = 0;

        while (start < ids.Count)
        {
            var end = Math.Min(start + ChunkTokens, ids.Count);
            var slice = ids.Skip(start).Take(end - start).ToList();
            chunks.Add(Tokenizer.Decode(slice));
            start += ChunkTokens - ChunkOverlap;
        }

        return chunks;
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/Services/IngestService.cs
git commit -m "feat: implement IngestService with tiktoken chunking and OpenAI embeddings"
```

---

## Task 8: VectorSearchService

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/Services/VectorSearchService.cs`

`VectorSearchService` injects `VectorDbContext` directly — the KNN + GIN filter pattern (`ORDER BY embedding <=> $vec LIMIT k` with `category_tags && $tags`) cannot be expressed as a `Specification<T>` predicate. `CosineDistance` is an extension method from `Pgvector.EntityFrameworkCore` that translates to SQL `<=>`.

Category tag filtering is conditional: when `UserContext` is null or empty, no filter is applied (all content may appear). When tags are present, rows whose `CategoryTags` is empty (`{}`) also match — empty means "general content, applies to everyone".

- [ ] **Step 1: Create Services/VectorSearchService.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Persistence;

namespace Reshape.ElectricAi.VectorDb.Services;

public sealed class VectorSearchService(
    VectorDbContext context,
    IEmbeddingService embeddingService) : IVectorSearchService
{
    public async Task<IReadOnlyList<RetrievedChunk>> SearchDocumentsAsync(
        DocumentSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var queryVector = await EmbedAsync(filter.QueryText, cancellationToken);
        var userTags = BuildUserTags(filter.UserContext);

        var query = context.DocumentChunks
            .AsNoTracking()
            .Include(c => c.Document)
            .Select(c => new
            {
                Chunk = c,
                Distance = c.Embedding.CosineDistance(queryVector)
            });

        if (userTags.Length > 0)
        {
            query = query.Where(x =>
                x.Chunk.CategoryTags.Length == 0 ||
                x.Chunk.CategoryTags.Any(t => userTags.Contains(t)));
        }

        var results = await query
            .OrderBy(x => x.Distance)
            .Take(filter.TopK)
            .ToListAsync(cancellationToken);

        return results
            .Select(x => new RetrievedChunk(
                x.Chunk.DocumentId,
                x.Chunk.Document.Title,
                x.Chunk.Content,
                1f - (float)x.Distance))
            .ToList();
    }

    public async Task<IReadOnlyList<RetrievedQA>> SearchQuestionsAsync(
        QuestionSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var queryVector = await EmbedAsync(filter.QueryText, cancellationToken);
        var userTags = BuildUserTags(filter.UserContext);

        var questionQuery = context.Questions
            .AsNoTracking()
            .Select(q => new
            {
                Question = q,
                Distance = q.Embedding.CosineDistance(queryVector)
            });

        if (userTags.Length > 0)
        {
            questionQuery = questionQuery.Where(x =>
                x.Question.CategoryTags.Length == 0 ||
                x.Question.CategoryTags.Any(t => userTags.Contains(t)));
        }

        var topQuestions = await questionQuery
            .OrderBy(x => x.Distance)
            .Take(filter.TopK)
            .ToListAsync(cancellationToken);

        var results = new List<RetrievedQA>(topQuestions.Count);

        foreach (var q in topQuestions)
        {
            var answerQuery = context.Answers
                .AsNoTracking()
                .Where(a => a.QuestionId == q.Question.Id)
                .Select(a => new
                {
                    Answer = a,
                    Distance = a.Embedding.CosineDistance(queryVector)
                });

            if (userTags.Length > 0)
            {
                answerQuery = answerQuery.Where(x =>
                    x.Answer.CategoryTags.Length == 0 ||
                    x.Answer.CategoryTags.Any(t => userTags.Contains(t)));
            }

            var answers = await answerQuery
                .OrderBy(x => x.Distance)
                .ToListAsync(cancellationToken);

            results.Add(new RetrievedQA(
                q.Question.Text,
                answers.Select(a => new RetrievedAnswer(a.Answer.Text, 1f - (float)a.Distance)).ToList(),
                1f - (float)q.Distance));
        }

        return results;
    }

    public async Task<IReadOnlyList<RetrievedEvent>> SearchEventsAsync(
        EventSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var queryVector = await EmbedAsync(filter.QueryText, cancellationToken);
        var userTags = BuildUserTags(filter.UserContext);

        var query = context.EventEntries
            .AsNoTracking()
            .Select(e => new
            {
                Event = e,
                Distance = e.Embedding.CosineDistance(queryVector)
            });

        if (userTags.Length > 0)
        {
            query = query.Where(x =>
                x.Event.CategoryTags.Length == 0 ||
                x.Event.CategoryTags.Any(t => userTags.Contains(t)));
        }

        var results = await query
            .OrderBy(x => x.Distance)
            .Take(filter.TopK)
            .ToListAsync(cancellationToken);

        return results
            .Select(x => new RetrievedEvent(
                x.Event.FeedEntryId,
                x.Event.Title,
                x.Event.TextRepresentation,
                x.Event.EventUtc,
                1f - (float)x.Distance))
            .ToList();
    }

    private async Task<Vector> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var floats = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken);
        return new Vector(floats.ToArray());
    }

    private static string[] BuildUserTags(
        IReadOnlyDictionary<Core.Enums.Category, IReadOnlyList<string>>? userContext) =>
        userContext is not null ? CategoryTagsHelper.ToTags(userContext) : [];
}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Reshape.ElectricAi.VectorDb/Reshape.ElectricAi.VectorDb.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/Services/VectorSearchService.cs
git commit -m "feat: implement VectorSearchService with KNN + category-tag filtering"
```

---

## Task 9: VectorDbModule + Program.cs + appsettings

**Files:**
- Create: `src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs`
- Modify: `src/Reshape.ElectricAi.Presentation/Program.cs`
- Modify: `src/Reshape.ElectricAi.Presentation/appsettings.json`

`OpenAi:ApiKey` goes to **user-secrets only** — never in `appsettings.json`.

- [ ] **Step 0: Create Services/OpenAiEmbeddingService.cs**

Wraps the sealed `EmbeddingClient` behind `IEmbeddingService`. Registered as singleton — `EmbeddingClient` is thread-safe.

```csharp
using OpenAI.Embeddings;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.VectorDb.Services;

internal sealed class OpenAiEmbeddingService(EmbeddingClient client, ChatOptions chatOptions) : IEmbeddingService
{
    private readonly EmbeddingGenerationOptions _options = new() { Dimensions = chatOptions.EmbeddingDimensions };

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await client.GenerateEmbeddingAsync(text, _options, cancellationToken);
        return result.Value.ToFloats();
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var result = await client.GenerateEmbeddingsAsync(texts, _options, cancellationToken);
        return result.Value.Select(e => e.ToFloats()).ToList();
    }
}
```

- [ ] **Step 1: Create VectorDbModule.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Embeddings;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.VectorDb;

public static class VectorDbModule
{
    public static IServiceCollection AddVectorDbModule(this IServiceCollection services, IConfiguration configuration)
    {
        var chatOptions = BuildChatOptions(configuration);
        services.AddSingleton(chatOptions);

        var apiKey = configuration["OpenAi:ApiKey"]
            ?? throw new InvalidOperationException("OpenAi:ApiKey is required (user-secrets in dev, env var in prod).");

        services.AddSingleton(new OpenAIClient(apiKey));
        services.AddSingleton(sp => sp.GetRequiredService<OpenAIClient>().GetEmbeddingClient(chatOptions.EmbeddingModel));
        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<VectorDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "vector");
                npgsql.UseVector();
            }));

        services.AddScoped<IRepository<Document>, VectorRepository<Document>>();
        services.AddScoped<IRepository<DocumentChunk>, VectorRepository<DocumentChunk>>();
        services.AddScoped<IRepository<Question>, VectorRepository<Question>>();
        services.AddScoped<IRepository<Answer>, VectorRepository<Answer>>();
        services.AddScoped<IRepository<EventEntry>, VectorRepository<EventEntry>>();

        services.AddScoped<IIngestService, IngestService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();

        return services;
    }

    private static ChatOptions BuildChatOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(ChatOptions.SectionName);
        return new ChatOptions
        {
            EmbeddingModel = section["EmbeddingModel"] ?? "text-embedding-3-small",
            EmbeddingDimensions = int.TryParse(section["EmbeddingDimensions"], out var dims) ? dims : 1536
        };
    }
}
```

- [ ] **Step 2: Add `AddVectorDbModule` to Program.cs**

Add after `builder.Services.AddPlansModule(builder.Configuration);`:

```csharp
builder.Services.AddVectorDbModule(builder.Configuration);
```

Add the using at the top of Program.cs:
```csharp
using Reshape.ElectricAi.VectorDb;
using Reshape.ElectricAi.VectorDb.Persistence;
```

Add VectorDbContext auto-migration in the development block (after the existing Plans migration):
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var plansDb = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
    await plansDb.Database.MigrateAsync();

    var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
    await vectorDb.Database.MigrateAsync();

    app.UseSwagger();
    app.MapScalarApiReference();
}
```

The full updated top of Program.cs:

```csharp
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Plans;
using Reshape.ElectricAi.Plans.Persistence;
using Reshape.ElectricAi.Presentation.Filters;
using Reshape.ElectricAi.Presentation.Middleware;
using Reshape.ElectricAi.VectorDb;
using Reshape.ElectricAi.VectorDb.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddPlansModule(builder.Configuration);
builder.Services.AddVectorDbModule(builder.Configuration);
```

And the dev migration block:

```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var plansDb = scope.ServiceProvider.GetRequiredService<PlansDbContext>();
    await plansDb.Database.MigrateAsync();

    var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
    await vectorDb.Database.MigrateAsync();

    app.UseSwagger();
    app.MapScalarApiReference();
}
```

- [ ] **Step 3: Update appsettings.json**

Add `Chat` section (do NOT add `OpenAi:ApiKey` here — that goes to user-secrets):

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=electric_ai;Username=postgres;Password=admin"
  },
  "Auth": {
    "Issuer": "reshape-electric-ai",
    "Audience": "reshape-electric-ai-api",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7,
    "SingleSession": false
  },
  "Chat": {
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingDimensions": 1536
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ],
    "Enrich": [ "FromLogContext" ]
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:3000" ]
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 4: Add OpenAi:ApiKey to user-secrets**

```bash
dotnet user-secrets set "OpenAi:ApiKey" "<your-openai-api-key>" --project src/Reshape.ElectricAi.Presentation
```

- [ ] **Step 5: Build full solution**

```bash
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/Reshape.ElectricAi.VectorDb/VectorDbModule.cs \
        src/Reshape.ElectricAi.Presentation/Program.cs \
        src/Reshape.ElectricAi.Presentation/appsettings.json
git commit -m "feat: wire VectorDbModule into Presentation — DI, migration, OpenAI client"
```

---

## Task 10: EF Migration + Final Build Verification

- [ ] **Step 1: Ensure pgvector extension exists in the database**

Run this once against your local database:
```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

Or via psql:
```bash
psql -U postgres -d electric_ai -c "CREATE EXTENSION IF NOT EXISTS vector;"
```

- [ ] **Step 2: Ensure the `vector` schema exists**

```bash
psql -U postgres -d electric_ai -c "CREATE SCHEMA IF NOT EXISTS vector;"
```

- [ ] **Step 3: Generate EF migration for VectorDb**

```bash
dotnet ef migrations add InitVectorDb \
  --project src/Reshape.ElectricAi.VectorDb \
  --startup-project src/Reshape.ElectricAi.Presentation \
  --output-dir Persistence/Migrations \
  --context VectorDbContext
```

Expected output: `Build started... Done. Migration 'InitVectorDb' created.`

- [ ] **Step 4: Inspect the generated migration**

Open `src/Reshape.ElectricAi.VectorDb/Persistence/Migrations/<timestamp>_InitVectorDb.cs` and verify:
- Tables created in schema `vector`: `documents`, `document_chunks`, `questions`, `answers`, `event_entries`
- `embedding` columns typed as `vector(1536)`
- HNSW indexes on embedding columns with `vector_cosine_ops`
- GIN indexes on `category_tags` columns
- Unique indexes on `source_hash`, `text_hash`, `feed_entry_id`
- Cascade delete from documents → document_chunks and questions → answers

If anything looks wrong, drop the migration with `dotnet ef migrations remove --project ... --startup-project ...`, fix the configuration, and re-generate.

- [ ] **Step 5: Apply migration**

```bash
dotnet ef database update \
  --project src/Reshape.ElectricAi.VectorDb \
  --startup-project src/Reshape.ElectricAi.Presentation \
  --context VectorDbContext
```

Expected: `Applying migration '..._InitVectorDb'. Done.`

- [ ] **Step 6: Full solution build**

```bash
dotnet build
```

Expected: Build succeeded, 0 errors, 0 warnings that aren't pre-existing.

- [ ] **Step 7: Start application and verify startup**

```bash
dotnet run --project src/Reshape.ElectricAi.Presentation
```

Expected log lines during startup (dev environment):
- `Applying migration` for both PlansDbContext and VectorDbContext (or `No migrations to apply` if already applied)
- No exceptions
- Application listening on configured port

Ctrl+C to stop.

- [ ] **Step 8: Commit migration**

```bash
git add src/Reshape.ElectricAi.VectorDb/Persistence/Migrations/
git commit -m "feat: add InitVectorDb EF Core migration"
```

---

---

## Task 11: Integration Tests

**Files:**
- Create: `tests/Reshape.ElectricAi.VectorDb.IntegrationTests/Reshape.ElectricAi.VectorDb.IntegrationTests.csproj`
- Create: `tests/Reshape.ElectricAi.VectorDb.IntegrationTests/VectorDbFixture.cs`
- Create: `tests/Reshape.ElectricAi.VectorDb.IntegrationTests/FakeEmbeddingService.cs`
- Create: `tests/Reshape.ElectricAi.VectorDb.IntegrationTests/IngestServiceTests.cs`
- Create: `tests/Reshape.ElectricAi.VectorDb.IntegrationTests/VectorSearchServiceTests.cs`

Tests use Testcontainers with `pgvector/pgvector:pg16` image — no real OpenAI calls. `FakeEmbeddingService` returns deterministic vectors seeded from text hash (same input → same vector). This allows search-by-exact-query-text to retrieve ingested content with distance 0.

- [ ] **Step 1: Add test project to solution**

```bash
dotnet new xunit -n Reshape.ElectricAi.VectorDb.IntegrationTests -o tests/Reshape.ElectricAi.VectorDb.IntegrationTests --framework net10.0
dotnet sln ElectricCastle.slnx add tests/Reshape.ElectricAi.VectorDb.IntegrationTests/Reshape.ElectricAi.VectorDb.IntegrationTests.csproj --solution-folder tests
```

- [ ] **Step 2: Write the .csproj**

Replace the auto-generated csproj with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Reshape.ElectricAi.VectorDb\Reshape.ElectricAi.VectorDb.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create FakeEmbeddingService.cs**

Returns deterministic unit-length vectors. Same text → same vector, so searching with the ingested text finds it at distance 0.

```csharp
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.VectorDb.IntegrationTests;

internal sealed class FakeEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 1536;

    public Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(MakeVector(text));

    public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(
            texts.Select(MakeVector).ToList());

    private static ReadOnlyMemory<float> MakeVector(string text)
    {
        var floats = new float[Dimensions];
        var rng = new Random(text.GetHashCode());
        for (var i = 0; i < Dimensions; i++)
            floats[i] = (float)rng.NextDouble();
        var magnitude = MathF.Sqrt(floats.Sum(f => f * f));
        for (var i = 0; i < Dimensions; i++)
            floats[i] /= magnitude;
        return new ReadOnlyMemory<float>(floats);
    }
}
```

- [ ] **Step 4: Create VectorDbFixture.cs**

Starts a pgvector Testcontainers instance, runs EF migrations, exposes `DbContext` + service factories.

```csharp
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Reshape.ElectricAi.Core.Configuration;
using Reshape.ElectricAi.Core.Services;
using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;
using Reshape.ElectricAi.VectorDb.Persistence;
using Reshape.ElectricAi.VectorDb.Services;

namespace Reshape.ElectricAi.VectorDb.IntegrationTests;

public sealed class VectorDbFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    public VectorDbContext DbContext { get; private set; } = null!;
    public IEmbeddingService EmbeddingService { get; } = new FakeEmbeddingService();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "vector");
                npgsql.UseVector();
            })
            .Options;

        DbContext = new VectorDbContext(options, new ChatOptions { EmbeddingDimensions = 1536 });
        await DbContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;");
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE vector.event_entries, vector.answers, vector.questions, vector.document_chunks, vector.documents RESTART IDENTITY CASCADE;");
    }

    public IngestService CreateIngestService()
    {
        return new IngestService(
            new VectorRepository<Document>(DbContext),
            new VectorRepository<DocumentChunk>(DbContext),
            new VectorRepository<Question>(DbContext),
            new VectorRepository<Answer>(DbContext),
            new VectorRepository<EventEntry>(DbContext),
            EmbeddingService);
    }

    public VectorSearchService CreateVectorSearchService() =>
        new(DbContext, EmbeddingService);
}
```

- [ ] **Step 5: Create IngestServiceTests.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.VectorDb.IntegrationTests;

public sealed class IngestServiceTests(VectorDbFixture fixture) : IClassFixture<VectorDbFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task IngestDocumentAsync_creates_document_and_chunks()
    {
        var svc = fixture.CreateIngestService();
        var request = new IngestDocumentRequest(
            "EC Guide",
            string.Join(' ', Enumerable.Repeat("word", 600))); // ~600 tokens → 2 chunks

        await svc.IngestDocumentAsync(request);

        var docs = await fixture.DbContext.Documents.CountAsync();
        var chunks = await fixture.DbContext.DocumentChunks.CountAsync();
        Assert.Equal(1, docs);
        Assert.True(chunks >= 1);
    }

    [Fact]
    public async Task IngestDocumentAsync_is_idempotent()
    {
        var svc = fixture.CreateIngestService();
        var request = new IngestDocumentRequest("EC Guide", "Some content about the festival.");

        await svc.IngestDocumentAsync(request);
        await svc.IngestDocumentAsync(request);

        Assert.Equal(1, await fixture.DbContext.Documents.CountAsync());
    }

    [Fact]
    public async Task IngestDocumentAsync_stores_category_tags_on_chunks()
    {
        var svc = fixture.CreateIngestService();
        var request = new IngestDocumentRequest(
            "Camping Info",
            "Camping zone is located in sector B.",
            new Dictionary<Category, IReadOnlyList<string>>
            {
                [Category.Accommodation] = ["Camping"]
            });

        await svc.IngestDocumentAsync(request);

        var chunk = await fixture.DbContext.DocumentChunks.FirstAsync();
        Assert.Contains("Accommodation.Camping", chunk.CategoryTags);
    }

    [Fact]
    public async Task IngestQAAsync_creates_question_and_answers()
    {
        var svc = fixture.CreateIngestService();
        var request = new IngestQARequest(
            "What time does the main stage open?",
            [
                new IngestAnswerRequest("The main stage opens at 14:00 on Friday."),
                new IngestAnswerRequest("Village residents: main stage opens at 15:00.")
            ]);

        await svc.IngestQAAsync(request);

        Assert.Equal(1, await fixture.DbContext.Questions.CountAsync());
        Assert.Equal(2, await fixture.DbContext.Answers.CountAsync());
    }

    [Fact]
    public async Task IngestQAAsync_is_idempotent()
    {
        var svc = fixture.CreateIngestService();
        var request = new IngestQARequest(
            "What time does the main stage open?",
            [new IngestAnswerRequest("14:00")]);

        await svc.IngestQAAsync(request);
        await svc.IngestQAAsync(request);

        Assert.Equal(1, await fixture.DbContext.Questions.CountAsync());
        Assert.Equal(1, await fixture.DbContext.Answers.CountAsync());
    }

    [Fact]
    public async Task IngestEventAsync_creates_event_entry()
    {
        var svc = fixture.CreateIngestService();
        var feedId = Guid.NewGuid();
        var request = new IngestEventRequest(
            feedId,
            "Headliner Set",
            "Headliner takes the Main Stage at 22:00.",
            DateTime.UtcNow.AddHours(5));

        await svc.IngestEventAsync(request);

        Assert.Equal(1, await fixture.DbContext.EventEntries.CountAsync());
        var entry = await fixture.DbContext.EventEntries.FirstAsync();
        Assert.Equal(feedId, entry.FeedEntryId);
    }

    [Fact]
    public async Task IngestEventAsync_is_idempotent()
    {
        var svc = fixture.CreateIngestService();
        var feedId = Guid.NewGuid();
        var request = new IngestEventRequest(
            feedId, "Headliner", "At 22:00.", DateTime.UtcNow);

        await svc.IngestEventAsync(request);
        await svc.IngestEventAsync(request);

        Assert.Equal(1, await fixture.DbContext.EventEntries.CountAsync());
    }
}
```

- [ ] **Step 6: Create VectorSearchServiceTests.cs**

```csharp
using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Enums;

namespace Reshape.ElectricAi.VectorDb.IntegrationTests;

public sealed class VectorSearchServiceTests(VectorDbFixture fixture) : IClassFixture<VectorDbFixture>, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SearchDocumentsAsync_returns_ingested_chunk()
    {
        var ingest = fixture.CreateIngestService();
        var search = fixture.CreateVectorSearchService();

        const string content = "Tickets can be purchased at the main entrance gate.";
        await ingest.IngestDocumentAsync(new IngestDocumentRequest("FAQ", content));

        var results = await search.SearchDocumentsAsync(new DocumentSearchFilter(content, TopK: 5));

        Assert.NotEmpty(results);
        Assert.Equal("FAQ", results[0].DocumentTitle);
        Assert.Equal(1f, results[0].Score, precision: 1); // same text → score ≈ 1
    }

    [Fact]
    public async Task SearchDocumentsAsync_filters_out_tagged_chunk_for_non_matching_user()
    {
        var ingest = fixture.CreateIngestService();
        var search = fixture.CreateVectorSearchService();

        const string query = "camping facilities nearby";

        await ingest.IngestDocumentAsync(new IngestDocumentRequest(
            "Camping Guide",
            query,
            new Dictionary<Category, IReadOnlyList<string>>
            {
                [Category.Accommodation] = ["Camping"]
            }));

        // Village user — no camping tag
        var userContext = new Dictionary<Category, IReadOnlyList<string>>
        {
            [Category.Accommodation] = ["Village"]
        };
        var results = await search.SearchDocumentsAsync(
            new DocumentSearchFilter(query, userContext, TopK: 5));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchDocumentsAsync_returns_untagged_chunk_for_any_user()
    {
        var ingest = fixture.CreateIngestService();
        var search = fixture.CreateVectorSearchService();

        const string content = "General festival rules apply to everyone.";
        await ingest.IngestDocumentAsync(new IngestDocumentRequest("Rules", content));

        var userContext = new Dictionary<Category, IReadOnlyList<string>>
        {
            [Category.Accommodation] = ["Village"]
        };
        var results = await search.SearchDocumentsAsync(
            new DocumentSearchFilter(content, userContext, TopK: 5));

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task SearchQuestionsAsync_returns_matching_qa()
    {
        var ingest = fixture.CreateIngestService();
        var search = fixture.CreateVectorSearchService();

        const string question = "Where can I refill my water bottle?";
        await ingest.IngestQAAsync(new IngestQARequest(
            question,
            [new IngestAnswerRequest("Water stations are marked on the festival map.")]));

        var results = await search.SearchQuestionsAsync(new QuestionSearchFilter(question, TopK: 5));

        Assert.NotEmpty(results);
        Assert.Equal(question, results[0].QuestionText);
        Assert.Single(results[0].Answers);
    }

    [Fact]
    public async Task SearchQuestionsAsync_filters_answers_by_user_context()
    {
        var ingest = fixture.CreateIngestService();
        var search = fixture.CreateVectorSearchService();

        const string question = "What time is breakfast?";
        await ingest.IngestQAAsync(new IngestQARequest(
            question,
            [
                new IngestAnswerRequest(
                    "Camping breakfast runs 07:00-10:00.",
                    new Dictionary<Category, IReadOnlyList<string>>
                    {
                        [Category.Accommodation] = ["Camping"]
                    }),
                new IngestAnswerRequest("General info: see the schedule app.")
            ]));

        // Village user — only gets the general (untagged) answer
        var userContext = new Dictionary<Category, IReadOnlyList<string>>
        {
            [Category.Accommodation] = ["Village"]
        };
        var results = await search.SearchQuestionsAsync(
            new QuestionSearchFilter(question, userContext, TopK: 5));

        Assert.NotEmpty(results);
        var answers = results[0].Answers;
        Assert.Single(answers); // only the general answer
        Assert.Contains("General info", answers[0].AnswerText);
    }

    [Fact]
    public async Task SearchEventsAsync_returns_matching_event()
    {
        var ingest = fixture.CreateIngestService();
        var search = fixture.CreateVectorSearchService();

        const string text = "Headliner performs on the Main Stage at 22:00 on Saturday.";
        await ingest.IngestEventAsync(new IngestEventRequest(
            Guid.NewGuid(), "Headliner", text, DateTime.UtcNow.AddHours(3)));

        var results = await search.SearchEventsAsync(new EventSearchFilter(text, TopK: 5));

        Assert.NotEmpty(results);
        Assert.Equal("Headliner", results[0].Title);
    }

    [Fact]
    public async Task SearchEventsAsync_filters_by_user_context()
    {
        var ingest = fixture.CreateIngestService();
        var search = fixture.CreateVectorSearchService();

        const string text = "Camping zone late-night shuttle departs at 02:00.";
        await ingest.IngestEventAsync(new IngestEventRequest(
            Guid.NewGuid(),
            "Camping Shuttle",
            text,
            DateTime.UtcNow.AddHours(2),
            new Dictionary<Category, IReadOnlyList<string>>
            {
                [Category.Accommodation] = ["Camping"]
            }));

        // Village user should not see camping events
        var userContext = new Dictionary<Category, IReadOnlyList<string>>
        {
            [Category.Accommodation] = ["Village"]
        };
        var results = await search.SearchEventsAsync(new EventSearchFilter(text, userContext, TopK: 5));

        Assert.Empty(results);
    }
}
```

- [ ] **Step 7: Build tests**

```bash
dotnet build tests/Reshape.ElectricAi.VectorDb.IntegrationTests/Reshape.ElectricAi.VectorDb.IntegrationTests.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Run tests**

```bash
dotnet test tests/Reshape.ElectricAi.VectorDb.IntegrationTests/ --logger "console;verbosity=normal"
```

Expected: All 12 tests pass. Testcontainers will pull `pgvector/pgvector:pg16` on first run — requires Docker.

- [ ] **Step 9: Commit**

```bash
git add tests/Reshape.ElectricAi.VectorDb.IntegrationTests/
git commit -m "test: add VectorDb integration tests for ingest and search flows"
```

---

## Out of Scope

- SSE / LiveFeed integration — separate developer
- Controllers / API endpoints that expose `IIngestService` / `IVectorSearchService` — separate plan
- RAG chat pipeline (combining search results with LLM completion) — separate plan
