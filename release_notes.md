#### 0.11.0 June 7th 2026 ####

**Terminal Input Reliability and Native Copy/Paste Support**

- Added an internal semantic input pipeline that decodes terminal protocol input before adapting back to Termina's existing public input events.
- Extracted protocol-specific decoders for SGR mouse input, SS3 keyboard input, CSI functional keys, legacy CSI keyboard sequences, Kitty keyboard formats, bracketed paste, and unknown-sequence fallback handling.
- Preserved the existing public input surface: `KeyPressed`, `MouseScrollEvent`, `PasteEvent`, and `ResizeEvent`.
- Expanded Linux terminal conformance coverage for baseline PTY, tmux, kitty, and kitty plus tmux scenarios.
- Added real terminal selection, explicit clipboard copy, and paste-into-focused-input coverage to validate native terminal clipboard workflows.
- Hardened file trace disposal so buffered trace events drain deterministically.

**Upgrade Notes**

- Existing applications continue using `LegacyMouseTracking` by default.
- Applications that need native terminal text selection should opt into raw input plus alternate-scroll.
- Built-in text inputs handle bracketed paste automatically.
- Custom focusable input components should implement `IPasteReceiver` to receive focused paste content.
- For tmux, enable passthrough with `set -g allow-passthrough on`.
- When tmux mouse mode captures selection, use Shift-drag plus the terminal copy shortcut, usually `Ctrl+Shift+C`.
- See the [0.11.0 upgrade advisory](https://aaronstannard.com/termina/guide/upgrade-0.11.html) for the recommended runtime configuration and tmux workflow.

---

#### 0.10.2 May 30th 2026 ####

**Bug Fixes** — Input parsing, rendering, and tracing:

- **Unrecognized CSI tilde sequences now flush correctly** ([#232](https://github.com/Aaronontheweb/termina/pull/232))
  - Fix for `[5~` (PgUp), `[6~` (PgDn) and other bracketed CSI sequences being silently swallowed by the escape sequence parser. These were causing the parser to stay stuck in `InBracketSequence` mode, blocking all subsequent input. Sequences now flush as raw `KeyPressed` events for application handling.

- **Legacy CSI tilde key decoding** — Maps legacy CSI tilde key sequences to semantic `ConsoleKey` events while preserving unknown tilde raw flush behavior.

- **`TextInputNode` placeholder background color fixed** ([#233](https://github.com/Aaronontheweb/termina/pull/233))
  - Placeholders now render with the correct background fill against the configured `Background` color instead of inheriting the terminal's default.

- **`FileTraceListener` UTF-8 encoding fixed** ([#234](https://github.com/Aaronontheweb/termina/pull/234))
  - Switched from `Encoding.Default` to UTF-8 (without BOM), preventing `ArgumentException` on Unicode characters (e.g., ➭ U+27AD) and the resulting consumer task faults, channel fill, and OOM issues in trace output.
  - `DisposeAsync` drain order fixed — `_disposed` flag is now set *after* waiting for the consumer to drain, preventing buffered events from being skipped and leaving trace files empty.

**Test Fixes**
- **`SelectionItemContent` invalidation test made deterministic** ([#236](https://github.com/Aaronontheweb/termina/pull/236))
