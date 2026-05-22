#!/usr/bin/env bash
# PreToolUse (Edit|Write) hook:
#   - exit 2 to block direct-edit of MEMORY.md (CLAUDE.md §3a)
#   - else inject CODE.md re-read reminder for code files (Phase 7)
#
# Input on stdin: { tool_name, tool_input: { file_path }, ... }

set -euo pipefail

INPUT=$(cat)

# Parse file_path via python (jq missing).
FILE_PATH=$(INPUT="$INPUT" python -c '
import json, os, sys
try:
    data = json.loads(os.environ["INPUT"])
    print(data.get("tool_input", {}).get("file_path", ""))
except Exception:
    print("")
')

if [[ -z "$FILE_PATH" ]]; then
  # No path → nothing to check. Allow.
  exit 0
fi

BASENAME=$(basename "$FILE_PATH")
BASENAME_LC=$(echo "$BASENAME" | tr '[:upper:]' '[:lower:]')

if [[ "$BASENAME_LC" == "memory.md" ]]; then
  # Allow the auto-memory index under ~/.claude/projects/<slug>/memory/MEMORY.md —
  # that IS the path /si:remember writes to. Block every other MEMORY.md.
  NORM_PATH=$(echo "$FILE_PATH" | tr '\\' '/')
  NORM_PATH_LC=$(echo "$NORM_PATH" | tr '[:upper:]' '[:lower:]')
  case "$NORM_PATH_LC" in
    */.claude/projects/*/memory/memory.md)
      : # auto-memory index — allow
      ;;
    *)
      echo "MEMORY.md is never direct-edited (case-insensitive). Use /si:remember <fact> instead (CLAUDE.md §3a)." >&2
      exit 2
      ;;
  esac
fi

# Inject CODE.md reminder only for code files. Skip md/json/yml/yaml to avoid noise.
case "$BASENAME" in
  *.cs|*.csproj|*.sln|*.ts|*.tsx|*.js|*.jsx|*.py|*.go|*.rs|*.java|*.kt)
    python -c '
import json, sys
sys.stdout.write(json.dumps({
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "additionalContext": "About to edit code. Re-read CODE.md and verify the change honors every rule (CLAUDE.md Phase 7)."
  }
}))
'
    ;;
  *)
    exit 0
    ;;
esac
