#!/usr/bin/env python3
"""
Adds ## heading markers to section boundaries in EC page markdown files.
- Most pages: detects ALL-CAPS lines as section headers.
- music-stages.md: uses a known stage list (artist names are also ALL-CAPS).
Skip: line-up.md (restructured separately), faq.md (superseded), README.md.
"""

import os
import re

SKIP_FILES = {"line-up.md", "faq.md", "README.md"}

ALL_CAPS_RE = re.compile(r"^[A-Z][A-Z\s&'\-]{2,}$")

# music-stages.md: annotate only stage names, not the artist names listed beneath each stage
MUSIC_STAGES_KNOWN = {
    "MAIN STAGE", "HANGAR", "BOOHA", "HIDEOUT", "BACKYARD STAGE", "PING PONG STAGE",
}


def is_all_caps_header(line: str) -> bool:
    stripped = line.strip()
    if not stripped or stripped.startswith("##"):
        return False
    if not ALL_CAPS_RE.match(stripped):
        return False
    return sum(1 for c in stripped if c.isupper()) >= 2


def annotate_file(path: str, known_headers: set[str] | None = None) -> int:
    with open(path, encoding="utf-8") as f:
        content = f.read()

    fm_match = re.match(r"(^---\n.*?\n---\n)(.*)", content, re.DOTALL)
    if not fm_match:
        return 0
    frontmatter, body = fm_match.group(1), fm_match.group(2)

    lines = body.splitlines()
    new_lines = []
    changed = 0

    for line in lines:
        stripped = line.strip()
        if stripped.startswith("##"):
            new_lines.append(line)
            continue
        if known_headers is not None:
            is_header = stripped in known_headers
        else:
            is_header = is_all_caps_header(stripped)
        if is_header:
            new_lines.append(f"## {stripped}")
            changed += 1
        else:
            new_lines.append(line)

    with open(path, "w", encoding="utf-8") as f:
        f.write(frontmatter + "\n".join(new_lines))

    return changed


def main() -> None:
    repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    pages_dir = os.path.join(repo_root, "data", "ec-pages")

    for filename in sorted(os.listdir(pages_dir)):
        if filename in SKIP_FILES or not filename.endswith(".md"):
            continue
        path = os.path.join(pages_dir, filename)
        known = MUSIC_STAGES_KNOWN if filename == "music-stages.md" else None
        count = annotate_file(path, known)
        print(f"  {filename}: {count} headers annotated")


if __name__ == "__main__":
    main()
