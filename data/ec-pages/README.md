# data/ec-pages — EC Website RAG Content

Scraped content from 9 official Electric Castle website pages.
These files feed the VectorDb ingest pipeline.

Re-scrape at any time by running `python scripts/scrape-ec-docs.py` from the repo root.
After re-scraping, re-run the annotation pipeline:

```bash
python scripts/annotate-ec-pages.py   # re-annotate informational pages
# line-up.md must be restructured manually (run the inline script in the plan)
python scripts/extract-ec-faq.py       # regenerate data/faqs-ec-website.json
```

---

## FAQ data (`data/faqs-ec-website.json`)

`data/faqs-ec-website.json` supersedes `data/ec-pages/faq.md` for FAQ content.

- **Ingest via `IIngestService.IngestQAAsync`**, NOT `IngestDocumentAsync`.
- `data/ec-pages/faq.md` contains only a redirect notice — do NOT ingest it as a document.
- Each entry maps 1:1 to an `IngestQARequest`:

```csharp
await ingestService.IngestQAAsync(
    new IngestQARequest(
        SourceRef: entry.source_ref,           // "ec-website/faq/tickets/how-to-buy-tickets"
        QuestionText: entry.question,
        QuestionCategoryValues: {},             // general — matches all users
        Answers: entry.answers.Select(a =>
            new IngestAnswerRequest(a.text, {})).ToList()),
    ct);
```

120 Q&A entries across 8 sections: Tickets (15), Merchandise (14), Exchange Platform (10),
Festival Area (25), Cashless System (12), EC Village (30), Transportation (9),
Vendors & Volunteers (5).

---

## Chunking annotation standard

All `.md` files (except `faq.md` and `README.md`) use `## Heading` markers as semantic
chunk boundaries. The VectorDb ingest pipeline **MUST split at these boundaries BEFORE
applying the 400-token/50-token-overlap chunker**. Splitting only by token count will
straddle section boundaries and degrade retrieval quality.

Recommended ingest strategy per document file:

1. Strip YAML frontmatter → extract `source`, `source_ref`
2. Split body at `\n## ` → list of sections (each section retains the `## Heading` line)
3. For each section: if token count > 400, apply 400t/50t overlap sub-chunking
4. Each resulting chunk → one `document_chunks` row

```csharp
await ingestService.IngestDocumentAsync(
    new IngestDocumentRequest(source, source_ref, content), ct);
```

`IngestDocumentAsync` handles SHA-256 deduplication and embedding via `text-embedding-3-small`.
Re-running on unchanged files is a no-op.

**Skip files whose body contains `WARNING:` or `ERROR:` (unresolved stubs).
Skip `faq.md` (redirect notice only — use `faqs-ec-website.json` instead).**

---

## Source mapping

| File | `source` | `source_ref` | Heading strategy |
|---|---|---|---|
| `line-up.md` | `EcLineup` | `ec-website/line-up` | `## ARTIST NAME` + `Day: DATE` per artist (195 entries) |
| `vip-experience.md` | `EcVipExperience` | `ec-website/vip-experience` | ALL-CAPS section names (26 headers) |
| `youth-pass-u25.md` | `EcYouthPass` | `ec-website/youth-pass-u25` | No headers (1.6 KB — single chunk) |
| `music-stages.md` | `EcMusicStages` | `ec-website/music-stages` | Stage names only (6 headers) |
| `fyi.md` | `EcFyi` | `ec-website/fyi` | ALL-CAPS section names (20 headers) |
| `faq.md` | — | — | **Redirect only — use `faqs-ec-website.json`** |
| `ec-village.md` | `EcVillage` | `ec-website/ec-village` | Accommodation sections (7 headers) |
| `sustainability.md` | `EcSustainability` | `ec-website/sustainability` | ALL-CAPS initiative names (15 headers) |
| `international.md` | `EcInternational` | `ec-website/international` | Stage/venue names (2 headers) |

---

## Quality report (scraped 2026-05-23)

| File | Chars | Status |
|---|---|---|
| `line-up.md` | ~3,900 | OK (restructured) |
| `vip-experience.md` | 3,910 | OK |
| `youth-pass-u25.md` | 1,634 | OK |
| `music-stages.md` | 4,951 | OK |
| `fyi.md` | 18,721 | OK |
| `faq.md` | ~200 | Redirect notice only |
| `ec-village.md` | 6,485 | OK |
| `sustainability.md` | 8,998 | OK |
| `international.md` | 7,754 | OK |

## Refreshing content

```bash
python scripts/scrape-ec-docs.py
python scripts/annotate-ec-pages.py
python scripts/extract-ec-faq.py
git add data/ec-pages/ data/faqs-ec-website.json
git commit -m "Refresh EC page content"
```
