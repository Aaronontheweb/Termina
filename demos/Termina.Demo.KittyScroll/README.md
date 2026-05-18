# Termina.Demo.KittyScroll

Prototype of "Option B" wheel-vs-arrow disambiguation from PR #215:

- **Kitty keyboard protocol** (`CSI > 8 u`, `report_all_keys`) forces every
  keyboard event through `CSI ... u` / `CSI 1;mods X`.
- **`?1007h` alternate-scroll mode** keeps mouse-wheel ticks on the legacy
  cursor-key path (`SS3 OA/OB` under DECCKM on).
- Because Ghostty's mouse subsystem ignores the kitty enhancement, wheel and
  arrows are now structurally distinct at the byte level.
- We never enable full mouse tracking (`?1000h`/`?1006h`), so native
  click-drag text selection keeps working.

## Run it

```bash
TERMINA_UNIX_RAW_INPUT=1 TERMINA_KITTY_KEYBOARD=8 \
  dotnet run --project demos/Termina.Demo.KittyScroll
```

Tested terminals (expected behavior):

| Terminal      | Kitty support | Result |
|---------------|---------------|--------|
| Ghostty       | ✅             | Wheel scrolls, arrows don't, selection works |
| kitty         | ✅             | Same |
| WezTerm       | ✅             | Same |
| foot          | ✅             | Same |
| iTerm2 ≥ 3.5  | ✅             | Same |
| macOS Terminal | ❌            | Falls back to today's ambiguity (wheel may register as arrow) |

## What to verify

1. **Scroll the wheel over the history panel** — the history should scroll,
   the `Wheel ↑/↓` counters should tick.
2. **Press the bare arrow keys** — the `Arrow ↑/↓` counters should tick, and
   the history should **not** move.
3. **Click and drag with the mouse** — your terminal's native selection
   highlight should appear over the text.
4. **`Ctrl+Q`** quits cleanly; the kitty enhancement is popped via `CSI < u`
   before termios is restored.

## Why two env vars

- `TERMINA_UNIX_RAW_INPUT=1` switches the framework off `Console.ReadKey`
  (which would fold `CSI A` and `SS3 OA` to the same `UpArrow` before our
  parser ever sees them) onto the raw-stdin `UnixConsole`.
- `TERMINA_KITTY_KEYBOARD=8` tells `UnixConsole` to push the kitty keyboard
  flags. `8` = `report_all_keys`. Other useful values: `9` = `8|1`
  (disambiguate + report all), `11` = `8|2|1` (also report event types).
