# VectorDb + Core RAG System — Design Spec

**Date:** 2026-05-23  
**Branch:** `feature/vector-db`  
**Scope:** `Reshape.ElectricAi.Core` adaptations + `Reshape.ElectricAi.VectorDb` full implementation  
**Out of scope:** LiveFeed/SSE (separate dev), AiChat orchestration (separate concern)

---

## Problem

The system needs a RAG layer over three distinct knowledge domains:
1. **Official EC documentation** — rules, lineup, daily recs, ticket guide, links (static, chunk-based)
2. **Q&A knowledge base** — questions with multiple context-specific answers (categorizable, user-filtered)
3. **Organizer events** — real-time facts emitted by organizers (categorizable, user-filtered)

All three need vector search **with category-based filtering**. A camping-section document is irrelevant to a village resident. If no user context is provided, documents/chunks with empty `category_tags` surface naturally; tagged documents may still appear if their tags overlap the user context.

---

## Decisions

### ICategorizable interface

The existing `ICategorizable` in Core returns `IReadOnlyCollection<Category>` (category types only). This is insufficient — we need the *values* within each category (e.g. which `TransportMode`, which accommodation type, which artists).

**Decision:** Replace the existing interface body with a value-bearing dictionary:

```csharp
public interface ICategorizable
{
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues { get; }
}
```

Values are:
- String representations of enum names for typed categories (e.g. `"EcBus"`, `"Camping"`, `"Vegan"`)
- Free-form artist name strings for `Category.Lineup` (lineup changes yearly, enum is impractical)

No new enums needed — all user-described categories map to existing Core enums:

| User category | Core enum |
|---|---|
| Transportation | `TransportMode` (RideShare, Car, EcTrain, EcBus, Helicopter) |
| Accommodation | `Accommodation` |
| Alimentation / food restrictions | `FoodRestriction` |
| Artists | `Category.Lineup` + free-form `string` values |
| Genres | `MusicGenre` |
| Ticket access type | `TicketType` |
| Activity types | `ActivityType` |

### Category filtering — DB storage strategy

Flatten the dict to a **namespaced `text[]` column** with a GIN index on every table that is categorizable:

```
category_tags text[] NOT NULL DEFAULT '{}'
```

Format: `{"Transport.EcBus","Accommodation.Camping","Lineup.Justin Timberlake"}`

Overlap query used across all three domains:
```sql
WHERE (entity.category_tags && ARRAY['Transport.EcBus','Accommodation.Camping']
       OR entity.category_tags = '{}')
```

- If the caller passes a user context, the `&&` filter applies; rows with empty tags pass through as "general content."
- If no user context is provided, the filter is dropped entirely — all rows are candidates.

`ICategorizable.CategoryValues` is reconstructed from tags at read time by splitting on the first `.`.

### Three search domains — all support category filtering

| Domain | Tables | Embedding on | Category tags on |
|---|---|---|---|
| Official EC docs | `documents` + `document_chunks` | `document_chunks` | `document_chunks` (copied from ingest request) |
| Q&A | `questions` + `answers` | `questions` | `questions` AND `answers` independently |
| Organizer events | `events` | `events` | `events` |

Placing `category_tags` on `document_chunks` (not on `documents`) avoids a JOIN at search time — the category filter and the KNN sort are evaluated in one pass on the chunk table.

### Embedding model — config-driven, migration-gated

The embedding model name and expected dimensions come from configuration:

```json
"Chat": {
  "EmbeddingModel": "text-embedding-3-small",
  "EmbeddingDimensions": 1536
}
```

`VectorDbModule` reads both at startup and passes `EmbeddingDimensions` to `IngestService` and `VectorSearchService`. **The EF migration hardcodes `vector(1536)` in the column definition.** Changing the model requires:
1. A new migration that drops + recreates the `vector(N)` columns and HNSW indexes
2. Re-embedding all existing rows

This is a documented breaking change (CODE.md: "Changing the model is a migration event"). The config value is cross-checked at startup: if `EmbeddingDimensions` does not match the value the migration was generated with, log a fatal and refuse to start.

