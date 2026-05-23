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

