# ModalNode

A modal overlay component that displays content over the rest of the UI with configurable backdrop, borders, and positioning.

## Basic Usage

```csharp
var modal = Layouts.Modal()
    .WithTitle("Confirm Action")
    .WithContent(new TextNode("Are you sure?"))
    .WithBorder(BorderStyle.Rounded)
    .WithBackdrop(BackdropStyle.Dim);

// Show modal by pushing focus
Focus.PushFocus(modal);

// Handle dismissal
modal.Dismissed.Subscribe(_ => {
    Focus.PopFocus();
    // Handle cancel...
});
```

## Features

- Captures all keyboard input when focused (high priority = 100)
- Configurable backdrop (transparent, dim, solid)
- Multiple positioning options (center, top, bottom)
- Border styles matching PanelNode
- Forwards input to focusable content
- Escape key dismissal (configurable)

## Backdrop Styles

```csharp
// Semi-transparent dim effect (default)
Layouts.Modal().WithBackdrop(BackdropStyle.Dim)
// ░░░░░░░░░░░░░░░░░
// ░░╭─ Modal ─╮░░░░
// ░░│ Content │░░░░
// ░░╰─────────╯░░░░
// ░░░░░░░░░░░░░░░░░

// Solid background (fully obscures content)
Layouts.Modal()
    .WithBackdrop(BackdropStyle.Solid)
    .WithBackdropColor(Color.DarkGray)

// Transparent (no backdrop)
Layouts.Modal().WithBackdrop(BackdropStyle.Transparent)
```

## Positioning

```csharp
// Centered (default)
Layouts.Modal().WithPosition(ModalPosition.Center)

// Near top of screen
Layouts.Modal().WithPosition(ModalPosition.Top)

// Near bottom of screen
Layouts.Modal().WithPosition(ModalPosition.Bottom)
```

## Styling

```csharp
Layouts.Modal()
    .WithTitle("Settings")
    .WithTitleColor(Color.Cyan)
    .WithBorder(BorderStyle.Double)
    .WithBorderColor(Color.Blue)
    .WithBackdrop(BackdropStyle.Dim)
    .WithBackdropColor(Color.BrightBlack)
    .WithBackdropChar('░')
    .WithPadding(2)
    .WithContent(content)
```

## With Interactive Content

Modals forward keyboard input to focusable content like TextInputNode or SelectionListNode:

```csharp
var textInput = new TextInputNode()
    .WithPlaceholder("Enter name...");

var modal = Layouts.Modal()
    .WithTitle("Enter Your Name")
    .WithContent(textInput);

// Handle text submission
textInput.Submitted.Subscribe(name => {
    Focus.PopFocus();
    ProcessName(name);
});

// Show modal
Focus.PushFocus(modal);
```

## Complete Example

Here's a real-world example from a todo list application:

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    private ModalNode? _confirmModal;
    private TextInputNode? _textInput;

    public ModalNode? ConfirmModal => _confirmModal;

    public override void OnActivated()
    {
        // Create text input
        _textInput = new TextInputNode()
            .WithPlaceholder("Enter task description...");

        _textInput.Submitted
            .Subscribe(text => {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AddTask(text);
                }
                HideModal();
            })
            .DisposeWith(Subscriptions);

        // Create modal
        _confirmModal = Layouts.Modal()
            .WithTitle("Add New Task")
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(Color.Cyan)
            .WithBackdrop(BackdropStyle.Dim)
            .WithPadding(1)
            .WithContent(_textInput)
            .WithDismissOnEscape(true);

        _confirmModal.Dismissed
            .Subscribe(_ => HideModal())
            .DisposeWith(Subscriptions);
    }

    private void ShowModal()
    {
        IsShowingModal = true;
        _textInput?.Clear();
        Focus.PushFocus(_confirmModal!);
    }

    private void HideModal()
    {
        IsShowingModal = false;
        Focus.PopFocus();
    }
}
```

In your page, use `Layouts.Deferred()` to prevent modal disposal when hidden:

```csharp
public override ILayoutNode BuildLayout()
{
    return Layouts.Stack()
        .WithChild(mainContent)
        .WithChild(
            ViewModel.IsShowingModalChanged
                .Select(show => show
                    ? Layouts.Deferred(() => ViewModel.ConfirmModal)
                    : (ILayoutNode)Layouts.Empty())
                .AsLayout());
}
```

## Observables

| Observable | Type | Description |
|------------|------|-------------|
| `Dismissed` | `IObservable<Unit>` | Emits when modal is dismissed (Escape) |
| `Invalidated` | `IObservable<Unit>` | Emits when redraw is needed |

## API Reference

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Content` | `ILayoutNode?` | `null` | Modal content |
| `CanFocus` | `bool` | `true` | Always focusable |
| `HasFocus` | `bool` | - | Whether modal has focus |
| `FocusPriority` | `int` | `100` | High priority to capture input |

### Fluent Methods

| Method | Description |
|--------|-------------|
| `.WithContent(ILayoutNode)` | Set modal content |
| `.WithTitle(string)` | Set title in border |
| `.WithTitleColor(Color)` | Set title color |
| `.WithBorder(BorderStyle)` | Set border style |
| `.WithBorderColor(Color)` | Set border color |
| `.WithBackdrop(BackdropStyle)` | Set backdrop style |
| `.WithBackdropColor(Color)` | Set backdrop color |
| `.WithBackdropChar(char)` | Set backdrop character (Dim style) |
| `.WithPosition(ModalPosition)` | Set vertical position |
| `.WithPadding(int)` | Set content padding |
| `.WithDismissOnEscape(bool)` | Enable/disable Escape dismissal |

### Enums

**BackdropStyle**
| Value | Description |
|-------|-------------|
| `Transparent` | No backdrop - content behind visible |
| `Dim` | Semi-transparent pattern (default) |
| `Solid` | Solid color background |

**ModalPosition**
| Value | Description |
|-------|-------------|
| `Center` | Centered vertically (default) |
| `Top` | Near top of screen |
| `Bottom` | Near bottom of screen |

## Source Code

::: details View ModalNode implementation
<<< @/../src/Termina/Layout/ModalNode.cs{csharp}
:::
