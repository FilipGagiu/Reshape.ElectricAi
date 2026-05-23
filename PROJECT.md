# PROJECT.md — Project Context

> **Read at session start alongside CODE.md and README.md (CLAUDE.md mandate).** This file describes the project — layout, commands, data shape pointers, navigation. It is NOT a code rulebook (that's [CODE.md](CODE.md)) and NOT a public catalog (that's [README.md](README.md)).

---

## What this is

A .NET 10 Web API backend for the **Electric Castle AI Builder Challenge** — a first-timer assistant that helps people who have never been to the Electric Castle festival plan their first trip. The brief lives in `Client Generic Requirements/1. BRIEF - AI Builder Challenge.pdf`. The high-level pitch: turn first-timer panic (transport, sleep, weather, line-up, budget, group decision) into a guided, personalized plan via free search, chat, and a 4-5 question planner — backed by an approved EC knowledge base.

The frontend is built by a separate team in a separate repo. This repo is **backend only**.

---

## Team + ownership

Three developers working in parallel, each owning one or two class libraries. Each lib is independent enough that PRs rarely conflict at the project/migrations level (different schemas, different `DbContext`s, separate folders).

| Owner | Project(s) | Responsibilities |
|---|---|---|
| Dev 1 | `Reshape.ElectricAi.Plans` + shared `Reshape.ElectricAi.Core` | Users, Auth (JWT issuance, register/login/refresh), Preferences (individual + group), Plan generation (per ticket type + state machine) |
| Dev 2 | `Reshape.ElectricAi.AiChat` + `Reshape.ElectricAi.VectorDb` | Vector ingest + retrieval (pgvector), OpenAI client wrapper, chat orchestration, RAG, daily budget enforcement, hot questions |
| Dev 3 | `Reshape.ElectricAi.LiveFeed` | Organizer feed CRUD, SSE channel hub, personalization targeting, indexing feed entries into vector store |

Presentation (`Reshape.ElectricAi.Presentation`) is **shared infrastructure** — controllers, middleware, `Program.cs`, `appsettings.json`. Every dev adds their controllers + their `AddXxxModule()` line. Merge conflicts here should be tiny (one-line additions).

The above assignment is a recommendation — the team confirms or rearranges at the kickoff. Update this table when locked.

---

## Solution layout

> **Status: scaffolded; Plans auth + preferences (incl. cuisines) slices landed.** Solution file is `ElectricCastle.slnx` (XML solution format). All six projects exist. `Plans` has entities + migrations (InitialPlansSchema + AddPushSubscriptions + AddPreferenceCuisines) + auth (register/login/refresh/me) + preferences (GET/PUT/PATCH `/api/v1/preferences` across 9 dimensions) + generic repository abstraction. Test project `Plans.Tests` has 32 base passing tests + 18 preferences integration tests (Docker-gated). Other libs are empty scaffolds.

```
ElectricCastle/
├── ElectricCastle.slnx                         (exists) XML solution format (replaces classic .sln)
├── src/
│   ├── Reshape.ElectricAi.Presentation/        (exists) API host, controllers, middleware, Program.cs, appsettings
│   ├── Reshape.ElectricAi.Core/                (exists) shared entities, DTOs, interfaces, enums, exceptions, persistence + config abstractions
│   ├── Reshape.ElectricAi.Infrastructure/      (exists) EfRepository<TContext,T> + SpecificationEvaluator (consumed by Plans + LiveFeed)
│   ├── Reshape.ElectricAi.Plans/               (auth slice live) Auth + Users + (Preferences/Plan generation TODO)
│   ├── Reshape.ElectricAi.VectorDb/            (scaffold) pgvector access, ingest pipeline, retrieval
│   ├── Reshape.ElectricAi.LiveFeed/            (scaffold) organizer feed + SSE channel state
│   └── Reshape.ElectricAi.AiChat/              (scaffold) chat, RAG, OpenAI wrapper, budget
├── tests/
│   ├── Reshape.ElectricAi.Plans.Tests/         (exists) xUnit + FluentAssertions + Testcontainers.PostgreSql + Mvc.Testing; 32 tests
│   ├── Reshape.ElectricAi.VectorDb.Tests/      (planned)
│   ├── Reshape.ElectricAi.LiveFeed.Tests/      (planned)
│   └── Reshape.ElectricAi.AiChat.Tests/        (planned)
├── data/                                       (planned) seed knowledge: FAQs, rules, lineup, daily recs (MD/JSON)
├── .claude/                                    (exists) hooks, plans, settings, skills
├── Client Generic Requirements/                (exists) brief PDF + source DOCX/XLSX/PNG
├── CLAUDE.md                                   (exists) Claude's workflow + rules
├── CODE.md                                     (exists) code rulebook
├── PROJECT.md                                  (exists) this file
├── README.md                                   (exists) public catalog
├── nuget.config                                (exists) clears global feeds, adds nuget.org only
├── .editorconfig                               (planned, follow-up)
└── .gitignore                                  (exists)
```

