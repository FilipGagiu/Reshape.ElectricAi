#!/usr/bin/env bash
# SessionStart hook: load CODE.md, PROJECT.md, README.md, STATE.md into context.
# Bootstrap warning when .claude/docs/ missing.
# Emits JSON: { hookSpecificOutput: { hookEventName, additionalContext }, systemMessage? }

set -eu  # NOTE: deliberate no `pipefail` — a wc/head failure on one doc must not kill the whole hook.

ROOT="${CLAUDE_PROJECT_DIR:-$(pwd)}"
MAX_LINES=400
DOCS=("CODE.md" "PROJECT.md" "README.md" ".claude/docs/STATE.md")

# Discard stdin (we don't need session input fields for this hook).
cat >/dev/null 2>&1 || true

ctx=""
for rel in "${DOCS[@]}"; do
  path="${ROOT}/${rel}"
  if [[ -f "$path" ]]; then
    lines=$(wc -l < "$path" 2>/dev/null | tr -d ' ' || echo 0)
    if [[ -z "$lines" ]]; then lines=0; fi
    if (( lines > MAX_LINES )); then
      body=$(head -n "$MAX_LINES" "$path" 2>/dev/null || true)
      ctx+="===== ${rel} (truncated — ${lines} lines total, first ${MAX_LINES} shown; Read the file for full content) =====
${body}

"
    else
      body=$(cat "$path" 2>/dev/null || true)
      ctx+="===== ${rel} =====
${body}

"
    fi
  fi
done

bootstrap_msg=""
if [[ ! -d "${ROOT}/.claude/docs" ]]; then
  bootstrap_msg="Bootstrap: .claude/docs/ missing. Offer to scaffold STATE.md/todo.md per CLAUDE.md Bootstrap section."
fi

# Emit JSON via python (jq not available on this machine).
ROOT_CTX="$ctx" ROOT_BOOT="$bootstrap_msg" python -c '
import json, os, sys
ctx = os.environ.get("ROOT_CTX", "")
boot = os.environ.get("ROOT_BOOT", "")
out = {"hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": ctx}}
if boot:
    out["systemMessage"] = boot
sys.stdout.write(json.dumps(out))
'
