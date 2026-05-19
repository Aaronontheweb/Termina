# Termina.Demo.KittyScroll

Prototype of "Option B" wheel-vs-arrow disambiguation from PR #215:

- **Kitty keyboard protocol** (`CSI > 8 u`, `report_all_keys`) forces every
  keyboard event through `CSI ... u` / `CSI 1;mods X`.
- **`?1007h` alternate-scroll mode** keeps mouse-wheel ticks on the legacy
  cursor-key path (`SS3 OA/OB` under DECCKM on, or bare `CSI A/B` otherwise).
- Because the terminal's mouse subsystem ignores the kitty enhancement, wheel and
  arrows are now structurally distinct at the byte level.
- We never enable full mouse tracking (`?1000h`/`?1006h`), so native
  click-drag text selection keeps working.

## Run it

### macOS / Linux (bash / zsh)

```bash
TERMINA_RAW_INPUT=1 TERMINA_KITTY_KEYBOARD=8 \
  dotnet run --project demos/Termina.Demo.KittyScroll
```

### Windows (PowerShell)

```powershell
$env:TERMINA_RAW_INPUT="1"; $env:TERMINA_KITTY_KEYBOARD="8"; `
  dotnet run --project demos/Termina.Demo.KittyScroll
```

> Note: the legacy `TERMINA_UNIX_RAW_INPUT` name still works on Unix as a
> deprecated alias but logs a warning — prefer `TERMINA_RAW_INPUT`.

Tested terminals (expected behavior):

| Terminal              | Kitty support | `?1007h` | Result |
|-----------------------|---------------|----------|--------|
| Ghostty               | ✅            | ✅       | Wheel scrolls, arrows don't, selection works |
| kitty                 | ✅            | ✅       | Same |
| WezTerm               | ✅            | ✅       | Same |
| foot                  | ✅            | ✅       | Same |
| iTerm2 ≥ 3.5          | ✅            | ✅       | Same |
| Windows Terminal 1.21+ | ✅           | ✅       | Same (requires `TERMINA_RAW_INPUT=1`) |
| Windows Terminal 1.16–1.20 | ❌       | ✅       | Wheel scrolls but real arrows also scroll (no kitty disambiguation) |
| macOS Terminal        | ❌            | ❌       | Falls back to today's ambiguity (wheel may register as arrow) |
| Windows conhost (cmd.exe direct) | ❌ | ❌      | Not supported — use Windows Terminal |

## What to verify

1. **Scroll the wheel over the history panel** — the history should scroll,
   the `Wheel ↑/↓` counters should tick, even while the input box is focused.
2. **Type into the input and press Enter** — the message should echo into the
   history as `you> …` / `bot> echo: …`. Arrow keys move the cursor within
   the input, they do NOT scroll the history (that's the disambiguation).
3. **Click and drag with the mouse** — your terminal's native selection
   highlight should appear over the text.
4. **Press Ctrl+C once** — a `Press Ctrl+C again to quit` toast appears.
   Press Ctrl+C again within 2s to exit cleanly. The kitty enhancement is
   popped via `CSI < u` before termios / Windows console mode is restored.

## Why two env vars

- `TERMINA_RAW_INPUT=1` switches the framework off `Console.ReadKey` /
  Windows record-mode (which would fold `CSI A` and `SS3 OA` to the same
  `UpArrow` before our parser ever sees them) onto the raw-byte console
  implementation for the current platform (`UnixConsole` or `WindowsConsole`'s
  raw-VT mode).
- `TERMINA_KITTY_KEYBOARD=8` tells the raw-byte console to push the kitty
  keyboard flags. `8` = `report_all_keys`. Other useful values: `9` = `8|1`
  (disambiguate + report all), `11` = `8|2|1` (also report event types).