**Persistence layer.** Generic EF repository (`EfRepository<TContext, T>`, `SpecificationEvaluator`) lives in `Reshape.ElectricAi.Infrastructure/Persistence/`. Each feature lib that needs EF persistence references `Infrastructure` and adds a closing class (`PlansRepository<T>`, `FeedRepository<T>`, etc.). Abstractions (`IRepository<T>`, `ISpecification<T>`, `Specification<T>`) live in Core.

**Project dependency graph (acyclic — enforced by reviewers + CODE.md):**

```
Presentation    →  Plans, VectorDb, LiveFeed, AiChat, Core, Infrastructure
Plans           →  Core, Infrastructure
LiveFeed        →  Core, Infrastructure, VectorDb
AiChat          →  Core, VectorDb
VectorDb        →  Core
Infrastructure  →  Core
Core            →  (nothing project-level)
```

Each lib exposes one DI entry point: `public static IServiceCollection AddXxxModule(this IServiceCollection s, IConfiguration c)`. Presentation's `Program.cs` calls all four — single mergeable line per dev.

---

## Data model overview

One Postgres database (`electric_ai`), four schemas, one `DbContext` per lib (CODE.md "DbContext + migrations").

| Schema | Owner | Key tables |
|---|---|---|
| `plans` | Plans lib | `Users`, `RefreshTokens`, `UserPreferences`, `UserPreferenceGenres`, `UserPreferenceFoodRestrictions`, `UserPreferenceActivities`, `UserPreferenceArtists`, `UserPreferenceCuisines`, `Groups`, `GroupMembers`, `GroupPreferences`, `GroupPreferenceGenres`, `GroupPreferenceFoodRestrictions`, `GroupPreferenceActivities`, `GroupPreferenceArtists`, `Plans` (PascalCase identifiers — Postgres double-quoting required in `psql`) |
| `vector` | VectorDb lib | `documents`, `document_chunks` (with `embedding vector(1536)` + HNSW index) |
| `feed` | LiveFeed lib | `feed_entries`, `feed_deliveries` |
| `chat` | AiChat lib | `chat_sessions`, `chat_messages`, `chat_budgets`, `faq_hot_questions` |

**Cross-schema references are loose `Guid` IDs** (e.g. `chat.chat_messages.UserId` does NOT declare a navigation property to `plans."Users"`). EF Core can't enforce FKs across `DbContext`s, so referential integrity is application-level. CODE.md mandates this.

For endpoint shapes + JSON schemas, see [README.md](README.md). For ORM and migrations rules, see [CODE.md](CODE.md).

---

## Build, test, run

> **Commands valid once the solution is scaffolded.** Run them from the repo root.

| Goal | Command |
|---|---|
| Restore + build everything | `dotnet build` |
| Run all tests | `dotnet test` |
| Run a specific lib's tests | `dotnet test tests/Reshape.ElectricAi.Plans.Tests` |
| Run the API locally | `dotnet run --project src/Reshape.ElectricAi.Presentation` |
| Add a migration in a feature lib | `dotnet ef migrations add <Name> -p src/Reshape.ElectricAi.Plans -s src/Reshape.ElectricAi.Presentation -- --context PlansDbContext` |
| Apply migrations to local Postgres | `dotnet ef database update -p src/Reshape.ElectricAi.Plans -s src/Reshape.ElectricAi.Presentation -- --context PlansDbContext` (repeat per `DbContext`) |
| Set OpenAI key in dev | `dotnet user-secrets set "OpenAi:ApiKey" "sk-..." --project src/Reshape.ElectricAi.Presentation` |
| Set JWT signing key in dev | `dotnet user-secrets set "Auth:JwtSigningKey" "$(openssl rand -base64 48)" --project src/Reshape.ElectricAi.Presentation` |

Default Kestrel port: `5217` (configurable in `Properties/launchSettings.json`). Scalar UI at `http://localhost:5217/scalar/v1`.

---

## Local prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` should report `10.x` |
| PostgreSQL | 16+ | with `pgvector` extension installed (`CREATE EXTENSION vector;` in the `electric_ai` database) |
| OpenAI account | — | API key with credits, set via `dotnet user-secrets` |
| Git | any recent | repo not initialized yet — `git init` when the team decides |

