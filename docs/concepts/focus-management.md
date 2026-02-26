# Focus Management

Termina's focus management system routes keyboard input to interactive components using a stack-based model. It supports auto-focus on navigation, Tab cycling, and nested modals.

## Core Concepts

### IFocusable

Any component that can receive keyboard focus implements `IFocusable`:

```csharp
public interface IFocusable : ILayoutNode
{
    bool CanFocus { get; }           // Can this receive focus?
    bool HasFocus { get; }           // Currently has focus?
    int FocusPriority { get; }       // Higher = higher priority
    void OnFocused();                // Called when focused
    void OnBlurred();                // Called when unfocused
    bool HandleInput(ConsoleKeyInfo key); // Handle keyboard input
}
```

Built-in focusable components: `SelectionListNode<T>`, `TextInputNode`, `ModalNode`, `GridNode`, `WizardNode`.

### FocusManager

The `FocusManager` maintains a focus stack:

```csharp
Focus.PushFocus(component);  // Push onto stack (for modals)
Focus.PopFocus();             // Pop from stack (close modal)
Focus.SetFocus(component);    // Replace top of stack
Focus.ClearFocus();           // Clear all focus
Focus.RouteInput(key);        // Route key to current focus
```

### FocusPolicy

Set `FocusPolicy` in your page to automatically focus a component on navigation:

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    public MyPage()
    {
        FocusPolicy = FocusPolicy.FirstFocusable; // Auto-focus first focusable
    }
}
```

| Policy | Behavior |
|--------|----------|
| `Manual` (default) | No auto-focus. Call `Focus.PushFocus()` manually. |
| `FirstFocusable` | Focus the first `IFocusable` found via depth-first tree walk. |
| `ByPriority` | Focus the `IFocusable` with the highest `FocusPriority`. |

## Tab Cycling

Opt into Tab/Shift+Tab cycling in your page:

```csharp
public override void OnNavigatedTo()
{
    base.OnNavigatedTo();
    KeyBindings.Register(ConsoleKey.Tab, CycleFocusForward);
    KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, CycleFocusBackward);
}
```

The cycle helpers walk the layout tree, collect all focusable nodes, and move focus to the next/previous with wrap-around.

## Before / After

**Before** (30+ lines of boilerplate):

```csharp
public override void OnNavigatedTo()
{
    base.OnNavigatedTo();
    // Manually find the first focusable component
    var selectionList = FindSelectionList(); // custom helper
    if (selectionList != null)
        Focus.PushFocus(selectionList);
}
```

**After** (2 lines):

```csharp
public MyPage()
{
    FocusPolicy = FocusPolicy.FirstFocusable;
}
```

## Focus Priority

Use `FocusPriority` to establish an input hierarchy:

| Range | Use |
|-------|-----|
| 100+ | Modals and overlays (capture all input) |
| 10-99 | Interactive controls (text inputs, lists) |
| 1-9 | Background handlers |

## PushFocus / PopFocus for Modals

```csharp
// Show modal
Focus.PushFocus(modal);  // Captures all input

// Close modal — focus returns to previous component
Focus.PopFocus();
```

## Tree Walk

`CollectFocusables` performs a depth-first walk of the layout tree, collecting all nodes that implement `IFocusable` where `CanFocus` is true. This traverses into `VerticalLayout`, `HorizontalLayout`, `ReactiveLayoutNode`, `DynamicLayoutNode`, and other container types.

```csharp
var focusables = Focus.CollectFocusables(layoutRoot);
// focusables[0] is the first focusable in document order
```
