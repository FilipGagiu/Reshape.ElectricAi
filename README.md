# Reshape ElectricAi

> First-timer assistant for the **Electric Castle** festival — a .NET 10 Web API that turns "where do I even start?" into a personalized EC plan via free search, chat, and a guided planner. Submitted to the **EC AI Builder Challenge** (see `Client Generic Requirements/1. BRIEF - AI Builder Challenge.pdf`).

**Status:** v1 in development (hackathon). All four feature libs are planned for v1: `Plans` (auth + preferences + plan generation), `VectorDb` (RAG over EC knowledge), `AiChat` (chat with budget caps), `LiveFeed` (organizer push via SSE). No source code is committed yet — this README documents the agreed architecture so the next session and the next dev open into context.

---

## Quick start (for consumers in the FE repo)

```bash
# Backend dev (this repo)
dotnet run --project src/Reshape.ElectricAi.Presentation
# API at http://localhost:5217
# Scalar UI at http://localhost:5217/scalar/v1

# FE: send JWT in Authorization header on every authed call
fetch("http://localhost:5217/api/v1/preferences", {
  headers: { Authorization: `Bearer ${accessToken}` }
});

# SSE: EventSource can't set headers, use ?access_token=
const stream = new EventSource(
  `http://localhost:5217/api/v1/feed/stream?access_token=${accessToken}`
);
stream.addEventListener("feed.created", (e) => console.log(JSON.parse(e.data)));
```

CORS allowed origins are configured server-side via `Cors:AllowedOrigins` (see [PROJECT.md](PROJECT.md) configuration matrix).

---

## Architecture

```
┌───────────────────────┐
│   Frontend (separate  │       HTTP + SSE
│   repo, separate team)│ ────────────────────────┐
└───────────────────────┘                         │
                                                  ▼
                          ┌─────────────────────────────────────────────┐
                          │   Reshape.ElectricAi.Presentation (API)     │
                          │   Controllers · Middleware · Program.cs     │
                          └────────────────┬────────────────────────────┘
                                           │   AddXxxModule() per lib
            ┌──────────────────────────────┼──────────────────────────────┐
            ▼                              ▼                              ▼
   ┌────────────────┐            ┌──────────────────┐          ┌──────────────────┐
   │  .Plans        │            │  .AiChat         │          │  .LiveFeed       │
   │  (auth, prefs, │            │  (chat, RAG,     │          │  (organizer feed,│
   │  plan-gen)     │            │  budget)         │          │  SSE hub)        │
   └────────┬───────┘            └──┬───────────────┘          └──┬───────────────┘
            │                       │                              │
            │                       └──────────┬───────────────────┘
            │                                  ▼
            │                       ┌──────────────────┐
            │                       │  .VectorDb       │
            │                       │  (pgvector,      │
            │                       │  ingest, search) │
            │                       └──────────┬───────┘
            │                                  │
            └──────────────┬───────────────────┘
                           ▼
                  ┌──────────────────┐         ┌──────────────────┐
                  │  .Core (shared:  │         │   OpenAI API     │
                  │  entities, DTOs, │         │   (chat +        │
                  │  interfaces)     │         │   embeddings)    │
                  └──────────────────┘         └──────────────────┘
                           │
                           ▼
                  ┌────────────────────────────────────────────┐
                  │   PostgreSQL 16 + pgvector                 │
                  │   schemas: plans · vector · feed · chat    │
                  └────────────────────────────────────────────┘
