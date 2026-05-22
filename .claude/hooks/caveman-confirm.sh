#!/usr/bin/env bash
# SessionStart (startup) hook: inject instruction making Claude ask the user
# about caveman mode if not specified in the session's first turn.

set -euo pipefail

cat >/dev/null 2>&1 || true

INSTRUCTION="First action this session: ask the user 'Caveman mode currently active (full from plugin). Keep full, switch to lite/ultra, or turn off?' UNLESS the user's first message explicitly sets a mode (mentions /caveman, 'stop caveman', 'normal mode', or 'caveman lite/full/ultra'). After that first turn, do not ask again."

INSTRUCTION="$INSTRUCTION" python -c '
import json, os, sys
sys.stdout.write(json.dumps({
  "hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": os.environ["INSTRUCTION"]
  }
}))
'
