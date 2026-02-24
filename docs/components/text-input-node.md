# TextInputNode

A single-line text input with cursor, selection, and keyboard handling.

## Basic Usage

```csharp
var input = new TextInputNode()
    .WithPlaceholder("Enter text...");

// Handle submission
input.Submitted.Subscribe(text => Console.WriteLine($"Submitted: {text}"));
```

## Features

- Blinking cursor
- Text selection (Shift+Arrow keys)
- Word-by-word navigation (Ctrl+Arrow)
- Password masking
- Max length validation
- Placeholder text

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `←/→` | Move cursor |
| `Ctrl+←/→` | Move by word |
| `Shift+←/→` | Select text |
| `Home/End` | Jump to start/end |
| `Backspace` | Delete before cursor |
| `Ctrl+Backspace` | Delete word before |
| `Delete` | Delete after cursor |
| `Ctrl+A` | Select all |
| `Enter` | Submit |
| `Escape` | Clear text |

## Password Mode

```csharp
new TextInputNode()
    .AsPassword()        // Use default mask '•'

new TextInputNode()
    .AsPassword('*')     // Use custom mask character
```

## Styling

```csharp
new TextInputNode()
    .WithForeground(Color.White)
    .WithBackground(Color.Blue)
    .WithPlaceholder("Type here...")
```

### Colors

| Property | Default | Description |
|----------|---------|-------------|
| `Foreground` | terminal default | Text color |
| `Background` | terminal default | Background color |
| `PlaceholderColor` | `BrightBlack` | Placeholder text color |
| `CursorColor` | `White` | Cursor background color |
| `SelectionColor` | `Blue` | Selection background color |

## Handling Input

TextInputNode is a layout node that should be **owned by the Page**. There are two patterns for input handling:

### Pattern 1: Inside a Modal (Recommended)

When TextInputNode is inside a Modal, Focus automatically routes input:

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    private TextInputNode _textInput = null!;
    private ModalNode _modal = null!;

    protected override void OnBound()
    {
        _textInput = new TextInputNode().WithPlaceholder("Enter command...");
        _modal = Layouts.Modal().WithContent(_textInput);

        _textInput.Submitted
            .Subscribe(text => ViewModel.OnTextSubmitted(text))
            .DisposeWith(Subscriptions);

        // When showing modal, Focus handles input routing automatically
        ViewModel.IsShowingModalChanged
            .Where(show => show)
            .Subscribe(_ => Focus.PushFocus(_modal))
            .DisposeWith(Subscriptions);
    }
}
```

### Pattern 2: Always-Visible Input

For always-visible text inputs, route input via `ViewModel.Input`:

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    private TextInputNode _promptInput = null!;

    protected override void OnBound()
    {
        _promptInput = new TextInputNode().WithPlaceholder("Enter command...");

        // Route input from ViewModel to the text input
        ViewModel.Input.OfType<KeyPressed>()
            .Subscribe(key => _promptInput.HandleInput(key.KeyInfo))
            .DisposeWith(Subscriptions);

        // Handle submission
        _promptInput.Submitted
            .Subscribe(text => {
                ViewModel.OnTextSubmitted(text);
                _promptInput.Clear();
            })
            .DisposeWith(Subscriptions);
    }
}
```

## Paste Handling

`TextInputNode` implements `IPasteReceiver` and automatically handles bracketed paste mode. When the user pastes text from the clipboard, Termina detects the terminal's paste escape sequences and delivers the content as a single `PasteEvent` rather than individual key presses.

### How It Works

1. The user pastes text (Ctrl+V or right-click paste)
2. The terminal wraps the content in `ESC[200~...ESC[201~` markers
3. Termina detects the markers and emits a `PasteEvent`
4. `TextInputNode` shows a summary: `[Pasted 500 lines, 12345 chars]`
5. On **Enter**, the full paste content (with newlines preserved) is submitted
6. On any **editing action** (typing, backspace, delete, escape), the paste is cleared

This prevents multi-line pastes from triggering individual submissions for each line — a common issue in terminal applications.

```csharp
// Paste handling is automatic — no additional setup needed
var input = new TextInputNode()
    .WithPlaceholder("Paste or type here...");

// Submitted receives the full paste content when Enter is pressed
input.Submitted.Subscribe(text =>
{
    // 'text' contains the full paste with newlines preserved
    Console.WriteLine($"Received {text.Length} chars");
});
```

### Paste in ViewModels

If you need to handle paste events at the ViewModel level (e.g., when no `TextInputNode` has focus):

```csharp
Input.OfType<PasteEvent>()
    .Subscribe(paste =>
    {
        // paste.Content contains the full pasted text
        ProcessPastedContent(paste.Content);
    })
    .DisposeWith(Subscriptions);
```

## Observables

| Observable | Type | Description |
|------------|------|-------------|
| `TextChanged` | `IObservable<string>` | Emits when text changes |
| `Submitted` | `IObservable<string>` | Emits when Enter is pressed |
| `Invalidated` | `IObservable<Unit>` | Emits when redraw is needed |

## API Reference

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | `""` | Current text value |
| `Placeholder` | `string?` | `null` | Placeholder text |
| `MaxLength` | `int` | `0` | Max length (0 = unlimited) |
| `IsPassword` | `bool` | `false` | Password mode |
| `PasswordChar` | `char` | `'•'` | Mask character |
| `HasSelection` | `bool` | - | Has selected text |
| `SelectedText` | `string` | - | Currently selected text |

### Methods

| Method | Description |
|--------|-------------|
| `HandleInput(ConsoleKeyInfo)` | Process a key press |
| `HandlePaste(PasteEvent)` | Handle bracketed paste (implements `IPasteReceiver`) |
| `Clear()` | Clear text and reset cursor |
| `Start()` | Start cursor animation |
| `Stop()` | Stop cursor animation |

## Source Code

::: details View TextInputNode implementation
<<< @/../src/Termina/Layout/TextInputNode.cs{csharp}
:::