### Repository pattern alignment

The merge introduced a Repository + Specification pattern across all libs:

- `IRepository<T>` (Core) — generic CRUD interface
- `EfRepository<TContext, T>` (currently in Plans) — generic EF Core implementation
- `SpecificationEvaluator` (currently in Plans) — applies `ISpecification<T>` to an `IQueryable<T>`
- `PlansRepository<T>` — typed wrapper that binds `EfRepository` to `PlansDbContext`

**Decision:** Move `EfRepository<TContext, T>` and `SpecificationEvaluator` to `Core/Persistence/` so VectorDb can share them without duplication. This requires adding `Microsoft.EntityFrameworkCore` to Core's `.csproj`. Core depending on EF Core abstractions does not violate the "Core MUST NOT reference any feature lib" rule — EF Core is a framework dependency, not a feature lib.

**Coordination:** The Plans dev must accept the move (file deletions from `Plans/Persistence/`). No behavior change — the namespace of `EfRepository` changes from `Reshape.ElectricAi.Plans.Persistence` to `Reshape.ElectricAi.Core.Persistence`; a single `using` update in Plans files.

VectorDb then adds `VectorRepository<T>` mirroring the Plans pattern:

```csharp
// VectorDb/Persistence/VectorRepository.cs
public sealed class VectorRepository<T>(VectorDbContext context)
    : EfRepository<VectorDbContext, T>(context)
    where T : class;
```

**Service access patterns:**

| Service | DB access method | Reason |
|---|---|---|
| `IngestService` | `IRepository<T>` (via `VectorRepository<T>`) | Standard CRUD: hash check, insert document/chunk/question/answer/event |
| `VectorSearchService` | `VectorDbContext` directly | KNN queries with `<=>` operator + category filter are not expressible as `ISpecification<T>` predicates |

`VectorDbModule` registers both:
```csharp
services.AddScoped(typeof(IRepository<>), typeof(VectorRepository<>));
services.AddScoped<IVectorSearchService, VectorSearchService>();
services.AddScoped<IIngestService, IngestService>();
```

---

## Section 1: Core changes

### Files moved

| From | To | Change |
|---|---|---|
| `Plans/Persistence/EfRepository.cs` | `Core/Persistence/EfRepository.cs` | Namespace: `Reshape.ElectricAi.Core.Persistence` |
| `Plans/Persistence/SpecificationEvaluator.cs` | `Core/Persistence/SpecificationEvaluator.cs` | Namespace: `Reshape.ElectricAi.Core.Persistence` |

Plans files that `using Reshape.ElectricAi.Plans.Persistence;` to reach these classes need their `using` updated to `Reshape.ElectricAi.Core.Persistence;`.

### Files changed / added in Core

| File | Change |
|---|---|
| `Core/Core.csproj` | Add `<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />` |
| `Core/Domain/ICategorizable.cs` | Replace `IReadOnlyCollection<Category> Categories` with `IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues` |
| `Core/Services/IVectorSearchService.cs` | New |
| `Core/Services/IIngestService.cs` | New |
| `Core/Dtos/VectorSearch/RetrievedChunk.cs` | New |
| `Core/Dtos/VectorSearch/RetrievedQA.cs` | New |
| `Core/Dtos/VectorSearch/RetrievedAnswer.cs` | New |
| `Core/Dtos/VectorSearch/RetrievedEvent.cs` | New |
| `Core/Dtos/VectorSearch/DocumentSearchFilter.cs` | New |
| `Core/Dtos/VectorSearch/QuestionSearchFilter.cs` | New |
| `Core/Dtos/VectorSearch/EventSearchFilter.cs` | New |
| `Core/Dtos/VectorSearch/IngestDocumentRequest.cs` | New |
| `Core/Dtos/VectorSearch/IngestQARequest.cs` | New |
| `Core/Dtos/VectorSearch/IngestAnswerRequest.cs` | New |
| `Core/Dtos/VectorSearch/IngestEventRequest.cs` | New |

### Interface contracts

