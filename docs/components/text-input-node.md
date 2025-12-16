# TextInputNode

A single-line text input with cursor, selection, and keyboard handling.

## Basic Usage

```csharp
var input = new TextInputNode()
    .WithPlaceholder("Enter text...");

// Handle submission
input.Submitted += text => Console.WriteLine($"Submitted: {text}");
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

TextInputNode exposes events and the `HandleInput` method for integration with your ViewModel:

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    public TextInputNode PromptInput { get; } = new TextInputNode()
        .WithPlaceholder("Enter command...");

    public override void OnActivated()
    {
        // Handle keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(key => PromptInput.HandleInput(key.KeyInfo))
            .DisposeWith(Subscriptions);

        // Handle submission
        PromptInput.Submitted += OnSubmitted;
    }

    private void OnSubmitted(string text)
    {
        // Process submitted text
        PromptInput.Clear();
    }
}
```

## Events

| Event | Type | Description |
|-------|------|-------------|
| `TextChanged` | `Action<string>` | Fired when text changes |
| `Submitted` | `Action<string>` | Fired when Enter is pressed |
| `Invalidated` | `Action` | Fired when redraw is needed |

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
| `Clear()` | Clear text and reset cursor |
| `Start()` | Start cursor animation |
| `Stop()` | Stop cursor animation |

## Source Code

::: details View TextInputNode implementation
<<< @/../src/Termina/Layout/TextInputNode.cs{csharp}
:::