```

**Boundaries:** controllers live ONLY in `Presentation`. Feature libs are self-contained vertical slices, each with its own `DbContext`, schema, and migrations. Cross-lib references are loose `Guid` IDs — no cross-context EF navigations. See [CODE.md](CODE.md) for the full rule set.

---

## REST API surface

All routes under `/api/v1`. JWT bearer required unless marked `[Anon]`. Status: **all endpoints planned for v1**; nothing implemented yet.

### Auth (`Reshape.ElectricAi.Plans`)

| Method | Route | Auth | Summary |
|---|---|---|---|
| POST | `/auth/register` | `[Anon]` | Create user (User role). Returns access + refresh tokens. |
| POST | `/auth/login` | `[Anon]` | Verify credentials. Returns access + refresh tokens. |
| POST | `/auth/refresh` | `[Anon]` (token in body) | Rotate refresh token, issue new pair. |
| GET | `/auth/me` | `[User]` | Current user (id, email, role). |

### Preferences (`Reshape.ElectricAi.Plans`)

| Method | Route | Auth | Summary | Status |
|---|---|---|---|---|
| GET | `/preferences` | `[User]` | My preferences + completion %. Empty default DTO when no row (no 404). | live |
| PUT | `/preferences` | `[User]` | Full replace. `null` scalar = clear; `null` list = treated as empty. | live |
| PATCH | `/preferences` | `[User]` | Partial update. `null` (or absent) = no change; `[]` = clear that list. | live |
| POST | `/preferences/parse-freetext` | `[User]` | AI-extract preferences from free text. Returns preview; does NOT save. | deferred (needs AiChat) |

### Groups (`Reshape.ElectricAi.Plans`)

| Method | Route | Auth | Summary |
|---|---|---|---|
| POST | `/groups` | `[User]` | Create group, current user becomes owner. |
| GET | `/groups/{id}` | `[User]` (member) | Group detail + members + aggregated prefs. |
| POST | `/groups/{id}/members` | `[User]` (owner) | Invite by email. |
| DELETE | `/groups/{id}/members/{userId}` | `[User]` (owner) | Remove member. |
| GET | `/groups/{id}/preferences` | `[User]` (member) | Aggregated group preferences. |
| PUT | `/groups/{id}/preferences` | `[User]` (member) | Replace group preferences. |

### Plans (`Reshape.ElectricAi.Plans`)

| Method | Route | Auth | Summary |
|---|---|---|---|
| GET | `/plans` | `[User]` | List my plans + group plans I'm a member of. |
| POST | `/plans/generate` | `[User]` | Generate a plan. Body: `{ scope: "individual" \| "group", groupId? }`. 422 with `state` field if prefs insufficient. |
| GET | `/plans/{id}` | `[User]` (owner/member) | Full plan. |
| GET | `/plans/{id}/export?format=md\|pdf\|json` | `[User]` (owner/member) | Download. |
| DELETE | `/plans/{id}` | `[User]` (owner) | Delete. |

### Chat (`Reshape.ElectricAi.AiChat`)

| Method | Route | Auth | Summary |
|---|---|---|---|
| GET | `/chat/hot-questions?category=&q=` | `[User]` | Curated FAQ list — pure vector retrieval, no LLM call (free). |
| POST | `/chat/sessions` | `[User]` | Create chat session. Returns session id. |
| POST | `/chat/sessions/{id}/messages` | `[User]` | Send message. Returns assistant reply + tokens + cost + citations. **402** if over daily budget. |
| GET | `/chat/sessions/{id}` | `[User]` (owner) | Session history. |
| GET | `/chat/budget` | `[User]` | Daily budget cents + spent cents + ticket tier. |

### Live Feed (`Reshape.ElectricAi.LiveFeed`)

| Method | Route | Auth | Summary |
|---|---|---|---|
| GET | `/feed?category=` | `[User]` | Recent entries (paged, last 100). |
| GET | `/feed/stream` | `[User]` (token via header **or** `?access_token=` query) | **SSE** stream. `Content-Type: text/event-stream`. |
| POST | `/feed` | `[Organizer]` | Publish entry. |
| PUT | `/feed/{id}` | `[Organizer]` | Update entry. |
| DELETE | `/feed/{id}` | `[Organizer]` | Delete entry. |

### Admin / Health (`Reshape.ElectricAi.Presentation` + `Reshape.ElectricAi.VectorDb`)

| Method | Route | Auth | Summary |
|---|---|---|---|
| GET | `/healthz` | `[Anon]` | Liveness. |
| GET | `/readyz` | `[Anon]` | Readiness (DB + OpenAI reachable). |
| POST | `/admin/ingest` | `[Organizer]` | Re-ingest knowledge sources into the vector store. |

`VectorDb` is **internal-only otherwise** — no public controllers; consumed via `IVectorSearchService` and `IIngestService` interfaces from other libs.

---

## Canonical JSON schemas

### `POST /auth/register` and `POST /auth/login` — response

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "opaque-base64-token",
  "expiresIn": 900,
  "user": {
    "id": "8e1f...-uuid",
    "email": "alice@example.com",
    "role": "User"
  }
}
```

### `PUT /preferences` — request (and `PreferencesDto` response)

```json
{
  "ticketType": "Standard",
  "accommodation": "Glamping",
  "transport": "EcBus",
  "ageGroup": "Adult25To34",
  "musicGenres": ["Techno", "House", "Rock"],
  "foodRestrictions": ["Vegan", "NoPeanuts"],
  "activities": ["Relax", "Social"],
  "artists": ["Justin Timberlake", "Queens of the Stone Age"],
  "completionPercent": 100,
  "updatedUtc": "2026-05-23T10:15:30Z"
}
```

