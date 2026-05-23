#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
ARTIFACT_DIR="${1:-${ROOT_DIR}/conformance-artifacts/baseline}"
mkdir -p "$ARTIFACT_DIR"

CAPTURE="$ARTIFACT_DIR/ansi-capture.log"
EVENTS="$ARTIFACT_DIR/events.jsonl"

export TERMINA_RAW_INPUT=0
unset TERMINA_KITTY_KEYBOARD || true
unset TMUX || true
unset TERM_PROGRAM || true

rm -rf /tmp/termina-conformance || true

script -qefc "dotnet run --project demos/Termina.Demo.Conformance/Termina.Demo.Conformance.csproj -c Release --no-build" "$CAPTURE" >/dev/null &
SCRIPT_PID=$!

for _ in $(seq 1 30); do
  [[ -f /tmp/termina-conformance/events.jsonl ]] && break
  sleep 1
done

sleep 2
pkill -f "Termina.Demo.Conformance" || true
wait "$SCRIPT_PID" || true

cp /tmp/termina-conformance/events.jsonl "$EVENTS"

python3 "$ROOT_DIR/tests/conformance/linux/assert-conformance.py" \
  --events "$EVENTS" \
  --ansi-capture "$CAPTURE" \
  --require-legacy-mouse-tracking
