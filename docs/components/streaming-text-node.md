# StreamingTextNode

Displays streaming text content with automatic scrolling and word wrapping. Ideal for chat interfaces, logs, and LLM output.

## Basic Usage

```csharp
// Create with persisted buffer (retains all content)
var stream = StreamingTextNode.Create();

// Append content
stream.AppendLine("User: Hello!");
stream.AppendLine("Bot: Hi there!");

// Append streaming content (character by character)
stream.Append("Thinking");
stream.Append(".");
stream.Append(".");
stream.Append(".");
```

## Buffer Types

### Persisted Buffer (Default)

Retains all content with scrolling support:

```csharp
var stream = StreamingTextNode.Create();

// Supports scrolling
stream.ScrollUp(5);
stream.ScrollDown(5);
stream.ScrollToBottom();
```

### Windowed Buffer

Rolling buffer that retains only recent lines:

```csharp
// Keep last 100 lines
var stream = StreamingTextNode.CreateWindowed(windowSize: 100);
```

## Line Prefixes

Add prefixes to each line (useful for chat):

```csharp
var userStream = StreamingTextNode.Create()
    .WithPrefix("You: ", Color.Cyan);

var botStream = StreamingTextNode.Create()
    .WithPrefix("Bot: ", Color.Green);
```

## Styling

### Node-Level Styling

Set default colors for the entire node:

```csharp
StreamingTextNode.Create()
    .WithForeground(Color.White)
    .WithBackground(Color.Black)
    .WithPrefix("> ", Color.Gray)
```

### Inline Styled Text

Append text with inline colors and text decorations:

```csharp
var stream = StreamingTextNode.Create();

// Colored text
stream.Append("Error: ", foreground: Color.Red);
stream.AppendLine("Something went wrong", foreground: Color.Yellow);

// With background
stream.Append("Highlighted", foreground: Color.Black, background: Color.Yellow);

// With decorations
stream.Append("Bold text", decoration: TextDecoration.Bold);
stream.Append("Italic text", decoration: TextDecoration.Italic);

// Combined decorations
stream.Append("Bold and underlined",
    foreground: Color.Cyan,
    decoration: TextDecoration.Bold | TextDecoration.Underline);
```

### Text Decorations

The following decorations are available:

| Decoration | Description |
|------------|-------------|
| `TextDecoration.None` | No decoration (default) |
| `TextDecoration.Bold` | Bold/bright text |
| `TextDecoration.Dim` | Dimmed text |
| `TextDecoration.Italic` | Italic text |
| `TextDecoration.Underline` | Underlined text |
| `TextDecoration.Strikethrough` | Strikethrough text |

Decorations can be combined using bitwise OR:

```csharp
var style = TextDecoration.Bold | TextDecoration.Italic | TextDecoration.Underline;
stream.Append("All styles", decoration: style);
```

### Using StyledSegment

For more control, use `StyledSegment` directly:

```csharp
var style = new TextStyle(
    foreground: Color.Green,
    background: Color.Default,
    decoration: TextDecoration.Bold
);

stream.Append(new StyledSegment("Success!", style));
```

### Style Precedence

Inline styles override node-level defaults:

```csharp
var stream = StreamingTextNode.Create()
    .WithForeground(Color.White);  // Default white

stream.AppendLine("This is white");  // Uses default
stream.AppendLine("This is red", foreground: Color.Red);  // Override
stream.AppendLine("Back to white");  // Uses default again
```

### Real-World Chat Example

Building a rich chat interface with styled messages:

```csharp
var chat = StreamingTextNode.Create();

// Tool invocation
chat.Append("○ ", foreground: Color.Yellow);
chat.Append("search_web", foreground: Color.Cyan);
chat.AppendLine("(\"latest news\")", foreground: Color.BrightBlack);

// Error message
chat.Append("Error: ", foreground: Color.Red, decoration: TextDecoration.Bold);
chat.AppendLine("Network timeout", foreground: Color.Red);

// Success message
chat.Append("✓ ", foreground: Color.Green);
chat.AppendLine("Task completed", foreground: Color.Green);

// Streaming LLM response with mixed styles
chat.Append("Assistant: ", foreground: Color.Blue, decoration: TextDecoration.Bold);
await foreach (var token in llmResponse)
{
    chat.Append(token);  // Plain text for LLM output
}
chat.AppendLine("");
```

::: tip Why Structured API Instead of Markup?
Termina uses a structured API (not markup like `[red]text[/red]`) because:
- **Safe for LLM output** - No need to escape user content or AI responses
- **Type-safe** - IDE autocompletion and compile-time checking
- **Composable** - Build styled segments programmatically
:::

### Style Preservation in Word Wrap

Styles are automatically preserved when text wraps to multiple lines:

```csharp
// Long styled text that wraps
stream.Append(
    "This is a long red message that will automatically wrap to multiple lines while preserving the red color across all wrapped lines.",
    foreground: Color.Red
);
```

## Scrolling API

For persisted buffers:

```csharp
var stream = StreamingTextNode.Create();

// Manual scrolling
stream.ScrollUp(lines: 5, viewportWidth: 80);
stream.ScrollDown(lines: 5);
stream.ScrollToBottom();

// Handle keyboard scrolling
stream.HandleInput(keyInfo, viewportHeight: 20, viewportWidth: 80);
```

### Scroll Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `PageUp` | Scroll up one page |
| `PageDown` | Scroll down one page |
| `Ctrl+Home` | Scroll to top |
| `Ctrl+End` | Scroll to bottom |

## Real-World Example

From the streaming chat demo:

```csharp
public partial class ChatViewModel : ReactiveViewModel
{
    public StreamingTextNode ChatHistory { get; }

    public ChatViewModel()
    {
        ChatHistory = StreamingTextNode.Create();
    }

    private async Task HandleResponse(IAsyncEnumerable<string> tokens)
    {
        ChatHistory.AppendLine("");
        ChatHistory.Append("Bot: ");

        await foreach (var token in tokens)
        {
            ChatHistory.Append(token);
        }

        ChatHistory.AppendLine("");
    }
}
```

## API Reference

### Factory Methods

```csharp
// Persisted buffer (all content retained)
StreamingTextNode.Create()

// Windowed buffer (rolling window)
StreamingTextNode.CreateWindowed(windowSize: 100)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Buffer` | `IStreamingTextBuffer` | Underlying buffer |
| `Foreground` | `Color?` | Text color |
| `Background` | `Color?` | Background color |
| `Prefix` | `string?` | Line prefix |
| `PrefixColor` | `Color?` | Prefix color |
| `ContentChanged` | `IObservable<Unit>` | Content change notifications |

### Methods

| Method | Description |
|--------|-------------|
| `.Append(string)` | Append plain text |
| `.Append(string, foreground?, background?, decoration?)` | Append styled text |
| `.Append(StyledSegment)` | Append a styled segment |
| `.AppendLine(string)` | Append plain text line |
| `.AppendLine(string, foreground?, background?, decoration?)` | Append styled line |
| `.Clear()` | Clear all content |
| `.ScrollUp(lines, width)` | Scroll up |
| `.ScrollDown(lines)` | Scroll down |
| `.ScrollToBottom()` | Jump to bottom |
| `.HandleInput(key, height, width)` | Handle scroll keys |
| `.WithForeground(Color)` | Set default text color |
| `.WithBackground(Color)` | Set default background color |
| `.WithPrefix(string, Color?)` | Set line prefix |

## Source Code

::: details View StreamingTextNode implementation
<<< @/../src/Termina/Layout/StreamingTextNode.cs{csharp}
:::
