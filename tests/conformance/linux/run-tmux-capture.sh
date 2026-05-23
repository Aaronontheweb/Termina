#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
ARTIFACT_DIR="${1:-${ROOT_DIR}/conformance-artifacts/tmux}"
mkdir -p "$ARTIFACT_DIR"

RUN_ROOT="$(mktemp -d)"
CONFORMANCE_DIR="$RUN_ROOT/termina-conformance"

CAPTURE="$ARTIFACT_DIR/pane.txt"
EVENTS="$ARTIFACT_DIR/events.jsonl"
SESSION="termina-conformance"
TMUX_SOCKET="termina-ci"

export TERMINA_RAW_INPUT=1
export TERMINA_KITTY_KEYBOARD=9
export TMPDIR="$RUN_ROOT"
unset TMUX || true
unset TERM_PROGRAM || true

tmux -L "$TMUX_SOCKET" kill-server >/dev/null 2>&1 || true
tmux -L "$TMUX_SOCKET" new-session -d -s "$SESSION" "env -u TMUX -u TERM_PROGRAM bash --noprofile --norc -lc 'dotnet run --project demos/Termina.Demo.Conformance/Termina.Demo.Conformance.csproj -c Release --no-build -- --kitty-alt-scroll'"

sleep 4
tmux -L "$TMUX_SOCKET" send-keys -t "$SESSION" a b c Left Left Up Down Enter
sleep 1
tmux -L "$TMUX_SOCKET" capture-pane -p -t "$SESSION" >"$CAPTURE"

cp "$CONFORMANCE_DIR/events.jsonl" "$EVENTS"

python3 "$ROOT_DIR/tests/conformance/linux/assert-conformance.py" \
  --events "$EVENTS" \
  --expect-wheel

tmux -L "$TMUX_SOCKET" kill-session -t "$SESSION" >/dev/null 2>&1 || true
tmux -L "$TMUX_SOCKET" kill-server >/dev/null 2>&1 || true
rm -rf "$RUN_ROOT"