Optional: `psql` CLI for ad-hoc SQL, `pgAdmin` or DataGrip for browsing.

---

## Configuration matrix

Every required key in `appsettings.json` (or user-secrets / environment variable override). Defaults marked `*`.

| Key | Example | Notes |
|---|---|---|
| `ConnectionStrings:Postgres` | `Host=localhost;Database=electric_ai;Username=postgres;Password=postgres` | per-dev local Postgres |
| `Auth:JwtSigningKey` | 32+ random bytes, base64 | **user-secrets in dev**, env var in prod |
| `Auth:AccessTokenMinutes` | `15`* | short-lived |
| `Auth:RefreshTokenDays` | `7`* | rotation on refresh |
| `Auth:SingleSession` | `false`* | when `true`, login revokes prior refresh tokens |
| `OpenAi:ApiKey` | `sk-...` | **user-secrets**, never committed |
| `OpenAi:Limits:MaxPromptTokens` | `8000`* | safety ceiling |
| `OpenAi:Limits:MaxCompletionTokens` | `1024`* | per-call cap |
| `OpenAi:Limits:TimeoutSeconds` | `30`* | per-call |
| `OpenAi:Models:<name>:PromptCentsPer1K` | e.g. `0.015` for `gpt-4o-mini` | check current pricing on each rev |
| `OpenAi:Models:<name>:CompletionCentsPer1K` | e.g. `0.060` for `gpt-4o-mini` | check current pricing on each rev |
| `Chat:DefaultModel` | `gpt-4o-mini`* | overridable per request later |
| `Chat:EmbeddingModel` | `text-embedding-3-small`* | **do not change** without re-embedding |
| `Chat:Tiers:None:DailyBudgetCents` | `200`* | ~2 RON |
| `Chat:Tiers:GeneralAccess:DailyBudgetCents` | `500`* | |
| `Chat:Tiers:Premium:DailyBudgetCents` | `1000`* | |
| `Chat:Tiers:Vip:DailyBudgetCents` | `2500`* | |
| `VectorDb:AutoIngest` | `true`* dev / `false` prod | re-ingest `data/` sources on startup |
| `VectorDb:SourcesDir` | `data/` | knowledge files root |
| `Cors:AllowedOrigins` | `["http://localhost:3000"]` | FE dev origins |
| `Logging:LogLlmPayloads` | `false`* | when `true` enables DEBUG logging of LLM bodies — never in prod |

---

## Knowledge sources

The `data/` folder (planned, not yet seeded) holds the knowledge base ingested into the vector store. Source files come from `Client Generic Requirements/`:

