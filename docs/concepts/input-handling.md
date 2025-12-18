# Input Handling

Termina provides a structured input routing system with two phases: **capture** (page-level) and **bubble** (component-level), followed by the ViewModel's input observable for any remaining events.

## Input Flow Overview

```
Keyboard Input
      │
      ▼
┌─────────────────────────────────────────┐
│  CAPTURE PHASE (Page)                   │
│  Page.KeyBindings intercepts first      │
│  Good for: Escape, Tab, global hotkeys  │
└─────────────────────────────────────────┘
      │ (if not consumed)
      ▼
┌─────────────────────────────────────────┐
│  BUBBLE PHASE (Components)              │
│  FocusManager routes to focused node    │
│  Good for: Text input, list navigation  │
└─────────────────────────────────────────┘
      │ (if not consumed)
      ▼
┌─────────────────────────────────────────┐
│  VIEWMODEL (ViewModel.Input)            │
│  Observable receives unconsumed events  │
│  Good for: State changes, fallback      │
└─────────────────────────────────────────┘
```

This model is similar to DOM event handling where events are captured from the top down, then bubble up from the target.

## Page-Level Key Bindings (Capture Phase)

Use `KeyBindings` in your Page to intercept keys **before** focused components receive them. This is essential for navigation keys like Escape that should always work, regardless of what component has focus.

### Basic Usage

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Register page-level key bindings (capture phase)
        // Pages have direct access to Navigate() and Shutdown()
        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));
        KeyBindings.Register(ConsoleKey.Q, () => Shutdown());
    }
}
```

### With Modifiers

```csharp
public override void OnNavigatedTo()
{
    base.OnNavigatedTo();

    // Ctrl+S to save
    KeyBindings.Register(ConsoleKey.S, ConsoleModifiers.Control, () => ViewModel.Save());

    // Shift+Tab for reverse navigation
    KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, () => FocusPrevious());

    // Plain Tab for forward navigation
    KeyBindings.Register(ConsoleKey.Tab, () => FocusNext());
}
```

### Focus Cycling Example

```csharp
public class FormPage : ReactivePage<FormViewModel>
{
    private TextInputNode _nameInput = null!;
    private TextInputNode _emailInput = null!;
    private int _focusIndex;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/"));
        KeyBindings.Register(ConsoleKey.Tab, CycleFocus);

        // Start with first input focused
        _focusIndex = 0;
        Focus.PushFocus(_nameInput);
    }

    private void CycleFocus()
    {
        _focusIndex = (_focusIndex + 1) % 2;
        var target = _focusIndex == 0 ? _nameInput : _emailInput;
        Focus.PushFocus(target);
    }
}
```

### When to Use Page-Level Bindings

| Use Case | Example |
|----------|---------|
| Navigation keys | Escape to go back, Q to quit |
| Global shortcuts | Ctrl+S save, Ctrl+Z undo |
| Focus management | Tab/Shift+Tab between inputs |
| Modal dismissal | Escape to close dialog |
| Menu shortcuts | F1 for help, F5 for refresh |

## Component-Level Input (Bubble Phase)

After the capture phase, input routes to the currently focused component via `FocusManager`. Components implement `IFocusable.HandleInput()` to process keys.

### Built-in Focusable Components

| Component | Handles |
|-----------|---------|
| `TextInputNode` | Character input, backspace, cursor movement |
| `SelectionListNode` | Arrow keys, Enter, number keys 1-9 |
| `ScrollableContainerNode` | Arrow keys for scrolling |

### Focus Management

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    private SelectionListNode<string> _list = null!;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Set initial focus
        Focus.PushFocus(_list);
    }

    private void ShowTextInput()
    {
        // Push new focus (previous is saved)
        Focus.PushFocus(_textInput);
    }

    private void HideTextInput()
    {
        // Pop focus (returns to previous)
        Focus.PopFocus();
    }
}
```

### Custom Focusable Component

```csharp
public class CustomInput : LayoutNode, IFocusable
{
    public bool IsFocused { get; set; }

    public bool HandleInput(ConsoleKeyInfo keyInfo)
    {
        // Return true if you handled (consumed) the key
        // Return false to let it bubble to ViewModel
        if (keyInfo.Key == ConsoleKey.Enter)
        {
            OnSubmit();
            return true;  // Consumed
        }

        return false;  // Not consumed, continue to ViewModel
    }
}
```

## ViewModel Input Observable

