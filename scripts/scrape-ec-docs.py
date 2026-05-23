#!/usr/bin/env python3
"""
EC website scraper — fetches 9 official EC pages, strips HTML, saves clean
Markdown files under data/ec-pages/ with YAML frontmatter for VectorDb ingest.

Usage (from repo root): python scripts/scrape-ec-docs.py
"""

import urllib.request
from html.parser import HTMLParser
import html
import re
import os
import time
from datetime import datetime, timezone

PAGES = [
    ("line-up",        "EcLineup",         "ec-website/line-up",        "https://electriccastle.ro/line-up"),
    ("vip-experience", "EcVipExperience",  "ec-website/vip-experience", "https://electriccastle.ro/vip-experience"),
    ("youth-pass-u25", "EcYouthPass",      "ec-website/youth-pass-u25", "https://electriccastle.ro/youth-pass-u25"),
    ("music-stages",   "EcMusicStages",    "ec-website/music-stages",   "https://electriccastle.ro/music-stages"),
    ("fyi",            "EcFyi",            "ec-website/fyi",            "https://electriccastle.ro/fyi"),
    ("faq",            "EcFaq",            "ec-website/faq",            "https://electriccastle.ro/faq"),
    ("ec-village",     "EcVillage",        "ec-website/ec-village",     "https://electriccastle.ro/ec-village"),
    ("sustainability", "EcSustainability", "ec-website/sustainability", "https://electriccastle.ro/sustainability"),
    ("international",  "EcInternational",  "ec-website/international",  "https://electriccastle.ro/international"),
]

SKIP_TAGS = {"script", "style", "nav", "header", "footer", "aside", "noscript", "iframe", "svg", "button", "form"}
MIN_CONTENT_CHARS = 200


class TextExtractor(HTMLParser):
    def __init__(self):
        super().__init__()
        self._skip_stack = []
        self.chunks = []

    def handle_starttag(self, tag, attrs):
        if tag in SKIP_TAGS:
            self._skip_stack.append(tag)

    def handle_endtag(self, tag):
        if self._skip_stack and self._skip_stack[-1] == tag:
            self._skip_stack.pop()

    def handle_data(self, data):
        if not self._skip_stack:
            stripped = data.strip()
            if stripped:
                self.chunks.append(stripped)


def clean_text(raw_html: str) -> str:
    parser = TextExtractor()
    parser.feed(raw_html)
    text = "\n".join(parser.chunks)
    text = html.unescape(text)
    text = re.sub(r"[ \t]+", " ", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def fetch_page(url: str) -> str:
    req = urllib.request.Request(
        url,
        headers={"User-Agent": "Mozilla/5.0 (compatible; EC-RAG-Scraper/1.0)"},
    )
    with urllib.request.urlopen(req, timeout=15) as resp:
        charset = resp.headers.get_content_charset() or "utf-8"
        return resp.read().decode(charset, errors="replace")


def write_page(out_dir: str, slug: str, source: str, source_ref: str, url: str, content: str) -> bool:
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    thin = len(content) < MIN_CONTENT_CHARS

    frontmatter = (
        f"---\n"
        f"source: {source}\n"
        f"source_ref: {source_ref}\n"
        f"url: {url}\n"
        f"scraped_at: {now}\n"
        f"---\n"
    )
    body = ""
    if thin:
        body = (
            f"\n> WARNING: thin content ({len(content)} chars). "
            f"Page may be JS-rendered or bot-blocked. Review and populate manually.\n\n"
        )
    body += f"\n{content}"

    path = os.path.join(out_dir, f"{slug}.md")
    with open(path, "w", encoding="utf-8") as f:
        f.write(frontmatter)
        f.write(body)

    return thin


def main():
    repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    out_dir = os.path.join(repo_root, "data", "ec-pages")
    os.makedirs(out_dir, exist_ok=True)

    results = []

    for i, (slug, source, source_ref, url) in enumerate(PAGES):
        print(f"[{i+1}/{len(PAGES)}] {url}")
        try:
            raw = fetch_page(url)
            content = clean_text(raw)
            thin = write_page(out_dir, slug, source, source_ref, url, content)
            char_count = len(content)
            status = "WARN (thin)" if thin else "OK"
            if thin:
                print(f"  ⚠ {char_count} chars — thin content, check manually")
            else:
                print(f"  ✓ {char_count} chars")
            results.append((slug, source, status, char_count, ""))
        except Exception as exc:
            print(f"  ✗ FAILED: {exc}")
            now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
            path = os.path.join(out_dir, f"{slug}.md")
            with open(path, "w", encoding="utf-8") as f:
                f.write(
                    f"---\nsource: {source}\nsource_ref: {source_ref}\n"
                    f"url: {url}\nscraped_at: {now}\n---\n\n"
                    f"> ERROR: fetch failed — {exc}\n"
                    f"> Populate this file manually before ingest.\n"
                )
            results.append((slug, source, "FAIL", 0, str(exc)))

        if i < len(PAGES) - 1:
            time.sleep(1)

    print("\n─── Quality report ───────────────────────────────")
    for slug, source, status, chars, err in results:
        icon = "✓" if status == "OK" else ("⚠" if "WARN" in status else "✗")
        note = f"  ERROR: {err}" if err else ""
        print(f"  {icon} {slug:<22} {chars:>6} chars  [{status}]{note}")
    print("──────────────────────────────────────────────────")
    print(f"Files written to: {out_dir}")

    return results


if __name__ == "__main__":
    main()
