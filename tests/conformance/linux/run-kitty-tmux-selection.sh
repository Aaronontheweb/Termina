#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
ARTIFACT_DIR="${1:-${ROOT_DIR}/conformance-artifacts/kitty-tmux}"
mkdir -p "$ARTIFACT_DIR"

RUN_ROOT="$(mktemp -d)"
CONFORMANCE_DIR="$RUN_ROOT/termina-conformance"

KITTY_CONF="$ARTIFACT_DIR/kitty.conf"
SELECTION_FILE="$ARTIFACT_DIR/selection.txt"
CLIPBOARD_FILE="$ARTIFACT_DIR/clipboard.txt"
EVENTS="$ARTIFACT_DIR/events.jsonl"
CAPTURE="$ARTIFACT_DIR/ansi-capture.log"
SOCKET_PATH="${ARTIFACT_DIR}/kitty.sock"
SOCKET="unix:${SOCKET_PATH}"
DISPLAY_NUM=":104"
TMUX_SOCKET="termina-ci"
TMUX_SESSION="conformance"

cat >"$KITTY_CONF" <<'EOF'
allow_remote_control socket
confirm_os_window_close 0
enable_audio_bell no
font_size 16.0
window_padding_width 0
tab_bar_style hidden
copy_on_select clipboard
shell_integration no-rc no-cursor
EOF

export DISPLAY="$DISPLAY_NUM"
export TERMINA_RAW_INPUT=1
export TERMINA_KITTY_KEYBOARD=9
export TMPDIR="$RUN_ROOT"
unset TERM_PROGRAM || true

Xvfb "$DISPLAY_NUM" -screen 0 1400x900x24 >"$ARTIFACT_DIR/xvfb.log" 2>&1 &
XVFB_PID=$!

cleanup() {
  kill "$KITTY_PID" "$XVFB_PID" >/dev/null 2>&1 || true
  rm -rf "$RUN_ROOT"
}
trap cleanup EXIT

kitty --config "$KITTY_CONF" --listen-on "$SOCKET" \
  tmux -L "$TMUX_SOCKET" new-session -d -s "$TMUX_SESSION" \
  "bash --noprofile --norc -lc 'tmux set -g allow-passthrough on; tmux set -g mouse off; script -qefc \"dotnet run --project demos/Termina.Demo.Conformance/Termina.Demo.Conformance.csproj -c Release --no-build -- --kitty-alt-scroll\" '\"$CAPTURE\"'" \
  >"$ARTIFACT_DIR/kitty.log" 2>&1 &
KITTY_PID=$!

for _ in $(seq 1 40); do
  kitty @ --to "$SOCKET" ls >"$ARTIFACT_DIR/kitty-ls.json" 2>/dev/null && [[ -s "$ARTIFACT_DIR/kitty-ls.json" ]] && [[ -f "$CONFORMANCE_DIR/events.jsonl" ]] && break
  sleep 1
done

WINDOW_ID=$(jq '.[0].platform_window_id // 0' "$ARTIFACT_DIR/kitty-ls.json")
COLS=$(jq '.[0].tabs[0].windows[0].columns // 0' "$ARTIFACT_DIR/kitty-ls.json")
LINES=$(jq '.[0].tabs[0].windows[0].lines // 0' "$ARTIFACT_DIR/kitty-ls.json")

if [[ "$WINDOW_ID" -eq 0 || "$COLS" -eq 0 || "$LINES" -eq 0 ]]; then
  echo "kitty window metadata not ready" >&2
  exit 1
fi

for _ in $(seq 1 30); do
  if xdotool getwindowgeometry --shell "$WINDOW_ID" >"$ARTIFACT_DIR/geometry.env" 2>/dev/null; then
    break
  fi
  sleep 1
done

if [[ ! -s "$ARTIFACT_DIR/geometry.env" ]]; then
  echo "kitty window geometry not ready" >&2
  exit 1
fi

eval "$(cat "$ARTIFACT_DIR/geometry.env")"

CELL_W=$((WIDTH / COLS))
CELL_H=$((HEIGHT / LINES))
START_X=$((CELL_W * 3 + CELL_W / 2))
START_Y=$((CELL_H * 3 + CELL_H / 2))
END_X=$((CELL_W * 31))
END_Y=$START_Y

xdotool key --window "$WINDOW_ID" a b c Left Left Up Down Return
sleep 1

xdotool mousemove --window "$WINDOW_ID" "$START_X" "$START_Y"
xdotool mousedown --window "$WINDOW_ID" 1
xdotool mousemove --sync --window "$WINDOW_ID" "$END_X" "$END_Y"
xdotool mouseup --window "$WINDOW_ID" 1
sleep 1

kitty @ --to "$SOCKET" get-text --extent selection >"$SELECTION_FILE"
xsel --clipboard --output >"$CLIPBOARD_FILE" 2>/dev/null || true

xdotool key --window "$WINDOW_ID" ctrl+q

for _ in $(seq 1 30); do
  if [[ -f "$CAPTURE" ]] && python3 - "$CAPTURE" <<'PY'
import sys
from pathlib import Path
capture = Path(sys.argv[1]).read_bytes()
sys.exit(0 if b"\x1b[<u" in capture and b"\x1b[?1007l" in capture else 1)
PY
  then
    break
  fi

  sleep 1
done

rm -f "$SOCKET_PATH"

cp "$CONFORMANCE_DIR/events.jsonl" "$EVENTS"

python3 "$ROOT_DIR/tests/conformance/linux/assert-conformance.py" \
  --events "$EVENTS" \
  --selection "$SELECTION_FILE" \
  --ansi-capture "$CAPTURE" \
  --expect-tmux true \
  --expect-tmux-mouse off \
  --expect-arrows \
  --expect-selection "SELECTABLE" \
  --expect-submitted "SELECTABLE" \
  --require-no-mouse-tracking
