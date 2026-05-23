# Plan: Main Feed Redesign (UX/UI only, no implementation)

> **Scope:** redesign the `LiveFeedComponent` (`/` route inside the mobile layout — the app's main page). UX/UI direction only; no code edits in this pass. Source agents: ux-designer + ui-designer dispatched in parallel; both reports synthesized below.

## Non-negotiable phases (restated verbatim per CLAUDE.md top of file)

1. Invoke task-specific superpowers skill(s) — match the task to a skill from §7. Fire BEFORE entering plan mode.
2. Enter plan mode (`EnterPlanMode`) — before ANY file edit.
3. Inventory / explore — gather facts via Explore agents or direct reads.
4. Design — propose specific custom agents for review, exploration, or design feedback.
5. Write the plan to `.claude/plans/<slug>.md`. Every plan MUST start by restating this phase list verbatim.
6. `ExitPlanMode` — the single approval gate.
7. Execute — YOU edit the files; only dispatch agents for review. **Re-read CODE.md before each code edit.**
8. Verify — build + tests + visible evidence.
9. Promote learnings to memory — `/si:remember`; direct-edit CODE.md / CLAUDE.md / PROJECT.md for enforced rules.
10. Delete the plan file — last step.

**Phase 1 application note for this plan:** task is FE design (UX/UI), not code modification. `superpowers:brainstorming` would be the closest match but agents already executed the brainstorm. Skipping. Phase 2 (`EnterPlanMode`) is skipped because no file edits land this pass; the plan file itself is the deliverable. Phase 7 is deferred to a follow-up plan when implementation is scoped.

---

## 1. Context recap

### Backend reality (Explore agent verified at `src/Reshape.ElectricAi.LiveFeed/`)

- `FeedEntry`: `Title` ≤200, `Body` ≤4000, single `PrimaryCategory` from 12 enums, `IsGeneral`, `TargetArtists[]`, `TargetGenres[]`, `PublishedUtc`, `UpdatedUtc?`, soft-delete.
- **12 categories:** `General, Transport, Accommodation, Food, Music, Lineup, Activity, Weather, Rules, Ticket, Safety, Health`.
- **Targeting:** organizer publishes with target artists/genres; user prefs filter inbound. `IsGeneral=true` = reaches all.
- **NO** severity/priority field. NO image attachment. NO author. NO location. NO user-generated content. NO comments.
- SSE pushes `feed.created / feed.updated / feed.deleted` with Last-Event-ID replay (10-entry cursor).
- FeedController exposes `GET /feed?category=`, `GET /feed/stream`, organizer-only `POST/PUT/DELETE`.

### Current FE (Angular `live-feed.component.html`)

- Mock data only, 7 invented client-side kinds (`NowPlaying, UpNext, Alert, ScheduleChange, Photo, Info, Social`). **5 of 7 have no backend source.**
- Header has a "Send demo notification" debug button shipping to production UI.
- All English; project i18n is EN+RO via Transloco — feed bypasses it.
- Generic `bg-surface-50` PrimeUI shell instead of EC tokens (`--ec-dark-navy`, `--ec-off-white`).
- Mobile layout bottom-nav: 3 tabs — Live Feed (`/`), Questions (`/questions`), Plan (`/plan`).

### User context

At-festival, on a phone, possibly tipsy/drunk, glancing 1-2 seconds per session. Stressed when practical things fail (transport, weather, lost friend, missed artist). One hand free. Low signal pockets. Decisions must be obvious; copy glanceable; taps forgiving.

---

## 2. UX direction

### 2.1 Priority order (top to bottom)

1. Safety / Health (high urgency)
2. Weather (high urgency)
3. Transport (medium urgency, irreversible failure mode: missed shuttle)
4. Lineup change affecting saved artists/genres
5. General urgent (Rules, Ticket)
6. Personalized lineup info (Music, Activity matching prefs)
7. General info (Food, Accommodation, General)

Rendering is **chronological within priority tier**, not strict sort. The sticky "right now" capsule handles the single-most-urgent surface (see 2.3).

### 2.2 Filter chips (consolidated from 12 to 5)

| Chip EN / RO | Maps to backend categories |
|---|---|
| All / Toate | (default, no filter) |
| Urgent / Urgent | Safety, Health, Weather |
| Schedule / Program | Music, Lineup, Activity |
| Getting around / Transport | Transport, Accommodation |
| General / General | Food, Rules, Ticket, General |

A sixth optional chip: **For you / Pentru tine** — filters to `IsGeneral=false` entries that intersect user prefs (artists/genres). Off by default.

Single-select. Horizontal scroll. Default "All".

### 2.3 Sticky "right now" capsule (client-derived, NO new endpoint)

Computed signal over the existing feed:

1. Highest unread Safety/Health/Weather entry (last 30 min). If none →
2. Highest-priority Transport entry tagged "shuttle"/"gate"/"close" keyword in title. If none →
3. Most recent entry targeting user's saved artists/genres. If none → capsule hidden.

Tap → scroll to entry in list. Dismiss on read (session-local, no persistence v1).

### 2.4 Per-card hierarchy (1-2 second glance)

Row 1: category badge (icon + label, accent color) · timestamp (right)
Row 2: title (`--type-h3`, max 2 lines)
Row 3: body (`--type-body` at 80% opacity, max 3 lines, ellipsis)

**One tap = expand body inline.** No navigation away. Transport / Safety categories may expose a secondary CTA inside expanded view (e.g. "See full schedule" deep-link to `/plan`).

### 2.5 Urgency derivation (no backend severity field)

| Tier | Categories | Visual cue |
|---|---|---|
| High | Safety, Health, Weather | 6 px EC Red (Safety/Health) or amber (Weather) left stripe + tinted icon square |
| Medium | Transport, Music, Lineup, Ticket | 4 px accent-color left stripe |
| Low | General, Food, Accommodation, Activity, Rules | 4 px gray-mid left stripe |

### 2.6 SSE arrival pattern

**Pick: "X new updates" banner**, not auto-insert. Twitter/X pattern. Sticky bar below header pulls down on `feed.created`. Taps to flush. Scroll position preserved. `feed.updated`/`feed.deleted` mutate silently.

Rationale: a card inserting itself while a tipsy user is reading mid-list is disorienting. Deliberate flush respects user agency.

### 2.7 Drunk-mode rules

- 48 × 48 px min tap target. Entire card row tappable, not just title.
- Title ≤60 chars (truncate). Body preview ≤80 chars / 2 lines.
- No confirmation modals for v1 (no dismiss/delete UI). If added later → undo toast (5s), not modal.
- No pull-to-refresh. SSE handles freshness; banner handles awareness.

---

## 3. UI direction

### 3.1 Surface

Dark navy (`--ec-dark-navy`) page background. Off-white (`--ec-off-white`) cards. Mirrors §17 EC App pattern + §15 dark-mode primary aesthetic. Establishes festival-night immersive feel.

### 3.2 Page chrome

- **Header strip:** 56 px, `--ec-red` background. Left: EC monogram (positive). Center-left: stacked "LIVE FEED" `--type-label` + "Bonțida · Electric Castle" `--type-body-sm` (white at 70%). Right: 6 × 6 px square live dot + "LIVE" label inside a 48 × 48 px tap target. Disconnected state → amber dot + "OFFLINE". Sticky `top: 0`, `z-index: var(--z-sticky)`.
- **Filter chip row:** 48 px tall, `--ec-dark-navy` background, horizontal scroll, sticky below header.
- **Pinned capsule** (when populated): full-width card, `--ec-red` background, white text, yellow outlined secondary CTA. Lives between filter chips and the scroll list. Sticky disabled (scrolls with content; once dismissed for the session, gone).

### 3.3 Category badge / icon table

| Category | PrimeIcon | Accent | EN | RO | Urgency |
|---|---|---|---|---|---|
| General | `pi-info-circle` | `--ec-info` blue | General | General | low |
| Transport | `pi-car` | `--ec-info` blue | Transport | Transport | medium |
| Accommodation | `pi-home` | `--ec-info` blue | Stay | Cazare | low |
| Food | `pi-shopping-cart` | `--ec-success` green | Food & Drink | Mâncare | low |
| Music | `pi-music` | `--ec-yellow` | Music | Muzică | medium |
| Lineup | `pi-star` | `--ec-yellow` | Line-Up | Line-Up | medium |
| Activity | `pi-bolt` | `--ec-yellow` | Activity | Activitate | low |
| Weather | `pi-cloud` | `--ec-warning` amber | Weather | Vreme | high |
| Rules | `pi-shield` | `--ec-gray-mid` | Rules | Regulament | low |
| Ticket | `pi-ticket` | `--ec-info` blue | Tickets | Bilete | medium |
| Safety | `pi-exclamation-triangle` | `--ec-red` | Safety | Siguranță | high |
| Health | `pi-heart` | `--ec-error` red | Health | Sănătate | high |

Pass colors (`--ec-pass-*`) MUST NOT be used here — reserved per visual §10 for ticket cards.

### 3.4 Card anatomy

**Canonical card** (most categories):

```
┌──────────────────────────────────────────┐
│ ▍ [icon20]  LINEUP        · 2 min ago    │
│  Boris Brejcha on Main Stage             │
│  Doors open at 22:00. Get there early.   │
└──────────────────────────────────────────┘
```

`relative flex gap-3 bg-[var(--ec-off-white)] shadow-[var(--shadow-1)] p-[var(--space-4)] border-l-4 border-l-[<accent>]`. Square corners. Dark navy text on the off-white card.

**High-urgency card** (Safety/Health/Weather):

- 6 px (not 4 px) left stripe.
- Icon sits inside a 32 × 32 px square with `bg-[<accent>]/10` tint.
- No background wash, no border changes elsewhere.

**Pinned "right now" card** (client-derived):

- Full-bleed `--ec-red` background, white text.
- "NOW" label + bold artist/event name (`--type-h3` white) + meta line (`--type-body-sm` white at 70%) + yellow outlined CTA ("GET THERE" / "AJUNGE ACOLO") 36 px.

### 3.5 Filter chip spec

- Unselected: `--ec-dark-navy` bg, 1 px white border at 30% opacity, white `--type-label` text. Height 32 px (48 px hit via container padding). Square.
- Selected: `--ec-yellow` bg, `--ec-dark-navy` text. No border. Mirrors §06 active-nav rule.

### 3.6 Empty / loading / connection-lost

- **Empty (no matches):** `pi-bell-slash` 48 px gray-mid + `--type-h3` title + `--type-body-sm` body (copy below).
- **Skeleton:** 4 × 72 px stripe block + 160 × 18 px title block + 240 × 28 px body block. Pulse `opacity 0.6 ↔ 0.9` over 1200 ms. 200 ms delay before show.
- **Disconnected indicator:** inline 24 px row inside header — amber square + "Connection lost. Retrying..." / "Conexiune pierdută. Reîncercăm...".

### 3.7 Motion

- "X new updates" banner enter: slide down + fade, `--duration-slow` `--ease-out`. Dismiss: slide up, `--duration-base`.
- Card hover/tap: subtle `--shadow-2` lift, `--duration-base`.
- Reduced-motion: drop slides, keep opacity fades.

---

## 4. Copy library (12 categories, EN + RO)

Source bodies from organizer; FE renders verbatim. Below are sample bodies illustrating tone, NOT hardcoded strings.

| Category | EN example | RO example |
|---|---|---|
| General | Gates open 30 minutes early tonight. Head to the main entrance. | Porțile se deschid cu 30 de minute mai devreme diseară. Mergi la intrarea principală. |
| Transport | Last shuttle to Cluj leaves at 04:30 from the main gate. | Ultima navetă spre Cluj pleacă la 04:30 de la poarta principală. |
| Accommodation | Camping Hub B showers open at 07:00. Bring your own towel. | Dușurile din Camping Hub B se deschid la 07:00. Vino cu prosop. |
| Food | New vegan stall open near the Hangar. Worth the walk. | Stand vegan nou lângă Hangar. Merită drumul. |
| Music | Bring Me the Horizon started at Main Stage. Set ends at 23:30. | Bring Me the Horizon a început pe Main Stage. Set până la 23:30. |
| Lineup | Apashe moved to Dance Arena. New time: 02:00. | Apashe s-a mutat pe Dance Arena. Ora nouă: 02:00. |
| Activity | Silent disco at EC Village starts at midnight. Headphones at the gate. | Silent disco la EC Village începe la miezul nopții. Căști la poartă. |
| Weather | Rain expected from 23:30. Covered stages: Hangar, Booha, Camping Hub. | Ploaie din 23:30. Scene acoperite: Hangar, Booha, Camping Hub. |
| Rules | No glass beyond the main arena. Plastic cups at every bar. | Fără sticlă în arenă. Pahare de plastic la toate barurile. |
| Ticket | RFID top-up near Main Stage now accepts card. | Reîncărcare RFID lângă Main Stage acceptă card acum. |
| Safety | Medical tent at the east gate is open 24/7. Don't hesitate to ask. | Cortul medical la poarta estică e deschis 24/7. Cere ajutor oricând. |
| Health | Free water refill at Forest Stage and Camping Hub B. Stay hydrated. | Reumplere gratuită la Forest Stage și Camping Hub B. Hidratează-te. |

### Empty-state copy

| Trigger | EN | RO |
|---|---|---|
| Personalized empty | Nothing here for your artists yet. Updates show up the moment organisers post them. | Nimic pentru artiștii tăi deocamdată. Actualizările apar imediat ce organizatorii le publică. |
| "Urgent" filter, none active | All quiet on the urgent front. | Totul e liniștit pe frontul urgent. |
| First-load empty | The feed is warming up. Check back in a moment. | Feed-ul se pregătește. Mai verifică în câteva momente. |

Note: all bodies avoid em-dashes and "it's not X, it's Y" per text-copy §03a. Romanian uses comma-below ș, ț.

---

## 5. What to cut from the current implementation

| Item | Reason |
|---|---|
| `Send demo notification` button in header | Dev tool leaked to prod UI. |
| `FeedItemKind.Photo` (rainbow gradient) | No backend image source. |
| `FeedItemKind.Social` ("Maria + 4 others") | No social graph in API. |
| `FeedItemKind.ScheduleChange` distinct kind | Backend has no ScheduleChange; folds into Lineup category. |
| `FeedItemKind.NowPlaying` and `UpNext` as raw backend types | No source. Acceptable only as the client-derived pinned capsule. |
| `animate-pulse` white dot on NowPlaying | Distraction; replace with static yellow square if kept. |
| `bg-surface-50 dark:bg-surface-950` shell | Replace with EC tokens (`--ec-dark-navy`, `--ec-off-white`). |
| English-only strings | Wire through Transloco EN + RO. |

The surviving model collapses cleanly to ONE backend-driven card kind, parameterized by `PrimaryCategory`, plus the client-derived pinned capsule. Down from 7 client kinds to 1 + 1 (pinned).

---

## 6. Reviewer-agent checklist (when implementation lands later)

Future code-review dispatches MUST include:

- Verify CODE.md compliance against the changed files.
- Verify `frontend/visual-design-language.md` compliance — square corners, EC tokens, hit targets, type tokens.
- Verify `frontend/text-copy-design-language.md` compliance — flag em-dashes (`—`), flag "it's not X, it's Y", restore comma-below ș/ț.
- Verify no invented backend fields (Photo, Social, severity, location, author).

---

## 7. Decisions (user delegated; revisit anytime)

User said: "go with your expertise, nothing finalized, not that important". Calls below; flag for re-review at implementation time.

1. **Pinned capsule source** — client-derive only. No new endpoint. `computed()` over feed signal.
2. **Filter chips** — 5 collapsed (All / Urgent / Schedule / Getting around / General). Drunk-test wins over taxonomy purity.
3. **"For you" chip** — defer. Easy add later once prefs read pipeline is wired.
4. **Pinned capsule dismissal** — session-local. Resets on app open. Festival context = yesterday's pinned is irrelevant.
5. **Disconnected/offline UI** — inline header indicator only. No toast. Persistent dot says enough; toast adds noise.
6. **Tap-to-expand vs detail route** — inline expand. Keep user in flow, fewer screens to back out of.
7. **Stories-viewer dirty file** — leave alone. Separate work (`plan-share`). Borrow motion vocab later if useful; do not entangle now.
8. **Demo notification button** — cut entirely. Belongs in a dev-only route, not main UI.
9. **Prefs read source** — `GET /api/v1/preferences` (Plans lib, `PreferencesDto.musicGenres[]` + `.artists[]`). Bootstrap once, cache client-side. Powers pinned capsule artist-hit branch; "For you" chip will reuse when un-deferred.
10. **SSE arrival pattern** — banner ("X new updates"), not auto-insert. Confirmed.
