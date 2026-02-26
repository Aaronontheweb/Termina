#### 0.7.0 February 26th 2026 ####

**Breaking Changes**:
- **Migrate from System.Reactive to R3** ([#140](https://github.com/Aaronontheweb/Termina/pull/140))
  - Replaced System.Reactive (Rx.NET) with [R3](https://github.com/Cysharp/R3) throughout the framework
  - `IObservable<T>` → `Observable<T>` in all public APIs
  - `OfType<T>()` → `OfType<TSrc, TDest>()` (R3 requires both type parameters)
  - `Select(...)` may require explicit type parameters: `Select<TIn, TOut>(...)`
  - `Subscribe` callbacks: `onError` → `onErrorResume`, `onCompleted` takes `Result` parameter
  - `BehaviorSubject<T>` removed — use `ReactiveProperty<T>` instead
  - `Subject<T>` now from R3 namespace
  - See [Migration Guide](https://aaronstannard.com/termina/guide/migration-0.7.html) for details

- **Replace `[Reactive]` source generator with `ReactiveProperty<T>`** ([#149](https://github.com/Aaronontheweb/Termina/pull/149))
  - Removed `[Reactive]` attribute and its source generator
  - Use `ReactiveProperty<T>` for all ViewModel state: `public ReactiveProperty<int> Count { get; } = new(0);`
  - Property access via `.Value`: `Count.Value++` instead of `Count++`
  - Page bindings subscribe directly to property: `ViewModel.Count.Select(...)` instead of `ViewModel.CountChanged.Select(...)`
  - ViewModels no longer need `partial` keyword (unless using `[FromRoute]`)
  - Manual `Dispose()` override required to dispose all `ReactiveProperty<T>` instances
  - Built-in `DistinctUntilChanged` — only emits when value actually changes

- **Replace timers with `Observable.Interval` + `TimeProvider`** ([#140](https://github.com/Aaronontheweb/Termina/pull/140))
  - All timer-based components now accept optional `TimeProvider` parameter
  - Enables deterministic testing with `FakeTimeProvider`
  - `System.Timers.Timer` and `System.Threading.Timer` no longer used

**New Features**:
- **DynamicLayoutNode** ([#147](https://github.com/Aaronontheweb/Termina/issues/147))
  - New `Layouts.Dynamic(Func<ILayoutNode> factory)` for imperative, page-local state
  - Re-evaluates factory on each Render/Measure cycle with reference equality to avoid lifecycle churn
  - `Invalidate()` method for programmatic trigger; `AsDynamicLayout(Observable<Unit>)` extension
  - Implements `IInvalidatingNode` for proper invalidation propagation

- **FocusManager auto-focus and Tab cycling** ([#146](https://github.com/Aaronontheweb/Termina/issues/146))
  - `FocusPolicy` enum: `Manual`, `FirstFocusable`, `ByPriority` — set on `ReactivePage` for automatic focus on navigation
  - `CollectFocusables(ILayoutNode root)` — depth-first tree walk collecting focusable nodes
  - `CycleFocus(focusables, reverse)` — next/previous with wrap-around
  - `CycleFocusForward()` / `CycleFocusBackward()` helpers on `ReactivePage`
  - `FocusManager` migrated from `BehaviorSubject<IFocusable?>` to `ReactiveProperty<IFocusable?>`

- **WizardNode** ([#148](https://github.com/Aaronontheweb/Termina/issues/148))
  - Multi-step wizard component: `Layouts.Wizard<TStep>()` where `TStep : struct, Enum`
  - Fluent builder: `.WithStep()`, `.WithProgressStyle()`, `.WithTitle()`, `.WithBorder()`
  - Navigation: `TryAdvance()`, `TryGoBack()`, `GoToStep()`, `AdvanceSubStep()`
  - Cancellable `BeforeAdvance` observable for step validation
  - `StepChanged` and `Completed` observables
  - Progress styles: `BlockBar`, `Arrow`, `Dots`, `None`
  - Sub-step support within each step
  - Implements `IFocusable` and `IInvalidatingNode`
  - Uses `DynamicLayoutNode` internally for step content switching

**Bug Fixes**:
- **Fix invalidation subscription lost after navigation round-trip** ([#150](https://github.com/Aaronontheweb/Termina/pull/150))
  - Fixed rendering bug where leaf nodes lost their invalidation subscriptions after navigating away and back
  - Custom `LayoutNode` subclasses should use `CreateSubContext(bounds)` in Render methods

**Cleanup**:
- Removed dead `HasReactiveAttribute` helper from `LayoutNodeInViewModelAnalyzer` (references removed `[Reactive]` attribute)

---

#### 0.6.1 February 25th 2026 ####

**New Features**:
- **Built-in input history for TextInputNode** ([#144](https://github.com/Aaronontheweb/Termina/pull/144))
  - New `WithHistory(maxEntries)` fluent API for enabling input history
  - New `AddHistory(text)` method for programmatically adding history entries
  - Up/Down arrow keys navigate through previous input submissions
  - Enter key automatically records non-empty text for future recall
  - Off by default for full backward compatibility
  - Move history logic from `StreamingChatViewModel` into `TextInputNode` for better reusability
  - Simplified `StreamingChatPage` by removing manual history wiring
  - Includes 17 new comprehensive tests covering navigation, eviction, and restoration
  - Updated text-input-node.md documentation

- **Auto-route PasteEvent to focused IPasteReceiver via layout tree walk** ([#143](https://github.com/Aaronontheweb/Termina/pull/143))
  - Automatic paste event routing when no focused `IPasteReceiver` exists
  - New `GetChildNodes()` virtual method on `LayoutNode` for tree traversal
  - Implemented in `ContainerNode`, `PanelNode`, `ModalNode`, `ScrollableContainerNode`, `ReactiveLayoutNode`, and `ConditionalNode`
  - Eliminates manual paste subscription boilerplate from pages
  - Paste routing order: focused receiver > tree walk > ViewModel.Input
  - Multi-line paste content in history recall shows as summary (e.g., `[Pasted 3 lines, 20 chars]`)
  - `TextInputNode.Text` setter automatically condenses multi-line content into paste-display mode
  - Includes comprehensive routing tests covering all scenarios

---

#### 0.6.0 February 23rd 2026 ####

**New Features**:
- **Scrollbar for StreamingTextNode** ([#138](https://github.com/Aaronontheweb/Termina/pull/138))
  - Visual scrollbar renders on the right edge of `StreamingTextNode` when content exceeds the viewport
  - `ScrollbarOptions` record for customizing track/thumb characters and colors, with `AutoHide` support
  - `WithScrollbar()` fluent method on `StreamingTextNode` for opt-in configuration
  - `GetMaxScrollOffset()` exposed on `PersistedStreamBuffer` for programmatic scroll position access
  - Closes [#135](https://github.com/Aaronontheweb/Termina/issues/135)

- **Bracketed paste mode for TextInputNode** ([#138](https://github.com/Aaronontheweb/Termina/pull/138))
  - `TextInputNode` now implements `IPasteReceiver` and handles terminal bracketed paste sequences
  - Multi-line pastes are captured as a single unit and displayed as a summary placeholder (e.g., `[Pasted 500 lines, 12345 chars]`)
  - Full pasted content (with newlines preserved) is submitted on Enter
  - Any edit action clears the paste and returns to normal input mode
  - Prevents multi-line pastes from triggering individual per-line submissions
  - `AnsiCodes.EnableBracketedPaste` / `DisableBracketedPaste` ANSI escape constants added
  - Closes [#134](https://github.com/Aaronontheweb/Termina/issues/134)

- **Mouse wheel scrolling for scrollable components** ([#138](https://github.com/Aaronontheweb/Termina/pull/138))
  - New `MouseScrollEvent` input event for mouse wheel up/down
  - New `IScrollable` interface for components that support scroll input
  - Mouse scroll events are automatically routed to the currently focused `IScrollable` component
  - `EscapeSequenceParser` detects SGR mouse scroll sequences
  - Closes [#136](https://github.com/Aaronontheweb/Termina/issues/136)

---

#### 0.5.1 December 19th 2025 ####

**Bug Fixes**:
- **Fix nullability propagation in [Reactive] source generator** ([#112](https://github.com/Aaronontheweb/Termina/pull/112))
  - Source generator now preserves nullable reference type annotations (e.g., `string?`, `int?`) in generated properties
  - Added custom `SymbolDisplayFormat` with `IncludeNullableReferenceTypeModifier` option
  - Nullable fields now generate properties with matching nullability annotations, providing correct nullability hints in consuming code
  - Previously, nullable fields like `string? _field` would generate non-nullable properties, losing nullability information

---

#### 0.5.0 December 18th 2025 ####

**New Features**:
- **BlockSegment wrapper for block-level streaming text** ([#108](https://github.com/Aaronontheweb/Termina/pull/108))
  - Enables text segments to render as block elements starting on new lines with vertical word wrapping
  - New `BlockSegment` class wraps any `ITextSegment` for block-level rendering
  - Fluent `.AsBlock()` API for creating block segments
  - `StreamingTextNode` automatically detects `BlockSegment` and inserts newlines
  - Supports both static and animated content (spinners, status indicators)
  - Perfect for LLM agent "thinking" displays and streaming content that should update in-place within a fixed area
  - Closes [#107](https://github.com/Aaronontheweb/Termina/issues/107)

- **Page-level key binding system with capture phase** ([#105](https://github.com/Aaronontheweb/Termina/pull/105))
  - Pages can now intercept keyboard input before focused components receive it
  - New `PageKeyBindings` class for registering page-level key handlers
  - Added `HandlePageInput` method to `IBindablePage` interface
  - `TerminaApplication.ProcessEvent()` now uses capture phase (page) then bubble phase (focused component)
  - `ReactivePage` exposes `KeyBindings` property for navigation key handling
  - Enables reliable Escape, Tab, and other navigation keys in complex pages
  - Closes [#104](https://github.com/Aaronontheweb/Termina/issues/104)

- **GridNode 2D layout primitive** ([#100](https://github.com/Aaronontheweb/Termina/pull/100))
  - New 2D grid layout enabling consistent column/row sizing across all cells
  - Cell addressing via `SetCell(row, col, node)` or fluent `AddRow()` API
  - Constraint-based sizing for columns and rows (Fixed, Auto, Fill, Percent)
  - Grid line rendering with Single, Double, Rounded, and Ascii border styles
  - Cell spanning support (`colspan` and `rowspan`) for merged header cells
  - Focus navigation with 2D arrow key support
  - Navigation modes: None, CellNavigation, ChildFocusRouting
  - Perfect for dashboards, data tables, and structured layouts

- **TextNode horizontal alignment** ([#100](https://github.com/Aaronontheweb/Termina/pull/100))
  - New `TextAlignment` enum: Left (default), Center, Right
  - Fluent methods: `Align()`, `AlignCenter()`, `AlignRight()`
  - Works seamlessly with GridNode cells and other layout containers

- **Component gallery demo** ([#103](https://github.com/Aaronontheweb/Termina/pull/103))
  - New `Termina.Demo.Gallery` project showcasing UI components
  - Interactive gallery pages for SelectionList, Grid, and other components
  - Run with: `dotnet run --project demos/Termina.Demo.Gallery`

**Bug Fixes**:
- **Fix SelectionListNode number prefixes** ([#103](https://github.com/Aaronontheweb/Termina/pull/103))
  - SelectionListNode now shows number prefixes for all items, not just items 1-9
  - Fixes [#101](https://github.com/Aaronontheweb/Termina/issues/101)

- **Fix SelectionListNode "Other" option auto-start** ([#103](https://github.com/Aaronontheweb/Termina/pull/103))
  - Navigating to "Other" option with arrow keys no longer auto-starts text input
  - Text input mode now requires explicit Enter or Space key press
  - Fixes [#102](https://github.com/Aaronontheweb/Termina/issues/102)

- **Fix async test method not awaiting** ([#109](https://github.com/Aaronontheweb/Termina/pull/109))
  - Corrected test method signature to properly await async operations
  - Improves test reliability and prevents potential race conditions

---

#### 0.4.0 December 17th 2025 ####

**Breaking Changes**:
- **VerticalLayout and HorizontalLayout now default to Auto() sizing** ([#97](https://github.com/Aaronontheweb/Termina/pull/97))
  - Previously, these layouts defaulted to Fill(), which caused nested layouts to compete with siblings for space
  - New Auto() default means nested layouts size to their content by default
  - Fill() is now only needed for children that should claim remaining space within a container
  - Root layouts always fill terminal bounds regardless of constraints
  - StackLayout retains Fill() as its default since overlays should fill
  - Fixes [#95](https://github.com/Aaronontheweb/Termina/issues/95)

**New Features**:
- **Rich content support for SelectionListNode** ([#96](https://github.com/Aaronontheweb/Termina/pull/96))
  - New `SelectionItemContent` class for multi-line, styled selection items
  - Support for `ITextSegment` (static and animated) in selection item content
  - "Other" option with inline text input for custom user responses
  - Multi-line items with per-line styling and animated segments (spinners, etc.)
  - Proper coordinate translation via sub-context rendering

- **Platform-native console abstraction for event-driven input** ([#90](https://github.com/Aaronontheweb/Termina/pull/90))
  - Replaced polling-based input with platform-native event-driven input on Windows
  - Windows console enables VT100/ANSI support via `ENABLE_VIRTUAL_TERMINAL_PROCESSING`
  - Event-driven input via `ReadConsoleInputW` eliminates polling latency
  - Window resize events via `WINDOW_BUFFER_SIZE_EVENT`
  - ~1000x rendering performance improvement on Windows (22-60ms → 0.02-0.08ms per frame)
  - Cached console dimensions to avoid repeated P/Invoke calls
  - Fallback console for non-Windows platforms with 1ms polling delay
  - Closes [#78](https://github.com/Aaronontheweb/Termina/issues/78), [#79](https://github.com/Aaronontheweb/Termina/issues/79), [#81](https://github.com/Aaronontheweb/Termina/issues/81)

- **Formalized diagnostic tracing system** ([#84](https://github.com/Aaronontheweb/Termina/pull/84))
  - Lightweight, zero-cost-when-disabled tracing system for debugging TUI applications
  - Custom abstraction over Microsoft.Extensions.Logging (no direct dependency required)
  - Deferred string formatting via `TraceEvent` struct with template and args
  - Lock-free file output using Channel with single reader pattern
  - Category-based filtering (Focus, Layout, Input, Page, Render, etc.)
  - Level-based filtering (Trace, Debug, Info, Warning, Error)
  - Optional MEL integration via `LoggerTraceListener` adapter
  - DI extension methods for easy configuration
  - Trace logs written to %TEMP%/termina-logs/ with timestamped filenames
  - Addresses [#72](https://github.com/Aaronontheweb/Termina/issues/72)

---

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
