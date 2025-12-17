# Input Handling

Termina provides an observable stream of input events for keyboard, mouse, and terminal resize handling.

## Input Ownership

The `Input` observable is owned by the **ViewModel** and is public, allowing both ViewModels and Pages to subscribe:

- **ViewModels** handle input to update state and business logic (most common)
- **Pages** can access `ViewModel.Input` to route input to interactive layout nodes (TextInputNode, scrollable content)

```csharp
// In ViewModel - handle input for state changes
public override void OnActivated()
{
    Input.OfType<KeyPressed>()
        .Subscribe(HandleKey)
        .DisposeWith(Subscriptions);
}

// In Page - route input to interactive layout nodes
protected override void OnBound()
{
    ViewModel.Input.OfType<KeyPressed>()
        .Subscribe(key => _textInput.HandleInput(key.KeyInfo))
        .DisposeWith(Subscriptions);
}
```

## Input Events

All input events implement `IInputEvent`. The framework provides:

- `KeyPressed` - Keyboard input
- `MouseEvent` - Mouse clicks, movement, scrolling
- `ResizeEvent` - Terminal window resize

## Keyboard Input

### Basic Key Handling

```csharp
public override void OnActivated()
{
    Input.OfType<KeyPressed>()
        .Subscribe(HandleKey)
        .DisposeWith(Subscriptions);
}

private void HandleKey(KeyPressed key)
{
    switch (key.KeyInfo.Key)
    {
        case ConsoleKey.UpArrow:
            Count++;
            break;
        case ConsoleKey.DownArrow:
            Count--;
            break;
        case ConsoleKey.Escape:
            Shutdown();
            break;
    }
}
```

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

    // Check for Shift+Tab
    if (info.Key == ConsoleKey.Tab &&
        (info.Modifiers & ConsoleModifiers.Shift) != 0)
    {
        MoveToPrevious();
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
    // Handle click at (mouse.X, mouse.Y)
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

## Source Code

::: details View KeyPressed
<<< @/../src/Termina/Input/KeyPressed.cs{csharp}
:::

::: details View MouseEvent
<<< @/../src/Termina/Input/MouseEvent.cs{csharp}
:::

::: details View ResizeEvent
<<< @/../src/Termina/Input/ResizeEvent.cs{csharp}
:::