```csharp
// Core/Services/IVectorSearchService.cs
public interface IVectorSearchService
{
    Task<IReadOnlyList<RetrievedChunk>> SearchDocumentsAsync(
        string query, DocumentSearchFilter filter, CancellationToken ct);

    Task<IReadOnlyList<RetrievedQA>> SearchQuestionsAsync(
        string query, QuestionSearchFilter filter, CancellationToken ct);

    Task<IReadOnlyList<RetrievedEvent>> SearchEventsAsync(
        string query, EventSearchFilter filter, CancellationToken ct);
}

// Core/Services/IIngestService.cs
public interface IIngestService
{
    Task IngestDocumentAsync(IngestDocumentRequest request, CancellationToken ct);
    Task IngestQAAsync(IngestQARequest request, CancellationToken ct);
    Task IngestEventAsync(IngestEventRequest request, CancellationToken ct);
}
```

### DTO shapes

```csharp
// Search results
public record RetrievedChunk(
    Guid DocumentId, int ChunkIndex, string Text,
    string Source, string SourceRef, double Score);

public record RetrievedAnswer(
    Guid AnswerId, string Text,
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues);

public record RetrievedQA(
    Guid QuestionId, string QuestionText, double QuestionScore,
    IReadOnlyList<RetrievedAnswer> Answers);

public record RetrievedEvent(
    Guid EventId, Guid FeedEntryId, string Title, string Body,
    double Score,
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues,
    DateTime PublishedUtc);

// Search filters — all three domains accept UserContext
public record DocumentSearchFilter(
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    string[]? Sources = null,
    int TopK = 6);

public record QuestionSearchFilter(
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 6);

public record EventSearchFilter(
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 6);

// Ingest requests
public record IngestDocumentRequest(
    string Source,
    string SourceRef,
    string Content,
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? CategoryValues = null);
// IngestService computes ContentHash internally (SHA-256 of Content)
// CategoryValues are copied down to every DocumentChunk at ingest time

public record IngestAnswerRequest(
    string Text,
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues);

public record IngestQARequest(
    string SourceRef,
    string QuestionText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>> QuestionCategoryValues,
    IReadOnlyList<IngestAnswerRequest> Answers);

public record IngestEventRequest(
    Guid FeedEntryId,
    string Title,
    string Body,
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues,
    DateTime PublishedUtc);
```

---

## Section 2: VectorDb schema

Schema name: `vector`. One `VectorDbContext`. Migrations history table: `vector.__EFMigrationsHistory`.

### Table: `vector.documents`

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` PK | |
| `source` | `text NOT NULL` | Enum name: Rule, Lineup, DailyRec, TicketGuide, Link |
| `source_ref` | `text NOT NULL` | Path-like: `"rules/no-drones"` |
| `content_hash` | `text NOT NULL UNIQUE` | SHA-256 — idempotency gate |
| `created_utc` | `timestamptz NOT NULL` | |

No `category_tags` on `documents` — tags live on chunks to avoid JOIN at search time.

### Table: `vector.document_chunks`

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` PK | |
| `document_id` | `uuid FK → documents` | CASCADE delete |
| `chunk_index` | `int NOT NULL` | 0-based position in parent document |
| `text` | `text NOT NULL` | Raw chunk text (400-token target, 50-token overlap) |
| `embedding` | `vector(1536)` | HNSW index (cosine) |
| `category_tags` | `text[] NOT NULL DEFAULT '{}'` | GIN index. Copied from `IngestDocumentRequest.CategoryValues` at ingest |

### Table: `vector.questions`

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` PK | |
| `text` | `text NOT NULL` | Question text |
| `embedding` | `vector(1536)` | HNSW index (cosine) |
| `category_tags` | `text[] NOT NULL DEFAULT '{}'` | GIN index. Format: `"Category.Value"` |
| `content_hash` | `text NOT NULL UNIQUE` | SHA-256 of question text — idempotency gate |
| `source_ref` | `text NOT NULL` | e.g. `"faqs/transport-bus"` |
| `created_utc` | `timestamptz NOT NULL` | |

### Table: `vector.answers`

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` PK | |
| `question_id` | `uuid FK → questions` | CASCADE delete |
| `text` | `text NOT NULL` | Answer text |
| `category_tags` | `text[] NOT NULL DEFAULT '{}'` | GIN index. Empty = general (matches all user contexts) |
| `created_utc` | `timestamptz NOT NULL` | |

