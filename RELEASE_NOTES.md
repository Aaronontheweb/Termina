# Release Notes — Termina 0.14.0-beta.1

**Release date:** 2026-06-18

####

This is the first beta of Termina 0.14.0 — a major internal rework of the rendering lifecycle and layout service infrastructure.

**Render Loop Frame Provider (#306)**

Added `TerminaRenderFrameProvider` — a centralized rendering loop that drives frame updates for the application. This replaces ad-hoc rendering triggers with a single, configurable frame loop that applications can opt into via `TerminaRuntimeOptions`.

- `TerminaRenderFrameProvider` — New service that manages the application render loop
- `TerminaRuntimeOptions` — New configuration for render loop settings (frame interval, mode)
- `ObservableLayoutExtensions` — New extension methods for reactive layout updates tied to frame intervals
- Updated demo apps and tutorials to use the new frame provider pattern

**Layout Runtime Context**

Added `LayoutRuntimeContext` — an app-owned layout runtime service that centralizes layout services (timing, frame info, rendering coordination) for all layout nodes. This moves component timing infrastructure onto a single, consistent path.

- `LayoutRuntimeContext` — New runtime context for app-owned layout nodes
- Component timing — Moved from scattered implementations to centralized runtime context
- Updated all demos, docs, and tests to use the single runtime-services path

**Internal Improvements**

- Refactored `ReactiveViewModel` and `ReactivePage` to work with the new rendering context
- Updated `TextInputBaseNode`, `TextAreaNode`, `SpinnerNode`, and `WizardNode` to consume runtime context
- Added comprehensive tests for `RenderFrameProvider` and `LayoutRuntimeContext`
- Updated tutorials (streaming chat, wizard app) to reflect new patterns

---

### Contributors

Aaron Stannard
