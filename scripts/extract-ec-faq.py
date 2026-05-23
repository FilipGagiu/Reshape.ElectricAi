#!/usr/bin/env python3
"""
Extracts Q&A pairs from data/ec-pages/faq.md and writes data/faqs-ec-website.json.
Output matches the IngestQARequest shape expected by IIngestService.IngestQAAsync.
"""

import json
import os
import re

SECTIONS = [
    "Tickets", "Merchandise", "Exchange Platform", "Festival Area",
    "Cashless System", "EC Village", "Transportation", "Vendors & Volunteers",
]
NOISE_LINES = {"Back to top", "expand all", "Frequently Asked Questions",
               "Artist List", "Alphabetical"}
# Page-template footer that appears after the last FAQ entry in the scraped HTML
FOOTER_SENTINEL = "That’s why when the party is over"


def slugify(text: str) -> str:
    text = text.lower().rstrip("?").strip()
    text = re.sub(r"[^a-z0-9\s-]", "", text)
    text = re.sub(r"\s+", "-", text)
    return text


def parse_faq(path: str) -> list[dict]:
    with open(path, encoding="utf-8") as f:
        raw = f.read()

    body = re.sub(r"^---\n.*?\n---\n", "", raw, flags=re.DOTALL).strip()
    lines = [line.strip() for line in body.splitlines()]

    try:
        start = lines.index("Frequently Asked Questions") + 1
    except ValueError:
        start = 0
    lines = lines[start:]

    entries = []
    current_section = "General"
    current_question = None
    current_answer_lines: list[str] = []

    def flush() -> None:
        if current_question:
            answer_text = " ".join(
                line for line in current_answer_lines if line not in NOISE_LINES
            ).strip()
            entries.append({
                "source_ref": f"ec-website/faq/{slugify(current_section)}/{slugify(current_question)}",
                "section": current_section,
                "question": current_question,
                "question_category_values": {},
                "answers": [{"text": answer_text, "category_values": {}}],
            })

    for line in lines:
        if not line or line in NOISE_LINES:
            continue
        if line.startswith(FOOTER_SENTINEL):
            break
        if line in SECTIONS:
            flush()
            current_section = line
            current_question = None
            current_answer_lines = []
        elif line.endswith("?"):
            flush()
            current_question = line
            current_answer_lines = []
        else:
            current_answer_lines.append(line)

    flush()
    return entries


def main() -> None:
    repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    faq_path = os.path.join(repo_root, "data", "ec-pages", "faq.md")
    out_path = os.path.join(repo_root, "data", "faqs-ec-website.json")

    entries = parse_faq(faq_path)
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(entries, f, ensure_ascii=False, indent=2)

    print(f"Extracted {len(entries)} Q&A entries → {out_path}")
    for section in SECTIONS:
        count = sum(1 for e in entries if e["section"] == section)
        print(f"  {section}: {count}")


if __name__ == "__main__":
    main()
