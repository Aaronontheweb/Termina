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
TMUX_MOUSE="${TMUX_CONFORMANCE_MOUSE:-off}"

export TERMINA_RAW_INPUT=1
export TERMINA_KITTY_KEYBOARD=9
export TMUX_EXPECTED_MOUSE="$TMUX_MOUSE"
export TMPDIR="$RUN_ROOT"
unset TERM_PROGRAM || true

tmux -L "$TMUX_SOCKET" kill-server >/dev/null 2>&1 || true

tmux -L "$TMUX_SOCKET" new-session -d -s "$SESSION" \
  "bash --noprofile --norc -lc 'dotnet run --project demos/Termina.Demo.Conformance/Termina.Demo.Conformance.csproj -c Release --no-build -- --kitty-alt-scroll'"

tmux -L "$TMUX_SOCKET" set-option -g allow-passthrough on
tmux -L "$TMUX_SOCKET" set-option -g mouse "$TMUX_MOUSE"

sleep 4
tmux -L "$TMUX_SOCKET" send-keys -t "$SESSION" a b c Left Left Up Down Enter
sleep 1
tmux -L "$TMUX_SOCKET" capture-pane -p -t "$SESSION" >"$CAPTURE"

cp "$CONFORMANCE_DIR/events.jsonl" "$EVENTS"

tmux -L "$TMUX_SOCKET" show -gv mouse >"$ARTIFACT_DIR/tmux-mouse-mode.txt" 2>&1 || true
tmux -L "$TMUX_SOCKET" -V >"$ARTIFACT_DIR/tmux-version.txt" 2>&1 || true

python3 "$ROOT_DIR/tests/conformance/linux/assert-conformance.py" \
  --events "$EVENTS" \
  --expect-tmux true \
  --expect-tmux-mouse "$TMUX_MOUSE" \
  --expect-wheel \
  --expect-arrows

tmux -L "$TMUX_SOCKET" kill-session -t "$SESSION" >/dev/null 2>&1 || true
tmux -L "$TMUX_SOCKET" kill-server >/dev/null 2>&1 || true
rm -rf "$RUN_ROOT"
