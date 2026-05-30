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
