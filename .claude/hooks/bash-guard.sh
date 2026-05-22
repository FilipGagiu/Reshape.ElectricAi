#!/usr/bin/env bash
# PreToolUse (Bash) hook: block package-install commands (CLAUDE.md §6a).
# Input on stdin: { tool_name: "Bash", tool_input: { command }, ... }

set -euo pipefail

INPUT=$(cat)

CMD=$(INPUT="$INPUT" python -c '
import json, os, sys
try:
    data = json.loads(os.environ["INPUT"])
    print(data.get("tool_input", {}).get("command", ""))
except Exception:
    print("")
')

if [[ -z "$CMD" ]]; then
  exit 0
fi

# Strip shell comment lines (#...) so `echo "# dotnet add package mentioned"` doesn't false-positive.
STRIPPED=$(printf '%s\n' "$CMD" | sed 's/#[^\n]*//g')
# Normalize whitespace for matching.
NORMALIZED=$(printf '%s\n' "$STRIPPED" | tr -s '[:space:]' ' ')

block() {
  echo "Package install blocked: \"$1\". Surface what you need to the user (package name, version, target project, why) and wait for them to install it (CLAUDE.md §6a)." >&2
  exit 2
}

# Match patterns. Use grep -E with anchors that handle leading wrappers like `cd x && ...`.
if echo "$NORMALIZED" | grep -Eq '(^|[ ;&|])dotnet add package( |$)'; then
  block "dotnet add package"
fi
if echo "$NORMALIZED" | grep -Eq '(^|[ ;&|])npm +(install|i|add)( +-{1,2}[A-Za-z][A-Za-z0-9-]*)* +[^-][^ ]*'; then
  # npm install/i/add <pkg> — tolerates leading flag tokens (--save-dev, -D, -g, etc.)
  block "npm install/i/add <package>"
fi
if echo "$NORMALIZED" | grep -Eq '(^|[ ;&|])pip(3)? install( |$)'; then
  block "pip install"
fi
if echo "$NORMALIZED" | grep -Eq '(^|[ ;&|])cargo add( |$)'; then
  block "cargo add"
fi
if echo "$NORMALIZED" | grep -Eq '(^|[ ;&|])yarn add( |$)'; then
  block "yarn add"
fi
if echo "$NORMALIZED" | grep -Eq '(^|[ ;&|])pnpm add( |$)'; then
  block "pnpm add"
fi

exit 0