No embedding on answers — retrieval is by question similarity, answers are fetched relationally.

### Table: `vector.events`

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` PK | |
| `feed_entry_id` | `uuid NOT NULL UNIQUE` | Loose ref to `feed.feed_entries` |
| `title` | `text NOT NULL` | |
| `body` | `text NOT NULL` | |
| `embedding` | `vector(1536)` | HNSW index (cosine) |
| `category_tags` | `text[] NOT NULL DEFAULT '{}'` | GIN index |
| `published_utc` | `timestamptz NOT NULL` | |
| `created_utc` | `timestamptz NOT NULL` | |

Deduplication: `feed_entry_id` UNIQUE constraint. `IngestEventAsync` upserts on `feed_entry_id`.

---

## Section 3: Services and implementation structure

### VectorDb entities → C# names

| DB table | Entity class | Implements `ICategorizable`? |
|---|---|---|
| `vector.documents` | `Document` | No |
| `vector.document_chunks` | `DocumentChunk` | Yes (has `category_tags`) |
| `vector.questions` | `Question` | Yes |
| `vector.answers` | `Answer` | Yes |
| `vector.events` | `EventEntry` | Yes |

Entities implementing `ICategorizable` reconstruct `CategoryValues` from `category_tags` by splitting each tag on the first `.`.

### Implementation services

**`VectorSearchService`** implements `IVectorSearchService`:
- Injects `VectorDbContext` directly (KNN + category filter not expressible as `ISpecification<T>`)
- `SearchDocumentsAsync`: embed query → cosine KNN on `document_chunks.embedding` → apply category filter if `UserContext` non-null → return `RetrievedChunk[]`
- `SearchQuestionsAsync`: embed query → cosine KNN on `questions.embedding` → JOIN `answers` WHERE `answers.category_tags && userTags OR answers.category_tags = '{}'` → return `RetrievedQA[]`
- `SearchEventsAsync`: embed query → cosine KNN on `events.embedding` → apply category filter → return `RetrievedEvent[]`

**`IngestService`** implements `IIngestService`:
- Injects `IRepository<Document>`, `IRepository<DocumentChunk>`, `IRepository<Question>`, `IRepository<Answer>`, `IRepository<EventEntry>`
- `IngestDocumentAsync`: SHA-256 hash → skip if exists → chunk (400-token, 50 overlap via `Microsoft.ML.Tokenizers`) → embed each chunk (OpenAI embedding client) → copy `CategoryValues` to each chunk as `category_tags` → upsert
- `IngestQAAsync`: SHA-256 of question text → skip if exists → embed question → upsert question + replace all answers
- `IngestEventAsync`: upsert on `feed_entry_id` → embed title+body → save

### VectorDbContext

```csharp
public class VectorDbContext(DbContextOptions<VectorDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<EventEntry> Events => Set<EventEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("vector");
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VectorDbContext).Assembly);
    }
}
```

### VectorDbModule (DI entry point)

```csharp
public static class VectorDbModule
{
    public static IServiceCollection AddVectorDbModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        services.AddDbContext<VectorDbContext>(o =>
            o.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "vector");
                npgsql.UseVector();
            }));

        services.AddScoped(typeof(IRepository<>), typeof(VectorRepository<>));
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IIngestService, IngestService>();

        return services;
    }
}
```

### File layout

```
src/Reshape.ElectricAi.Core/
├── Core.csproj                                    ← add EF Core package ref
├── Domain/
│   └── ICategorizable.cs                          ← updated
├── Persistence/
│   ├── IRepository.cs                             ← existing
│   ├── ISpecification.cs                          ← existing
│   ├── Specification.cs                           ← existing
│   ├── EfRepository.cs                            ← MOVED from Plans
│   └── SpecificationEvaluator.cs                  ← MOVED from Plans
├── Services/
│   ├── IVectorSearchService.cs                    ← new
│   └── IIngestService.cs                          ← new
└── Dtos/
    └── VectorSearch/
        ├── RetrievedChunk.cs
        ├── RetrievedQA.cs
        ├── RetrievedAnswer.cs
        ├── RetrievedEvent.cs
        ├── DocumentSearchFilter.cs
        ├── QuestionSearchFilter.cs
        ├── EventSearchFilter.cs
        ├── IngestDocumentRequest.cs
        ├── IngestQARequest.cs
        ├── IngestAnswerRequest.cs
        └── IngestEventRequest.cs

