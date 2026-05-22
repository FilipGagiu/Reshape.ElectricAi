#!/usr/bin/env bash
# UserPromptSubmit hook: inject current date + git branch into context.

set -euo pipefail

cat >/dev/null 2>&1 || true

ROOT="${CLAUDE_PROJECT_DIR:-$(pwd)}"
DATE=$(date +%Y-%m-%d)
BRANCH=$(git -C "$ROOT" branch --show-current 2>/dev/null || echo "unknown")

DATE="$DATE" BRANCH="$BRANCH" python -c '
import json, os, sys
d = os.environ.get("DATE", "")
b = os.environ.get("BRANCH", "")
sys.stdout.write(json.dumps({
  "hookSpecificOutput": {
    "hookEventName": "UserPromptSubmit",
    "additionalContext": "Date: " + d + "\nBranch: " + b
  }
}))
'
