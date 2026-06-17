# Release Notes — Termina 0.13.0

**Release date:** 2026-06-17

## Features

### Gradient primitives, GraphNode, and ProgressBarNode (#298) — Jan (@st0o0)

A major visual components update with gradient coloring support across the board.

- **`Color.Lerp`** — Linear RGB interpolation between two colors
- **`Gradient`** — Immutable multi-stop gradient with color stop interpolation
- **`GraphNode`** — Reactive, self-invalidating layout node for live scrolling graphs in four styles:
  - `Blocks` — Filled block columns (▁▂▃▄▅▆▇█)
  - `Outline` — Top edge only, sparkline feel
  - `Braille` — Double vertical resolution using braille characters
  - `Ascii` — ASCII fallback for terminals without Unicode
  - All styles support gradient coloring by row height
  - Configurable timer interval or data-driven updates
- **`ProgressBarNode`** — Single-row progress bar with:
  - Gradient fill across the bar width
  - Configurable fill/empty characters and colors
  - Optional formatted label (e.g., `"{0:P0}"` for percentage)
- Full API docs and interactive gallery demo

### Color and icon support for toast notifications (#296) — Jan (@st0o0)

Toast notifications can now be customized with colors, icons, and positions.

- **`ToastOptions.Color`** — Optional color override for toast border
- **`ToastOptions.Icon`** — Optional icon string displayed in the toast
- Built-in presets:
  - **Success** — Green border, ✓ icon, Top Right
  - **Error** — Red border, ✗ icon, Top Right
  - **Warning** — Yellow border, ⚠ icon, Top Center
  - **Info** — Cyan border, ℹ icon, Bottom Right
  - **Custom** — Magenta border, 🚀 icon, Bottom Center
  - **Default** — No overrides (unchanged behavior)

## Bug Fixes

### Parse CSI Z (backtab) as Shift+Tab (#297) — Jan (@st0o0)

In legacy terminal mode, the CSI Z escape sequence (`ESC [ Z`) was silently dropped. It now correctly maps to `Shift+Tab`, restoring expected backward-tab behavior.

### ScrollableContainerNode ignoring bounds.Y (#301)

`ScrollableContainerNode` was ignoring `bounds.Y` during layout, causing it to overwrite content above the scrollable area. The layout calculation now correctly respects vertical bounds.

## Docs

- Embedded animated GIF demos for Toast and Graph/Progress components (#302)

---

### Contributors

Thanks to **Jan (@st0o0)** for these contributions!
