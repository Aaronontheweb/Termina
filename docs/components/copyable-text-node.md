# CopyableTextNode

`CopyableTextNode` is a focusable, read-only text component for values users need to copy out of a TUI: OAuth URLs, tokens, connection strings, and diagnostic output.

It supports keyboard selection, configurable copy shortcuts, and copy feedback via toast notifications, inline indicators, or both.

## Basic Usage

```csharp
public sealed class AuthPage : ReactivePage<AuthViewModel>
{
    private readonly IClipboardService _clipboard;
    private readonly IToastService _toasts;

    public AuthPage(IClipboardService clipboard, IToastService toasts)
    {
        _clipboard = clipboard;
        _toasts = toasts;
    }

    protected override ILayoutNode BuildLayout()
    {
        return new CopyableTextNode(
                _clipboard,
                ViewModel.DeviceCodeUrl,
                _toasts)
            .WithHint("Press Enter to copy this URL");
    }
}
```

## Keyboard Behavior

| Key | Action |
|-----|--------|
| `Left/Right` | Move the caret |
| `Shift+Left/Right` | Extend selection |
| `Home/End` | Jump to start or end |
| `Ctrl+A` | Select all |
| `Enter` | Copy selected text, or the full value when nothing is selected |
| `Ctrl+C` | Copy selected text, or the full value when nothing is selected |
| `Escape` | Clear the current selection |

## Custom Copy Bindings

You can override the default `Enter` and `Ctrl+C` bindings per node.

```csharp
new CopyableTextNode(_clipboard, ViewModel.Token, _toasts)
    .WithCopyBindings(
        new CopyKeyBinding(ConsoleKey.F5),
        new CopyKeyBinding(ConsoleKey.Enter));
```

## Copy Feedback Modes

`CopyableTextNode` separates the copy action from how success is shown.

```csharp
new CopyableTextNode(_clipboard, ViewModel.Token, _toasts)
    .WithFeedbackMode(CopyFeedbackMode.ToastAndInline)
    .WithToastPosition(ToastPosition.TopRight)
    .WithInlineIndicator("✓ Copied", Color.BrightGreen)
    .WithFeedbackDuration(TimeSpan.FromSeconds(1.5));
```

Available feedback modes:

- `CopyFeedbackMode.None`
- `CopyFeedbackMode.Toast`
- `CopyFeedbackMode.InlineIndicator`
- `CopyFeedbackMode.ToastAndInline`

## Toast Placement

Toasts are rendered globally by `TerminaApplication`, but placement is configurable per toast request.

Supported positions:

- `ToastPosition.BottomRight`
- `ToastPosition.BottomCenter`
- `ToastPosition.BottomLeft`
- `ToastPosition.TopRight`
- `ToastPosition.TopCenter`
- `ToastPosition.TopLeft`

## Transport Behavior

Clipboard writes go through `IClipboardService`, which can use multiple transports.

- `OSC 52` terminal clipboard transport for terminal-native copy
- `tmux` clipboard transport when running inside tmux

`CopyableTextNode` does not know or care which transport succeeded. It only reacts to the boolean success returned by the clipboard service.

## When to Use It

Use `CopyableTextNode` when:

- the content is read-only
- users need to copy all or part of a value
- you want focusable keyboard selection without exposing a text editor

Use `TextInputNode` or `TextAreaNode` when the content should be editable.
