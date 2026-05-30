# Termina 0.10.2

**Release Date:** May 30, 2026

## Bug Fixes

### Input Parsing & CSI Sequences
- **[#232](https://github.com/Aaronontheweb/termina/pull/232)** — Fix unrecognized CSI tilde sequences (`[5~`, `[6~`, etc.) being silently swallowed by the escape sequence parser, causing the parser to stay stuck in `InBracketSequence` mode. These sequences now flush correctly as raw `KeyPressed` events so applications can handle them ([#232](https://github.com/Aaronontheweb/termina/pull/232))
- Fix legacy CSI tilde key decoding — maps legacy CSI tilde key sequences to semantic `ConsoleKey` events while preserving unknown tilde raw flush behavior

### Rendering
- **[#233](https://github.com/Aaronontheweb/termina/pull/233)** — Fix `TextInputNode` placeholder not respecting the configured `Background` color. Placeholders now render with the correct background fill against the configured terminal background

### Tracing & Logging
- **[#234](https://github.com/Aaronontheweb/termina/pull/234)** — Fix `FileTraceListener` encoding to use UTF-8 (without BOM) instead of `Encoding.Default`, preventing `ArgumentException` on Unicode characters (e.g., ➭ U+27AD) and the resulting consumer task faults, channel fill, and OOM issues in trace output
- Fix `DisposeAsync` drain order — `_disposed` flag is now set *after* waiting for the consumer to drain, preventing buffered events from being skipped and leaving trace files empty

## Test Fixes
- **[#236](https://github.com/Aaronontheweb/termina/pull/236)** — Make `SelectionItemContent` invalidation test deterministic