src/Reshape.ElectricAi.Plans/
├── Persistence/
│   ├── EfRepository.cs                            ← DELETED (moved to Core)
│   └── SpecificationEvaluator.cs                  ← DELETED (moved to Core)
│   (all using directives updated to Core.Persistence namespace)

src/Reshape.ElectricAi.VectorDb/
├── Entities/
│   ├── Document.cs
│   ├── DocumentChunk.cs
│   ├── Question.cs
│   ├── Answer.cs
│   └── EventEntry.cs
├── Persistence/
│   ├── VectorDbContext.cs
│   ├── VectorDbContextFactory.cs
│   ├── VectorRepository.cs                        ← mirrors PlansRepository<T>
│   └── Configurations/
│       ├── DocumentConfiguration.cs
│       ├── DocumentChunkConfiguration.cs
│       ├── QuestionConfiguration.cs
│       ├── AnswerConfiguration.cs
│       └── EventEntryConfiguration.cs
├── Services/
│   ├── VectorSearchService.cs
│   └── IngestService.cs
├── Migrations/
│   └── (EF-generated)
└── VectorDbModule.cs
```

---

## Implementation order

1. **Core — move EfRepository + SpecificationEvaluator** — update Plans `using` directives, add EF Core to Core.csproj, verify `dotnet build` green
2. **Core — ICategorizable update + interfaces + DTOs** — all files in Core/Services/ and Core/Dtos/VectorSearch/
3. **VectorDb entities** — 5 entity classes; `ICategorizable` implemented where applicable
4. **VectorDb EF configurations** — HNSW indexes on embeddings, GIN indexes on `category_tags[]`, FK cascades, `source` stored as text
5. **VectorDbContext + VectorDbContextFactory + VectorRepository**
6. **EF migration:**
   ```
   dotnet ef migrations add InitialVectorSchema \
     -p src/Reshape.ElectricAi.VectorDb \
     -s src/Reshape.ElectricAi.Presentation \
     -- --context VectorDbContext
   ```
7. **Service implementations** — `IngestService` (uses `IRepository<T>`) then `VectorSearchService` (uses `VectorDbContext` directly)
8. **VectorDbModule** — DI wiring + `Program.cs` registration
9. **Build verification** — `dotnet build` with zero errors/warnings

---

## Out of scope

- LiveFeed SSE broadcaster and event targeting (separate dev)
- AiChat RAG orchestration (calls `IVectorSearchService` — separate dev)
- Data seeding (`data/faqs.json`, `data/rules.md`, etc.) — separate plan
- `POST /admin/ingest` controller — separate plan (Presentation layer work)
- Test projects — separate plan

---

## Key constraints (from CODE.md)

- Embedding model and dimensions from config (`Chat:EmbeddingModel`, `Chat:EmbeddingDimensions`). The migration hardcodes `vector(1536)` — changing the model requires a new migration + full re-embed.
- Chunker: `Microsoft.ML.Tokenizers` cl100k_base, 400-token target, 50-token overlap.
- No cross-context EF navigations. `EventEntry.FeedEntryId` is a loose `Guid`, not a navigation.
- Controllers live ONLY in Presentation — VectorDb exposes no controllers.
- Package installs are human-only. VectorDb `.csproj` already has all needed packages. Core needs `Microsoft.EntityFrameworkCore` added (human install).