Keys not consumed by the capture or bubble phases arrive at the ViewModel's `Input` observable:

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    public override void OnActivated()
    {
        // Handle keys that weren't consumed by Page or focused component
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKey)
            .DisposeWith(Subscriptions);
    }

    private void HandleKey(KeyPressed key)
    {
        // This only receives keys not handled elsewhere
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.F5:
                Refresh();
                break;
        }
    }
}
```

::: warning Migration Note
Prior to v0.5, the ViewModel's `Input` observable received all keys. Now, with page-level key bindings, navigation keys like Escape should be handled in the Page using `KeyBindings.Register()` rather than in the ViewModel.
:::

## Input Events

All input events implement `IInputEvent`. The framework provides:

- `KeyPressed` - Keyboard input
- `MouseEvent` - Mouse clicks, movement, scrolling
- `ResizeEvent` - Terminal window resize

## Keyboard Input Details

### Checking Modifiers

```csharp
private void HandleKey(KeyPressed key)
{
    var info = key.KeyInfo;

    // Check for Ctrl+C
    if (info.Key == ConsoleKey.C &&
        (info.Modifiers & ConsoleModifiers.Control) != 0)
    {
        Shutdown();
    }
}
```

### Character Input

```csharp
private void HandleKey(KeyPressed key)
{
    var ch = key.KeyInfo.KeyChar;

    if (!char.IsControl(ch))
    {
        // Regular character typed
        Text += ch;
    }
}
```

## Mouse Input

### Mouse Event Properties

```csharp
Input.OfType<MouseEvent>()
    .Subscribe(mouse =>
    {
        var x = mouse.X;           // Column position
        var y = mouse.Y;           // Row position
        var button = mouse.Button; // Which button
        var type = mouse.EventType;// Press, Release, etc.
        var mods = mouse.Modifiers;// Shift, Ctrl, Alt
    })
    .DisposeWith(Subscriptions);
```

### Mouse Buttons

| Button | Description |
|--------|-------------|
| `MouseButton.None` | No button |
| `MouseButton.Left` | Left click |
| `MouseButton.Right` | Right click |
| `MouseButton.Middle` | Middle button |
| `MouseButton.WheelUp` | Scroll up |
| `MouseButton.WheelDown` | Scroll down |

### Mouse Event Types

| Type | Description |
|------|-------------|
| `MouseEventType.Press` | Button pressed down |
| `MouseEventType.Release` | Button released |
| `MouseEventType.Drag` | Move while button held |
| `MouseEventType.Move` | Move without button |
| `MouseEventType.Scroll` | Wheel scrolled |

### Click Handling Example

```csharp
Input.OfType<MouseEvent>()
    .Where(m => m.EventType == MouseEventType.Press &&
                m.Button == MouseButton.Left)
    .Subscribe(HandleClick)
    .DisposeWith(Subscriptions);

private void HandleClick(MouseEvent mouse)
{
    if (IsInButtonBounds(mouse.X, mouse.Y))
    {
        OnButtonClicked();
    }
}
```

## Terminal Resize

```csharp
Input.OfType<ResizeEvent>()
    .Subscribe(resize =>
    {
        var width = resize.Width;   // New width in columns
        var height = resize.Height; // New height in rows

        // Layout is automatically recalculated
        // Use this for custom resize logic
    })
    .DisposeWith(Subscriptions);
```

## Input Filtering with Rx

### Filter by Type

```csharp
Input.OfType<KeyPressed>()  // Only keyboard events
Input.OfType<MouseEvent>()  // Only mouse events
```

### Filter by Condition

```csharp
Input.OfType<KeyPressed>()
    .Where(k => k.KeyInfo.Key == ConsoleKey.Enter)
    .Subscribe(HandleEnter);
```

### Throttle Input

```csharp
Input.OfType<KeyPressed>()
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(HandleKey);
```

### Combine Streams

```csharp
var escPressed = Input.OfType<KeyPressed>()
    .Where(k => k.KeyInfo.Key == ConsoleKey.Escape);

var rightClick = Input.OfType<MouseEvent>()
    .Where(m => m.Button == MouseButton.Right);

Observable.Merge(escPressed.Select(_ => Unit.Default),
                 rightClick.Select(_ => Unit.Default))
    .Subscribe(_ => CloseMenu());
```

## Best Practices

### 1. Use Page-Level Bindings for Navigation

```csharp
// ✅ Good - Page intercepts Escape before any component
public override void OnNavigatedTo()
{
    KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/"));
}

// ❌ Avoid - Component might consume Escape first
public override void OnActivated()
{
    Input.OfType<KeyPressed>()
        .Where(k => k.KeyInfo.Key == ConsoleKey.Escape)
        .Subscribe(_ => Navigate("/"));
}
```

### 2. Let Components Handle Their Own Input

```csharp
// ✅ Good - SelectionListNode handles its own navigation
Focus.PushFocus(_selectionList);  // Arrow keys, Enter work automatically

// ❌ Avoid - Manually routing input to components
Input.OfType<KeyPressed>()
    .Subscribe(k => _selectionList.HandleInput(k.KeyInfo));
```

### 3. Use ViewModel.Input for State Changes

```csharp
// ✅ Good - ViewModel handles business logic keys
Input.OfType<KeyPressed>()
    .Where(k => k.KeyInfo.Key == ConsoleKey.F5)
    .Subscribe(_ => RefreshData());
```

## Source Code

::: details View PageKeyBindings
<<< @/../src/Termina/Input/PageKeyBindings.cs{csharp}
:::

::: details View KeyPressed
<<< @/../src/Termina/Input/KeyPressed.cs{csharp}
:::

::: details View MouseEvent
<<< @/../src/Termina/Input/MouseEvent.cs{csharp}
:::

::: details View ResizeEvent
<<< @/../src/Termina/Input/ResizeEvent.cs{csharp}
:::
