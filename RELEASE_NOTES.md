# Release Notes — Termina 0.15.0

**Release date:** 2026-07-01

####

**Bug Fixes**

- **Fixed CJK and Unicode display width handling** ([#321](https://github.com/Aaronontheweb/termina/pull/321))
  - Terminal rendering now correctly accounts for East Asian and wide Unicode character widths in layout and cursor behavior

**Dependency Updates**

- Updated `actions/setup-dotnet` from 5.2.0 to 5.3.0 ([#270](https://github.com/Aaronontheweb/termina/pull/270))
- Updated `Microsoft.Extensions.TimeProvider.Testing` from 10.6.0 to 10.7.0 ([#288](https://github.com/Aaronontheweb/termina/pull/288))
- Updated `dotnet-sdk` from 10.0.201 to 10.0.301 ([#290](https://github.com/Aaronontheweb/termina/pull/290))
- Updated `Microsoft.NET.Test.Sdk` from 18.6.0 to 18.7.0 ([#319](https://github.com/Aaronontheweb/termina/pull/319))

####

#### 0.14.0 June 23rd 2026 ####

**New Features**:

- **Gradient primitives, GraphNode, and ProgressBarNode** ([#298](https://github.com/Aaronontheweb/termina/pull/298))
  - New gradient system for rich visual theming
  - `GraphNode` for data visualization and flow diagrams
  - `ProgressBarNode` for animated progress indicators

- **Toast notifications with colors and icons** ([#296](https://github.com/Aaronontheweb/termina/pull/296))
  - Enhanced `ToastNode` with configurable foreground/background colors
  - Icon support for status differentiation

- **Modal footers with configurable colors** ([#292](https://github.com/Aaronontheweb/termina/pull/292), [#293](https://github.com/Aaronontheweb/termina/pull/293))
  - `ModalNode` now supports `WithFooter` and `WithFooterColor` for adding a styled footer section
  - Footer renders below modal content with optional color theming
  - Includes a Gallery demo showcasing the feature on TodoList modals

- **Render loop frame provider** ([#306](https://github.com/Aaronontheweb/termina/pull/306))
  - New `IRenderLoopFrameProvider` for deterministic frame timing control
  - Enables precise animation and layout update scheduling

**Bug Fixes**:

- **Suppressed stale pre-swap frames on deferred navigation** ([#315](https://github.com/Aaronontheweb/termina/pull/315))
  - Fixed visual artifacts when navigation is queued from an input handler during render swap

- **Eliminated no-op resize full refresh** ([#312](https://github.com/Aaronontheweb/termina/pull/312))
  - Resizes that don't change bounds no longer trigger expensive full layout refresh

- **Corrected ScrollableContainerNode bounds.Y handling** ([#301](https://github.com/Aaronontheweb/termina/pull/301))
  - `ScrollableContainerNode` now respects `bounds.Y` instead of overwriting layout above it
  - Fixes layout stacking issues in scrollable containers

- **Fixed CSI Z (backtab/Shift+Tab) parsing** ([#297](https://github.com/Aaronontheweb/termina/pull/297))
  - CSI Z sequences now correctly map to Tab with Shift modifier instead of being misinterpreted

**Dependency Updates**:

- Updated `Akka.Hosting` from 1.5.68 to 1.5.69 ([#299](https://github.com/Aaronontheweb/termina/pull/299))
- Updated `OpenTelemetry.Api` from 1.15.3 to 1.16.0 ([#291](https://github.com/Aaronontheweb/termina/pull/291))

---

# Release Notes — Termina 0.14.0-beta.3

**Release date:** 2026-06-22

####

This is the third beta of Termina 0.14.0 — a focused render-loop stability release for deferred navigation.

**Bug Fixes**

- Suppressed stale pre-swap frames when deferred navigation is queued from an input handler (#314)

---

### Contributors

Aaron Stannard
