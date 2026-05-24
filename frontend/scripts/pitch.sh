#!/usr/bin/env bash
# Build + serve + tunnel. One command, Ctrl+C to stop everything.
#
# Usage: ./scripts/pitch.sh
#
# Requires: node, npx (cloudflared + http-server fetched via npx on demand).

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST_DIR="$ROOT_DIR/dist/electric-ai/browser"
PORT=8080
HTTP_PID=""
CF_PID=""
WATCH_PID=""

cleanup() {
    echo ""
    echo "→ shutting down..."
    [[ -n "$WATCH_PID" ]] && kill "$WATCH_PID" 2>/dev/null || true
    [[ -n "$HTTP_PID"  ]] && kill "$HTTP_PID"  2>/dev/null || true
    [[ -n "$CF_PID"    ]] && kill "$CF_PID"    2>/dev/null || true
    wait 2>/dev/null || true
    exit 0
}
trap cleanup INT TERM

cd "$ROOT_DIR"

# Clear stale processes on our port / cloudflared
lsof -ti :"$PORT" 2>/dev/null | xargs -r kill 2>/dev/null || true
pgrep -f "cloudflared tunnel" | xargs -r kill 2>/dev/null || true

echo "→ initial production build..."
npx --no-install ng build

echo "→ starting watch rebuild (background)..."
npx --no-install ng build --watch --configuration production > /tmp/electric-ai-build.log 2>&1 &
WATCH_PID=$!

echo "→ starting http-server on :$PORT (background)..."
npx --yes http-server "$DIST_DIR" -p "$PORT" -c-1 --proxy "http://localhost:$PORT?" > /tmp/electric-ai-http.log 2>&1 &
HTTP_PID=$!

# Wait for http-server to be ready
for _ in {1..20}; do
    if curl -sf "http://localhost:$PORT/" -o /dev/null; then break; fi
    sleep 0.25
done

echo "→ opening Cloudflare tunnel..."
echo ""
echo "  (PWA URL appears below — open it on your phone in Safari/Chrome)"
echo "  (Ctrl+C to stop everything)"
echo ""

npx --yes cloudflared tunnel --url "http://localhost:$PORT" &
CF_PID=$!

wait "$CF_PID"
