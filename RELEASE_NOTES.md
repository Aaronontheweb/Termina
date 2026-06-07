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

#### 0.10.1 May 24th 2026 ####

**Bug Fixes** — `StreamingTextNode` thread-safety and disposal hardening:

- **`RebuildBuffer` correctly inserts a newline before block segments after `Clear()`** ([#224](https://github.com/Aaronontheweb/termina/pull/224))
  - Pre-fix, after `Clear()` reset `HasContentOnCurrentLine`, the first re-added block segment skipped its leading newline during rebuild, causing block content to collide with prior content on the same line.
  - Moves the block-newline check inline, evaluated *after* prior elements are re-appended so the buffer state reflects the in-progress reconstruction.

- **Animation callbacks now hold `_contentLock`** ([#225](https://github.com/Aaronontheweb/termina/pull/225))
  - Animation `Invalidated` callbacks from `IAnimatedTextSegment` (e.g. `SpinnerSegment` on the R3 timer thread) previously called `RebuildBuffer` without locking, racing with public mutators (`Append`/`AppendTracked`/`Remove`/`Replace`/`Clear`/`Dispose`) that all hold the lock. Symptoms ranged from `InvalidOperationException` ("Collection was modified") on the `foreach` inside `RebuildBuffer` to torn buffer state and `ObjectDisposedException` if `Dispose()` ran while a callback was queued behind the lock.
  - Funnels both callback sites through a single `OnAnimationInvalidated()` helper that takes `_contentLock`, checks a new volatile `_disposed` flag, then runs `RebuildBuffer` + notification safely.
  - `Dispose()` is now idempotent and sets `_disposed = true` first so in-flight callbacks bail before touching `_invalidated`.

- **`NotifyChanged` hardened against post-Dispose race** ([#227](https://github.com/Aaronontheweb/termina/pull/227))
  - Every public mutator released `_contentLock` before calling `NotifyChanged()`, so a racing `Dispose()` could complete `_invalidated.OnCompleted()/Dispose()` in the gap and the mutator's `OnNext` would throw `ObjectDisposedException`. Same shape applied to deliberate use-after-dispose calls.
  - `NotifyChanged` now reads `_disposed` and try/catches `ObjectDisposedException`. `OnAnimationInvalidated` routes through `NotifyChanged` so the animation path inherits the same hardening.

**Test improvements** ([#226](https://github.com/Aaronontheweb/termina/pull/226)):

- Restructured the `Replace` thread-safety stress test so the ticker always fires on a live, permanently-tracked spinner (was mostly hitting an already-disposed segment after each `Replace`).
- Added a reflection-based test that directly exercises the `_disposed` belt-and-suspenders guard inside `OnAnimationInvalidated` (the original `AfterDispose` test was passing for the wrong reason because subscription teardown was hiding the in-flight-callback path).

---

#### 0.10.0 May 24th 2026 ####

**New Features**:
- **`ReactivePage.InvalidateLayout()` for runtime layout rebuilds** ([#220](https://github.com/Aaronontheweb/termina/pull/220))
  - New `protected void InvalidateLayout()` method on `ReactivePage<TViewModel>` that discards the cached layout tree and rebuilds via `BuildLayout()`
  - Solves the problem of layout values "baked in" at first navigation time (e.g. `SizeConstraint.Auto` records) being permanently frozen — consumers can now trigger a full rebuild when external state changes (terminal resize, etc.)
  - Preserves user subscriptions, key bindings, and focus when the target node survives the rebuild
  - Exception-safe: builds the new tree first, then atomically swaps — if `BuildLayout` throws, the existing layout is left intact
  - Includes 17 comprehensive lifecycle tests covering disposed-guard, re-entrancy, idempotent dispose, and subscription cleanup

- **Alternate-scroll wheel mode with Kitty keyboard protocol disambiguation** ([#215](https://github.com/Aaronontheweb/termina/pull/215))
  - Replaces SGR mouse tracking with `?1007h` alternate-scroll mode for wheel events, combined with Kitty keyboard protocol (`report_all_keys`) for true key disambiguation
  - On Ghostty, kitty, WezTerm, foot, and iTerm2 ≥ 3.5: scroll-wheel scrolls `IScrollable` components, arrow keys act as arrows even in always-focused text inputs, and native text selection (click-drag) still works
  - New opt-in `TERMINA_UNIX_RAW_INPUT=1` environment variable enables raw Unix stdin for the protocol to work
  - Dual Ctrl+C quit pattern replaces per-demo Ctrl+Q convention

- **Harden alternate-scroll rollout and document runtime input modes** ([#217](https://github.com/Aaronontheweb/termina/pull/217))
  - Restored legacy mouse tracking as the framework default — alternate-scroll is now opt-in via explicit app configuration
  - Moved Kitty keyboard negotiation into `TerminaApplication` with safer raw-input fallback on non-interactive consoles
  - Added comprehensive website docs covering runtime input modes, raw input, alternate-scroll, Kitty flags, and tmux passthrough

**Bug Fixes**:
- **`ResizeEvent` now forwards to `ViewModel.Input` observable** ([#219](https://github.com/Aaronontheweb/termina/pull/219))
  - Fixed `TerminaApplication.ProcessEvent` swallowing `ResizeEvent` via early `return;` — the event now correctly falls through to `_inputSubject.OnNext(inputEvent)`
  - Pages and ViewModels subscribing via `ViewModel.Input.OfType<ResizeEvent>()` now receive resize notifications as intended
  - Enables consumers to recompute width- or height-sensitive layout on terminal resize

---