Enum values:

| Field | Allowed values |
|---|---|
| `ticketType` | `Standard`, `Vip`, `UltraVip`, `Black` |
| `accommodation` | `VillageRental`, `Camping`, `CarCamping`, `RvCamping`, `Glamping` |
| `transport` | `RideShare`, `Car`, `EcTrain`, `EcBus`, `Helicopter` |
| `ageGroup` | `Under18`, `Adult18To24`, `Adult25To34`, `Adult35To44`, `Adult45Plus` |
| `musicGenres[]` | `HipHop`, `House`, `Balkan`, `Rock`, `Folk`, `Techno`, `Pop`, `Electronic`, `Jazz`, `Metal`, `Other` |
| `foodRestrictions[]` | `Vegan`, `Vegetarian`, `NoPeanuts`, `NoMeat`, `NoPork`, `NoDairy`, `NoGluten`, `NoShellfish`, `NoEggs`, `Halal`, `Kosher` |
| `activities[]` | `Relax`, `Energetic`, `Adrenaline`, `Social`, `Creative`, `Wellness`, `Discovery` |
| `artists[]` | free-text strings, 1..200 chars each |

Source-of-truth enums live in [`src/Reshape.ElectricAi.Core/Enums/`](src/Reshape.ElectricAi.Core/Enums/). `completionPercent` is computed server-side (8 dimensions equal weight, integer divide × 100); `updatedUtc` is server-set. Both ignored on input.

**Validation caps:** `artists` ≤ 20, `musicGenres` ≤ 11, `foodRestrictions` ≤ 11, `activities` ≤ 7. Duplicates rejected with 400 + standard envelope. `artists` dedup is case-insensitive; strings trimmed.

**PATCH semantics:** every field nullable. `null` (or absent) = no change for that field. Send `[]` on a list to clear it. Send PUT instead if you need to clear a scalar enum.

### `POST /plans/generate` — response (`PlanDto`)

```json
{
  "id": "uuid",
  "scope": "individual",
  "state": "Ready",
  "ticketType": "GeneralAccess",
  "days": [
    {
      "date": "2025-07-17",
      "transport": {
        "outbound": { "mode": "EcBus", "from": "Iulius Mall", "departLocal": "17:55" },
        "return": { "mode": "EcBus", "note": "Non-stop, last 12:00 Monday" }
      },
      "concerts": [
        { "stage": "Main Stage by Coca-Cola", "artist": "Justin Timberlake", "startLocal": "22:30", "endLocal": "23:45" }
      ],
      "activities": [
        { "name": "Castle Market by Mastercard", "note": "Explore, shop, enjoy" }
      ],
      "weatherNotes": [
        "Possible thunderstorms after 18:00 — pack a poncho and switch to waterproof shoes."
      ]
    }
  ],
  "food": [
    { "name": "Zero", "cuisine": "Vegan", "allergenFlags": [], "priceRange": "$$" }
  ],
  "budget": {
    "ticket": 12000,
    "transport": 800,
    "accommodation": 5000,
    "food": 2500,
    "drinks": 1500,
    "chaosFund": 1500,
    "total": 23300,
    "currency": "RON-cents"
  },
  "exportedAt": null
}
```

If preferences are too sparse: HTTP **422** with `{ "error": { "code": "preferences-insufficient", "message": "...", "details": { "state": "NoPrefs" | "Partial", "completionPercent": 35 } } }`.

### `POST /chat/sessions/{id}/messages` — response

```json
{
  "messageId": "uuid",
  "reply": "Best way from Bucharest is train to Cluj-Napoca then EC Train from Gara Mică to Bonțida — about half an hour. Bus from Iulius Mall runs non-stop, no schedule needed.",
  "tokens": { "prompt": 1247, "completion": 88 },
  "cost": { "cents": 12 },
  "citations": [
    { "source": "DailyRec", "sourceRef": "transportation/ec-trains", "score": 0.87 },
    { "source": "Faq", "sourceRef": "transport/bus-hours", "score": 0.81 }
  ],
  "budgetRemainingCents": 488
}
```

Over budget: HTTP **402** with `{ "error": { "code": "chat-budget-exceeded", "message": "Daily chat budget exceeded. Resets at 00:00 UTC.", "details": { "tier": "GeneralAccess", "dailyBudgetCents": 500, "spentCents": 500 } } }`.

### `GET /feed/stream` — SSE frame format

