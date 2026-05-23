# EC Website URL Ingest — Design Spec

**Date:** 2026-05-23  
**Branch:** `feature/vector-db`  
**Scope:** Scrape 9 official EC website pages → clean Markdown files → ready for VectorDb ingest pipeline  
**Out of scope:** VectorDb implementation (separate spec), AiChat RAG orchestration, re-scraping automation

---

## Problem

The VectorDb ingest pipeline (`IIngestService.IngestDocumentAsync`) needs content from EC's official website pages (lineup, FAQ, VIP experience, etc.). That content lives at public URLs, not in `Client Generic Requirements/` files. It needs to be scraped, cleaned, and structured before the developer implements the ingest pipeline so data is ready to load on day one.

---

## Decisions

### Scrape now, ingest later

Scrape content today and commit the results as Markdown files under `data/ec-pages/`. The VectorDb developer reads these files at ingest time — no runtime web dependency. Content is version-controlled and human-reviewable.

### Python stdlib only

`urllib.request` + `html.parser` + `html.unescape` — zero package installs. If pages are JS-rendered and yield thin content (<200 chars), the script flags stubs for manual review; upgrade to BeautifulSoup or Playwright only if needed.

### One source string per page (granular)

Each URL gets its own `source` string (e.g., `EcFaq`, `EcLineup`). Allows future source-filtered vector searches per topic.

### YAML frontmatter in each file

Each `.md` file carries `source`, `source_ref`, and `url` in a YAML header. The ingest code reads these directly — no separate mapping file required at runtime.

---

## Source mapping

| URL | `source` | `source_ref` |
|---|---|---|
| `https://electriccastle.ro/line-up` | `EcLineup` | `ec-website/line-up` |
| `https://electriccastle.ro/vip-experience` | `EcVipExperience` | `ec-website/vip-experience` |
| `https://electriccastle.ro/youth-pass-u25` | `EcYouthPass` | `ec-website/youth-pass-u25` |
| `https://electriccastle.ro/music-stages` | `EcMusicStages` | `ec-website/music-stages` |
| `https://electriccastle.ro/fyi` | `EcFyi` | `ec-website/fyi` |
| `https://electriccastle.ro/faq` | `EcFaq` | `ec-website/faq` |
| `https://electriccastle.ro/ec-village` | `EcVillage` | `ec-website/ec-village` |
| `https://electriccastle.ro/sustainability` | `EcSustainability` | `ec-website/sustainability` |
| `https://electriccastle.ro/international` | `EcInternational` | `ec-website/international` |

---

## Output file format

```
data/
└── ec-pages/
    ├── README.md             ← developer guide: source mapping + ingest contract
    ├── line-up.md
    ├── vip-experience.md
    ├── youth-pass-u25.md
    ├── music-stages.md
    ├── fyi.md
    ├── faq.md
    ├── ec-village.md
    ├── sustainability.md
    └── international.md
```

Each `.md` file:

```markdown
---
source: EcFaq
source_ref: ec-website/faq
url: https://electriccastle.ro/faq
scraped_at: 2026-05-23T<ISO8601>
---

[clean extracted text — no HTML, no nav/footer/script noise]
```

---

## Scraper script

**File:** `scripts/scrape-ec-docs.py`  
**Runtime:** Python 3.x stdlib only

### Algorithm (per URL)

1. `urllib.request.urlopen` with `User-Agent: Mozilla/5.0` header (avoids basic bot blocks)
2. Parse HTML with `html.parser.HTMLParser` subclass — removes `<script>`, `<style>`, `<nav>`, `<header>`, `<footer>`, `<aside>`, `<noscript>` elements and their content
3. Strip remaining HTML tags from accumulated text
4. `html.unescape` to decode entities (`&amp;`, `&nbsp;`, etc.)
5. Collapse runs of whitespace; remove blank lines exceeding 2 consecutive
6. Quality gate: if extracted text < 200 chars, write stub file with `WARNING: thin content` and log to console — indicates JS rendering or bot block
7. Write `data/ec-pages/<slug>.md` with YAML frontmatter

### Idempotent

Re-running the script overwrites existing files. `scraped_at` timestamp updates. This is intentional — content can be refreshed before the hackathon event.

---

## Ingest contract (for VectorDb developer)

When implementing `VectorDb:AutoIngest` startup behavior, walk `data/ec-pages/*.md` (skip `README.md`) and for each file:

1. Parse YAML frontmatter → `source`, `source_ref`
2. Read body (everything after the closing `---`) → `content`
3. Skip files containing `WARNING: thin content` in the body (flagged stubs)
4. Call:

```csharp
await ingestService.IngestDocumentAsync(
    new IngestDocumentRequest(source, source_ref, content), ct);
```

`IngestDocumentAsync` handles SHA-256 deduplication, chunking (400-token, 50-token overlap), and embedding via `text-embedding-3-small`. Re-running ingest on unchanged files is a no-op.

---

## data/ec-pages/README.md contents

The committed `README.md` documents:

- Source mapping table (URL → source → source_ref)
- Ingest contract (code snippet above)
- Quality notes per file (auto-populated by scraper: char count, any warnings)
- Instructions: "To refresh content, re-run `python scripts/scrape-ec-docs.py` and commit the diff"

---

## Implementation steps

1. Create `data/ec-pages/` directory (commit empty `.gitkeep` first if needed)
2. Write `scripts/scrape-ec-docs.py`
3. Run the script — inspect output quality for all 9 pages
4. If any page yields thin content: investigate (JS rendering?) and handle manually or via curl fallback
5. Write `data/ec-pages/README.md` with source mapping + quality report
6. Commit `scripts/scrape-ec-docs.py` + all `data/ec-pages/*.md` + `README.md`

---

## Risks

| Risk | Mitigation |
|---|---|
| EC pages are JS-rendered (SPA) | Quality gate flags stubs; upgrade to BeautifulSoup or Playwright if needed |
| Bot detection / rate limiting | Browser User-Agent header; 1s delay between requests |
| Page structure changes before hackathon | Script is idempotent — re-run and commit updated files |
| Thin content after stripping | Manual review + manual edit of stub files |
