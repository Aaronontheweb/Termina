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

```csharp
StreamingTextNode.Create()
    .WithForeground(Color.White)
    .WithBackground(Color.Black)
    .WithPrefix("> ", Color.Gray)
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
| `.Append(string)` | Append text (streaming) |
| `.AppendLine(string)` | Append line |
| `.Clear()` | Clear all content |
| `.ScrollUp(lines, width)` | Scroll up |
| `.ScrollDown(lines)` | Scroll down |
| `.ScrollToBottom()` | Jump to bottom |
| `.HandleInput(key, height, width)` | Handle scroll keys |
| `.WithForeground(Color)` | Set text color |
| `.WithPrefix(string, Color?)` | Set line prefix |

## Source Code

::: details View StreamingTextNode implementation
<<< @/../src/Termina/Layout/StreamingTextNode.cs{csharp}
:::
