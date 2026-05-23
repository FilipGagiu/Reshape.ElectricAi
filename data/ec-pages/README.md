# data/ec-pages — EC Website RAG Content

Scraped content from 9 official Electric Castle website pages.
These files feed the VectorDb ingest pipeline via `IIngestService.IngestDocumentAsync`.

Re-scrape at any time by running `python scripts/scrape-ec-docs.py` from the repo root.

## Source mapping

| File | `source` | `source_ref` | URL |
|---|---|---|---|
| `line-up.md` | `EcLineup` | `ec-website/line-up` | https://electriccastle.ro/line-up |
| `vip-experience.md` | `EcVipExperience` | `ec-website/vip-experience` | https://electriccastle.ro/vip-experience |
| `youth-pass-u25.md` | `EcYouthPass` | `ec-website/youth-pass-u25` | https://electriccastle.ro/youth-pass-u25 |
| `music-stages.md` | `EcMusicStages` | `ec-website/music-stages` | https://electriccastle.ro/music-stages |
| `fyi.md` | `EcFyi` | `ec-website/fyi` | https://electriccastle.ro/fyi |
| `faq.md` | `EcFaq` | `ec-website/faq` | https://electriccastle.ro/faq |
| `ec-village.md` | `EcVillage` | `ec-website/ec-village` | https://electriccastle.ro/ec-village |
| `sustainability.md` | `EcSustainability` | `ec-website/sustainability` | https://electriccastle.ro/sustainability |
| `international.md` | `EcInternational` | `ec-website/international` | https://electriccastle.ro/international |

## Ingest contract (for VectorDb developer)

Walk `data/ec-pages/*.md`, skip `README.md`. For each file:

1. Parse YAML frontmatter → `source`, `source_ref`
2. Read everything after the closing `---` → `content`
3. Skip files whose body contains `WARNING:` or `ERROR:` (unresolved stubs)
4. Call:

```csharp
await ingestService.IngestDocumentAsync(
    new IngestDocumentRequest(source, source_ref, content), ct);
```

`IngestDocumentAsync` handles SHA-256 deduplication, 400-token chunking with
50-token overlap, and embedding via `text-embedding-3-small`. Re-running on
unchanged files is a no-op.

## Quality report (scraped 2026-05-23)

| File | Chars | Status |
|---|---|---|
| `line-up.md` | 6,338 | OK |
| `vip-experience.md` | 3,910 | OK |
| `youth-pass-u25.md` | 1,634 | OK |
| `music-stages.md` | 4,951 | OK |
| `fyi.md` | 18,721 | OK |
| `faq.md` | 35,806 | OK |
| `ec-village.md` | 6,485 | OK |
| `sustainability.md` | 8,998 | OK |
| `international.md` | 7,754 | OK |

## Refreshing content

```bash
python scripts/scrape-ec-docs.py
git add data/ec-pages/
git commit -m "Refresh EC page content"
```
