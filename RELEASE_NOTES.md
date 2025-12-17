#### 0.3.0 December 17th 2025 ####

**New Features**:
- **Inline animated text segments with tracked segment support** ([#71](https://github.com/Aaronontheweb/Termina/pull/71))
  - Opt-in tracked segments for `StreamingTextNode` enabling inline animations (spinners, timers, etc.)
  - Caller-provided `SegmentId` system (like HTML div IDs) for tracking and manipulating segments
  - New interfaces: `ITextSegment`, `IAnimatedTextSegment`, `ICompositeTextSegment`
  - `SpinnerSegment` component with 6 animation styles: Dots, Line, Arrow, Bounce, Box, Circle
  - `StaticTextSegment` for trackable static text
  - Methods: `AppendTracked(id, segment)`, `Replace(id, segment, keepTracked)`, `Remove(id)`
  - Interface-based message protocol with `IChatMessage` for clean chat operations
  - Enables any inline animated element (timers, progress bars, blinks, highlighters) with minimal overhead

**Bug Fixes**:
- **Fix modal text input not accepting keystrokes** ([#70](https://github.com/Aaronontheweb/Termina/issues/70), [#74](https://github.com/Aaronontheweb/Termina/pull/74))
  - Critical fix where `TextInputNode` in modal dialogs would be immediately disposed after being focused
  - Fixed `ReactiveLayoutNode` lifecycle to call `OnDeactivate()` instead of `Dispose()` when switching children
  - Fixed modal focus propagation - `ModalNode` now forwards `OnFocused()` and `OnBlurred()` to content
  - Fixed `ReactiveLayoutNode` to call `OnActivate()` on new children when they are dynamically swapped in
  - Fixed render loop invalidation to properly propagate through container hierarchy
  - Users can now properly type in modal dialogs without `ObjectDisposedException`

- **Fix NavigationBehavior.PreserveState layout disposal issue** ([#67](https://github.com/Aaronontheweb/Termina/issues/67), [#69](https://github.com/Aaronontheweb/Termina/pull/69))
  - Implemented Active/Inactive State Pattern for layout nodes to prevent `ObjectDisposedException` when navigating
  - Added `IActivatableNode` interface defining `OnActivate`/`OnDeactivate` lifecycle methods
  - Layout nodes now pause/resume instead of dispose/recreate on navigation with PreserveState behavior
  - `ReactivePage` now builds layout once and preserves it across navigations
  - `TextInputNode`, `SpinnerNode`, `ReactiveLayoutNode`, `ConditionalNode`, `SelectionListNode`, `ModalNode`, and `ContainerNode` all properly implement lifecycle propagation
  - `TerminaApplication` now properly disposes cached pages via `IDisposable` implementation
  - Fixes race conditions and ensures proper resource cleanup

**Improvements**:
- **Add TERMINA002 analyzer and refactor MVVM architecture** ([#68](https://github.com/Aaronontheweb/Termina/pull/68))
  - New Roslyn analyzer (TERMINA002) detects layout nodes incorrectly stored as fields/properties in ViewModels
  - Refactored MVVM pattern: ViewModel owns Input (public) for business logic, Page owns Focus for interactive control
  - ViewModels now handle keyboard input directly in `OnActivated()`
  - Pages can access `ViewModel.Input` when routing to interactive layout nodes
  - Updated documentation to reflect clearer separation of concerns
  - Reduces ceremony while maintaining clean MVVM architecture

- **Documentation improvements** ([#66](https://github.com/Aaronontheweb/Termina/pull/66))
  - Added quick install section to docs homepage with NuGet badges and installation commands
  - Improved documentation discoverability for new users
  - Fixed docs deployment workflow trigger

---

#### 0.2.1 December 16th 2025 ####

**Improvements**:
- **Diff-based rendering system** ([#61](https://github.com/Aaronontheweb/Termina/pull/61))
  - Implemented double-buffering terminal wrapper that eliminates screen flickering
  - Only outputs changed cells on flush instead of clearing entire screen
  - New `DiffingTerminal` wrapper with `FrameBuffer` for efficient cell-level diffing
  - Uses same proven pattern as ncurses and termbox for flicker-free rendering
  - `TerminaApplication` automatically wraps terminals with `DiffingTerminal`
  - Added `ForceFullRefresh()` for complete redraw when needed (resize/corruption)

**Bug Fixes**:
- **Fix terminal state cleanup on application exit** ([#62](https://github.com/Aaronontheweb/Termina/pull/62))
  - Properly restore terminal when Termina application exits
  - Disable mouse tracking if enabled
  - Reset colors and text attributes
  - Flush buffered ANSI commands
  - Prevents visual artifacts and inconsistent terminal state after exit
  - Fixes [#44](https://github.com/Aaronontheweb/Termina/issues/44)

---

#### 0.2.0 December 16th 2025 ####

**Breaking Changes**:
- **Pure reactive architecture** ([#50](https://github.com/Aaronontheweb/Termina/pull/50))
  - All .NET events migrated to `IObservable<T>` for consistency with System.Reactive
  - `IInvalidatingNode.Invalidated`: `event Action?` → `IObservable<Unit>`
  - `TextInputNode`: `Submitted`, `TextChanged`, `Invalidated` now use observables
  - Other components (`SpinnerNode`, `ConditionalNode`, `ReactiveLayoutNode`, `ScrollableContainerNode`, `StreamingTextNode`) migrated to observables
  - Rendering layer events migrated: `OnSubmit` → `Submitted`, `OnDirty` → `Dirty`
  - **Migration required**: Replace event subscriptions (`+=`) with observable subscriptions (`.Subscribe()`)
  - Use `.DisposeWith()` for automatic subscription cleanup in ViewModels

**New Features**:
- **Focus management system** ([#51](https://github.com/Aaronontheweb/Termina/pull/51))
  - Stack-based focus management for interactive components
  - `IFocusable` interface for components that can receive keyboard focus
  - `IFocusManager` with priority-based focus routing
  - `TerminaApplication` now routes keyboard input through focus manager
  - `ReactiveViewModel` exposes `Focus` property for managing focus state
- **ModalNode component** ([#51](https://github.com/Aaronontheweb/Termina/pull/51))
  - Overlay component for modal dialogs and selection prompts
  - Configurable backdrop styles: Transparent, Dim, Solid
  - Positioning options: Center, Top, Bottom
  - Escape key dismissal support
  - Integrates with focus management system
  - Created via `Layouts.Modal()` factory method
- **SelectionListNode component** ([#51](https://github.com/Aaronontheweb/Termina/pull/51))
  - Keyboard-navigable selection lists with single/multi-select modes
  - Number key shortcuts (1-9) for quick selection
  - Home/End navigation and automatic scrolling
  - Optional "Other" option for custom text input
  - Typed items support with `SelectionListNode<T>`
  - Created via `Layouts.SelectionList()` factory methods
  - Exposes `SelectionConfirmed` and `SelectionCancelled` observables
- **DeferredNode component** ([#51](https://github.com/Aaronontheweb/Termina/pull/51))
  - Non-owning node delegation pattern for reactive layouts
  - Prevents `ObjectDisposedException` when conditionally showing/hiding nodes
  - Useful for modal dialogs that shouldn't be disposed when hidden
  - Created via `Layouts.Deferred()` factory method
- **Inline styled text support** ([#48](https://github.com/Aaronontheweb/Termina/pull/48))
  - Structured API for colored and decorated text in `StreamingTextNode`
  - `TextDecoration` flags: Bold, Dim, Italic, Underline, Strikethrough
  - `TextStyle` record combining foreground, background, and decorations
  - `StyledSegment` and `StyledLine` for composing styled text
  - `StyledWordWrapper` preserves styles across word wrap boundaries
  - New styled `Append`/`AppendLine` overloads on `StreamingTextNode`
  - Avoids escaping issues with LLM output and user content

**Bug Fixes**:
- **Fix auto-disposal of reactive fields** ([#49](https://github.com/Aaronontheweb/Termina/pull/49))
  - Source generator now creates `DisposeReactiveFields()` method for classes with `[Reactive]` fields
  - Auto-generates `Dispose()` override for `ReactiveViewModel` subclasses
  - Added TERMINA001 compiler error when custom `Dispose()` doesn't call `DisposeReactiveFields()`
  - Prevents `BehaviorSubject` backing field leaks

**Improvements**:
- Updated documentation with new components (ModalNode, SelectionListNode, DeferredNode)
- Added `WithChild()` method to `StackLayout` for fluent API consistency
- `TextInputNode` now implements `IFocusable` for modal integration
- Enhanced Todo demo showcasing modal dialogs and two-step input flows
- 48 new unit tests covering focus management, modals, and selection lists

---

#### 0.1.0 December 15th 2025 ####

First stable release of Termina - a reactive terminal UI (TUI) framework for .NET.

**Major Changes**:

This release represents a complete architectural redesign from the beta, removing the Spectre.Console dependency and introducing a custom ANSI terminal rendering system with a declarative layout API.

**Breaking Changes**:
- **Complete removal of Spectre.Console dependency** ([#33](https://github.com/Aaronontheweb/Termina/pull/33))
  - Replaced with custom ANSI terminal rendering system for direct terminal control
  - New tree-based declarative layout API replaces component-based approach
  - Surgical region-based updates for better performance
  - New layout components: `TextNode`, `PanelNode`, `TextInputNode`, `ScrollableContainerNode`, `StreamingTextNode`, `SpinnerNode`
  - New layout containers: `Layouts.Vertical()` and `Layouts.Horizontal()` with fluent API
  - Size constraint system: `Fixed`, `Fill`, `Auto`, `Percent`
  - Pages now use `BuildLayout()` returning `ILayoutNode` instead of render-based approach
  - **Migration required**: All beta applications will need to be rewritten using the new declarative layout API

**New Features**:
- **Live documentation website** ([#40](https://github.com/Aaronontheweb/Termina/pull/40))
  - Comprehensive VitePress documentation now available at https://aaronstannard.com/Termina/
  - Includes getting started guides, tutorials, component reference, and advanced topics
  - Code examples synced with actual source files
  - Automated deployment on releases
- **Word wrapping support** ([#38](https://github.com/Aaronontheweb/Termina/pull/38))
  - `TextNode` automatically wraps text to multiple lines when exceeding available width
  - Configurable via `WordWrap` property and `NoWrap()` fluent method
- **Automatic terminal resize detection** ([#37](https://github.com/Aaronontheweb/Termina/pull/37))
  - UI automatically adjusts when terminal window is resized
  - Cross-platform polling approach works on all platforms
- **Streaming text components** ([#24](https://github.com/Aaronontheweb/Termina/pull/24))
  - Two-tier buffer architecture with Persisted and Windowed modes
  - `StreamingTextNode` for chat interfaces, logs, and LLM output
  - Word wrapping support for streaming content
  - Includes streaming chat demo with Akka.NET

**Bug Fixes**:
- Fix `TextInputNode` timer disposal race condition preventing crashes during shutdown ([#34](https://github.com/Aaronontheweb/Termina/pull/34))
- Fix `PreserveState` subscription leak by auto-disposing subscriptions in `ReactiveViewModel` ([#35](https://github.com/Aaronontheweb/Termina/pull/35))
- Fix reactive layout disposal bug ([#39](https://github.com/Aaronontheweb/Termina/pull/39))

**Improvements**:
- XML documentation now generated for all public APIs
- Enhanced demos moved to dedicated `demos/` folder
- Better AOT/trimming support with analyzer packaging improvements

---

#### 0.1.0-beta1 December 11th 2025 ####

Initial release of Termina - a reactive terminal UI (TUI) framework for .NET.

**Features**:
- Reactive MVVM architecture with source-generated properties
- ASP.NET Core-style routing with parameterized routes and type constraints
- Two-tier event architecture (pages and navigation)
- Virtualizable input support for large datasets
- Built on Spectre.Console for rich terminal rendering
- Full AOT/trimming compatibility
