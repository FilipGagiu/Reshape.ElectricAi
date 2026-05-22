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

All three need vector search. Domains 2 and 3 need **category-based filtering** so that retrieval respects user context (e.g., a camper gets camping-relevant answers; a VIP gets VIP-specific events).

---

## Decisions

### ICategorizable interface

The existing `ICategorizable` in Core returns `IReadOnlyCollection<Category>` (category types only). This is insufficient for filtering — we need the *values* within each category (e.g. which `TransportMode`, which accommodation type, which artists).

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

Storing the dict as JSONB makes intersection queries awkward. Instead, flatten to a **namespaced `text[]` column** with a GIN index:

```
category_tags text[] NOT NULL DEFAULT '{}'
```

Format: `{"Transport.EcBus","Accommodation.Camping","Lineup.Justin Timberlake"}`

Overlap query:
```sql
WHERE category_tags && ARRAY['Transport.EcBus','Accommodation.Camping']
OR category_tags = '{}'   -- empty = general, matches everyone
```

`ICategorizable.CategoryValues` is reconstructed from tags at read time by splitting on the first `.`.

### Three search domains, three tables

| Domain | Tables | Filtering |
|---|---|---|
| Official EC docs | `documents` + `document_chunks` | Source enum only (no user context) |
| Q&A | `questions` + `answers` | User context on `answers.category_tags` |
| Organizer events | `events` | User context on `events.category_tags` |

Q&A retrieval: KNN on `questions.embedding` → JOIN `answers` WHERE `answers.category_tags && userTags OR answers.category_tags = '{}'`.

---

## Section 1: Core changes

### Files changed / added

| File | Change |
|---|---|
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

// Search filters
public record DocumentSearchFilter(string[]? Sources = null, int TopK = 6);

public record QuestionSearchFilter(
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 6);

public record EventSearchFilter(
    IReadOnlyDictionary<Category, IReadOnlyList<string>>? UserContext = null,
    int TopK = 6);

// Ingest requests
public record IngestDocumentRequest(string Source, string SourceRef, string Content);
// IngestService computes ContentHash internally (SHA-256 of Content)

public record IngestAnswerRequest(
    string Text,
    IReadOnlyDictionary<Category, IReadOnlyList<string>> CategoryValues);

public record IngestQARequest(
    string SourceRef, string QuestionText,
    IReadOnlyDictionary<Category, IReadOnlyList<string>> QuestionCategoryValues,
    IReadOnlyList<IngestAnswerRequest> Answers);

public record IngestEventRequest(
    Guid FeedEntryId, string Title, string Body,
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

### Table: `vector.document_chunks`

| Column | Type | Notes |
|---|---|---|
| `id` | `uuid` PK | |
| `document_id` | `uuid FK → documents` | CASCADE delete |
| `chunk_index` | `int NOT NULL` | 0-based position in parent document |
| `text` | `text NOT NULL` | Raw chunk text (400-token target, 50-token overlap) |
| `embedding` | `vector(1536)` | HNSW index (cosine). Populated by `IngestService` |

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
| `category_tags` | `text[] NOT NULL DEFAULT '{}'` | GIN index. Empty = general (matches all users) |
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

Deduplication: `feed_entry_id` has a UNIQUE constraint. `IngestEventAsync` is idempotent — upsert on `feed_entry_id`.

---

## Section 3: Services and implementation structure

### VectorDb entities → C# names

| DB table | Entity class |
|---|---|
| `vector.documents` | `Document` |
| `vector.document_chunks` | `DocumentChunk` |
| `vector.questions` | `Question` |
| `vector.answers` | `Answer` |
| `vector.events` | `EventEntry` |

All entities with `category_tags` implement `ICategorizable` by reconstructing the dict from tags.

### Implementation services

**`VectorSearchService`** implements `IVectorSearchService`:
- `SearchDocumentsAsync`: embed query → cosine KNN on `document_chunks.embedding`, optional Source filter → return `RetrievedChunk[]`
- `SearchQuestionsAsync`: embed query → cosine KNN on `questions.embedding`, then `LEFT JOIN answers WHERE answers.category_tags && userTags OR answers.category_tags = '{}'` → return `RetrievedQA[]`
- `SearchEventsAsync`: embed query → cosine KNN on `events.embedding`, apply `events.category_tags && userTags` filter → return `RetrievedEvent[]`

**`IngestService`** implements `IIngestService`:
- `IngestDocumentAsync`: SHA-256 content hash → skip if exists → chunk (400-token, 50 overlap via `Microsoft.ML.Tokenizers`) → embed each chunk (OpenAI `text-embedding-3-small`) → upsert `documents` + `document_chunks`
- `IngestQAAsync`: SHA-256 of question text → skip if exists → embed question → upsert `questions` + replace `answers`
- `IngestEventAsync`: upsert on `feed_entry_id` → embed title+body → upsert `events`

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
        var connectionString = configuration.GetConnectionString("Postgres")!;
        services.AddDbContext<VectorDbContext>(o =>
            o.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "vector");
                npgsql.UseVector();
            }));
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IIngestService, IngestService>();
        return services;
    }
}
```

### File layout

```
src/Reshape.ElectricAi.Core/
├── Domain/
│   └── ICategorizable.cs                          ← updated
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

1. **Core changes** — ICategorizable update + IVectorSearchService + IIngestService + all DTOs
2. **VectorDb entities** — 5 entity classes with `ICategorizable` where applicable
3. **VectorDb EF configurations** — fluent API: columns, indexes (HNSW on embeddings, GIN on `category_tags[]`), FK cascades
4. **VectorDbContext + VectorDbContextFactory**
5. **EF migration** — `dotnet ef migrations add InitialVectorSchema -p src/Reshape.ElectricAi.VectorDb -s src/Reshape.ElectricAi.Presentation -- --context VectorDbContext`
6. **Service implementations** — VectorSearchService + IngestService (full implementations, not stubs)
7. **VectorDbModule** — DI wiring + register in `Program.cs`
8. **Build verification** — `dotnet build` must pass with zero errors/warnings

---

## Out of scope

- LiveFeed SSE broadcaster and event targeting (separate dev)
- AiChat RAG orchestration (calls `IVectorSearchService` — separate dev)
- Data seeding (`data/faqs.json`, `data/rules.md`, etc.) — separate plan
- `POST /admin/ingest` controller — separate plan (Presentation layer work)
- Test projects — to be addressed in a follow-up plan

---

## Key constraints (from CODE.md)

- Embedding model fixed: `text-embedding-3-small` (1536 dims). Do not change.
- Chunker: `Microsoft.ML.Tokenizers` cl100k_base, 400-token target, 50-token overlap.
- No cross-context EF navigations. `EventEntry.FeedEntryId` is a loose `Guid`, not a navigation.
- `Controllers live ONLY in Presentation` — VectorDb exposes no controllers.
- Package installs are human-only. VectorDb `.csproj` already has all needed packages (EF Core, Npgsql, Pgvector, OpenAI, ML.Tokenizers).