| `data/` file (planned) | Source | Vector `Source` enum |
|---|---|---|
| `data/faqs.json` | extracted from `Examples of Questions with Answers.docx` (Q→A pairs) | `Faq` |
| `data/rules.md` | extracted from `Festival Rules and Regulations.docx` | `Rule` |
| `data/lineup.json` | extracted from `2025 Line-up schedule & descriptions.xlsx` (artist + stage + time + tags) | `Lineup` |
| `data/daily-recs.md` | extracted from `Daily recommendations.docx` (#ECrecommends per day + tone) | `DailyRec` |
| `data/tickets-guide.md` | extracted from `How to purchase tickets.docx` | `TicketGuide` |
| `data/links.json` | extracted from `Important Links.docx` | `Link` |
| `data/tone-of-voice.md` | extracted/written from the brief's tone guidance + daily-recs voice | (system prompt only — not ingested) |
| (live, in DB) | `feed.feed_entries` published by organizers | `FeedEntry` |

Ingest is idempotent via `ContentHash` (CODE.md "Vector DB"). Re-running ingest on unchanged files is a no-op.

The original DOCX/XLSX/PDF files stay in `Client Generic Requirements/` as source-of-truth references — only the extracted MD/JSON in `data/` is what the ingest pipeline reads.

---

## Plan + handoff files

- **Plan files: `.claude/plans/<slug>.md`** — written before any code edit (CLAUDE.md Phase 5), deleted after the work ships (Phase 10). Code + commits are the source of truth post-merge.
- **Handoff: `.claude/docs/STATE.md`** — written ONLY by the `Senior Project Manager` subagent, ONLY when the user explicitly asks for an end-of-session handoff. Reset to `_No task queued._` when the next session ships it.
- The `.claude/docs/` directory does not exist yet. CLAUDE.md says Claude should "offer to scaffold the missing files with their canonical headers and wait for explicit user confirmation." Don't auto-create.

---

## Known limitations (v1 / hackathon)

- **Ticket tier is self-declared** by users — no integration with the real EC ticketing system. Budget caps use the self-declared tier. Bypass by lying about your tier is possible. Documented for the demo; production would resolve from ticket API.
- **Organizer role is set manually in DB** (`UPDATE plans."Users" SET "Role"='Organizer' WHERE "Email"='...'`). No promotion UI in v1.
- **No email verification, no password reset.** Hackathon shortcut. Users typo their email = they lose the account.
- **SSE channels are in-memory.** Single-instance deployment only. Horizontal scaling would need Redis pub/sub or a SignalR backplane.
- **No real ticket payment** — `POST /plans/generate` returns a budget estimate, doesn't link to checkout.
- **No i18n.** English-only API responses. Tone of voice draws from EC's RO+EN content but assistant output defaults to English unless the user writes in Romanian (LLM handles language matching).
- **Plan export to PDF** uses a minimal Markdown→PDF library if added (NuGet, ask first per CODE.md). JSON export is the safe default; PDF can stay "future."
- **No rate limiting beyond per-IP and per-user fixed windows** — abuse mitigation is shallow.

---

## Navigation pointers

- **Code rules** → [CODE.md](CODE.md)
- **API surface, JSON schemas, FE integration guide** → [README.md](README.md)
- **Workflow + plan-mode discipline** → [CLAUDE.md](CLAUDE.md)
- **Brief + challenge requirements** → `Client Generic Requirements/1. BRIEF - AI Builder Challenge.pdf`
- **Tone of voice + example questions** → `Client Generic Requirements/Examples of Questions with Answers.docx`
- **Daily recommendations + vendors + stages** → `Client Generic Requirements/Daily recommendations.docx`
- **Festival rules** → `Client Generic Requirements/Festival Rules and Regulations.docx`
- **Lineup spreadsheet** → `Client Generic Requirements/2025 Line-up schedule & descriptions.xlsx`
- **Design proposal mockup** → `Client Generic Requirements/Design Proposal.png`

---

## Known follow-ups (next plans)

1. ~~**Scaffolding plan**~~ — DONE. `ElectricCastle.slnx` + six `.csproj` + `Plans.Tests` test project all exist.
2. **Knowledge-base seeding plan** — extract `Client Generic Requirements/*` to the `data/` folder in the documented JSON/MD shapes; wire the ingest source classes.
3. **Plans next slices** — ~~Preferences endpoints (GET/PUT/PATCH)~~ DONE. Remaining: `POST /preferences/parse-freetext` (deferred until AiChat), Groups CRUD + invites + group preferences (mirror user-preferences pattern with `GroupId` PK + member check), Plan generation (LLM-backed). All use the existing `IRepository<T>` + `ISpecification<T>` foundation. Atomic multi-row ops go in dedicated stores (mirroring `RefreshTokenStore`).
4. **Promote `EfRepository<TContext,T>` to a shared `Infrastructure` project** — trigger: when the second feature lib (LiveFeed / AiChat / VectorDb) needs EF persistence. Move `EfRepository`, `SpecificationEvaluator`, and adopt a per-lib closing-class pattern (e.g. `FeedRepository<T> : EfRepository<FeedDbContext, T>`).
3. **Plans next slices** — Preferences endpoints (GET/PUT/PATCH + parse-freetext), Groups CRUD + invites, Plan generation (LLM-backed). All use the existing `IRepository<T>` + `ISpecification<T>` foundation. Atomic multi-row ops go in dedicated stores (mirroring `RefreshTokenStore`).
4. ~~**Promote `EfRepository<TContext,T>` to a shared `Infrastructure` project**~~ — DONE alongside the LiveFeed initial slice. `EfRepository` + `SpecificationEvaluator` now live in `Reshape.ElectricAi.Infrastructure`; Plans + LiveFeed consume via closing classes (`PlansRepository<T>`, `FeedRepository<T>`).
5. **Per-lib feature plans (other devs)** — VectorDb (ingest + retrieval), AiChat (chat + RAG + budget), LiveFeed (CRUD + SSE). Each follows the Plans pattern: entities → migration → `XxxModule.AddXxxModule()` → controllers → tests.
6. **PM-agent handoff scaffolding** — offer to create `.claude/docs/` + `STATE.md` + `todo.md` per CLAUDE.md bootstrap.
7. **`xmin` concurrency token on `RefreshToken`** — currently rotation is atomic via `ExecuteUpdateAsync` (no xmin needed). If we ever revert to load-mutate-save for refresh tokens, add `xmin` to match the other entities. Low priority.
