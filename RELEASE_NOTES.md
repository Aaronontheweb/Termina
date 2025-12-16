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