```
event: feed.created
id: 2025-07-17T18:42:11.012Z-uuid
data: {"id":"uuid","title":"Rain incoming","body":"Light shower expected around 21:00. Stages stay open.","category":"Weather","publishedUtc":"2025-07-17T18:42:11Z"}

: keepalive

event: feed.updated
id: 2025-07-17T19:10:02.118Z-uuid
data: {...}
```

The `: keepalive` line is a comment heartbeat sent every 25 seconds. Use `Last-Event-Id` on reconnect for catch-up.

---

## SSE consumer guide

```js
const stream = new EventSource(
  `${API}/api/v1/feed/stream?access_token=${accessToken}`
);

stream.addEventListener("feed.created", (e) => render(JSON.parse(e.data)));
stream.addEventListener("feed.updated", (e) => update(JSON.parse(e.data)));
stream.addEventListener("feed.deleted", (e) => remove(JSON.parse(e.data)));

stream.onerror = () => {
  // EventSource auto-reconnects; on connect we re-send the last seen id
  // via Last-Event-Id header which the server uses for catch-up.
};
```

**Query-string token note:** `?access_token=` is accepted ONLY on `/api/v1/feed/stream`. Every other endpoint rejects it (mitigates token leakage in logs).

---

## Data sources (ingested into vector store)

| Source | Origin file in `Client Generic Requirements/` | Vector `Source` enum |
|---|---|---|
| FAQs | `Examples of Questions with Answers.docx` | `Faq` |
| Festival rules + regulations | `Festival Rules and Regulations.docx` | `Rule` |
| Lineup + stages | `2025 Line-up schedule & descriptions.xlsx` | `Lineup` |
| Daily recommendations (#ECrecommends) | `Daily recommendations.docx` | `DailyRec` |
| Ticket purchase guide | `How to purchase tickets.docx` | `TicketGuide` |
| Important links | `Important Links.docx` | `Link` |
| Live organizer feed entries | created at runtime via `POST /feed` | `FeedEntry` |

Extracted to `data/` (planned, not yet seeded). Ingest is idempotent via `ContentHash`. Embedding model fixed to `text-embedding-3-small` (1536 dims) — see [CODE.md](CODE.md) "Vector DB".

---

## Auth model

- **JWT bearer**, HS256, signing key from `Auth:JwtSigningKey` (user-secrets in dev, env var in prod).
- Access token: 15 minutes (`Auth:AccessTokenMinutes`).
- Refresh token: 7 days (`Auth:RefreshTokenDays`), opaque, stored as SHA-256 hash in `plans."RefreshTokens"`. Rotated on refresh, old token revoked.
- Password hashing: BCrypt work factor 12 + per-user salt. Login runs verify even when user not found (constant-time).
- Roles: `User`, `Organizer`. Organizer role granted manually in DB (v1 limitation, see [PROJECT.md](PROJECT.md)).

---

## Error envelope (every non-2xx)

```json
{
  "error": {
    "code": "kebab-case-code",
    "message": "Human-readable English message.",
    "details": { /* optional, e.g. field-level validation errors */ }
  }
}
```

Standard codes the FE can branch on: `validation-failed`, `unauthorized`, `forbidden`, `not-found`, `conflict`, `preferences-insufficient`, `chat-budget-exceeded`, `internal-error`.

---

## External dependencies

| Service | Use | Failure mode |
|---|---|---|
| OpenAI API | chat + embeddings | typed `LlmException` → 502 with `code: "llm-unavailable"` after 2 retries |
| PostgreSQL + pgvector | all persistence + vector search | 503 from `/readyz` if unreachable |

No other neighbor services in v1.

---

## Versioning

All routes live under `/api/v1`. Breaking changes will move to `/api/v2` and `/api/v1` will continue to work in parallel for a deprecation window. v1-only is the current reality.

---

## Useful links inside the repo

- **[CLAUDE.md](CLAUDE.md)** — workflow, plan-mode discipline, memory rules (read every session start)
- **[CODE.md](CODE.md)** — code rulebook (re-read before every code edit)
- **[PROJECT.md](PROJECT.md)** — layout, commands, configuration matrix, known limitations
- **`Client Generic Requirements/1. BRIEF - AI Builder Challenge.pdf`** — the original challenge brief
- **`Client Generic Requirements/Examples of Questions with Answers.docx`** — tone of voice + sample Q&A
- **`Client Generic Requirements/Daily recommendations.docx`** — #ECrecommends + vendors + stages
- **`Client Generic Requirements/Festival Rules and Regulations.docx`** — festival rules
- **`Client Generic Requirements/Design Proposal.png`** — UI mockup from the brief
