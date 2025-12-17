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

## Text Composition and Tracked Segments

StreamingTextNode supports **tracked segments** - text elements that can be independently animated and later removed or replaced. This enables inline animations like spinners, progress bars, timers, and other dynamic content that appears alongside static text.

### Concepts

**Untracked vs Tracked Content:**
- **Untracked** (default): Regular `Append()` and `AppendLine()` calls create immutable content with zero overhead
- **Tracked**: `AppendTracked()` creates mutable segments that can be animated, removed, or replaced

**Segment Types:**

| Type | Interface | Purpose |
|------|-----------|---------|
| `StaticTextSegment` | `ITextSegment` | Static trackable text that can be removed/replaced |
| `SpinnerSegment` | `IAnimatedTextSegment` | Animated spinner with multiple styles |
| Custom implementations | `ITextSegment` or `IAnimatedTextSegment` | Your own tracked segments |

### Caller-Provided IDs

Like HTML elements with `id` attributes, tracked segments use caller-provided IDs. You choose the ID upfront:

```csharp
// Define your segment IDs
private static readonly SegmentId SpinnerId = new(1);
private static readonly SegmentId TimerId = new(2);
private static readonly SegmentId ProgressId = new(3);

// Use them for tracking
stream.AppendTracked(SpinnerId, new SpinnerSegment());
```

This eliminates the need to capture and track returned IDs - you control the identifiers.

### Static Tracked Segments

Use `StaticTextSegment` when you need to track non-animated text for later removal or replacement:

```csharp
var stream = StreamingTextNode.Create();

// Append tracked static text
var placeholder = new StaticTextSegment("[loading...]", Color.Gray);
stream.AppendTracked(new SegmentId(1), placeholder);

// Later, replace with actual content
stream.Replace(
    new SegmentId(1),
    new StaticTextSegment("Operation complete!"),
    keepTracked: false  // Convert to untracked
);
```

### Animated Segments

`SpinnerSegment` provides inline animated spinners with 6 styles:

```csharp
using Termina.Components.Streaming;

var stream = StreamingTextNode.Create();

// Append "Thinking: " followed by animated spinner
stream.Append("Thinking: ");
var spinner = new SpinnerSegment(
    SpinnerStyle.Dots,  // ⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏
    Color.Yellow,
    intervalMs: 80
);
stream.AppendTracked(new SegmentId(1), spinner);
```

**Available Spinner Styles:**

| Style | Animation Frames |
|-------|-----------------|
| `SpinnerStyle.Dots` | ⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏ |
| `SpinnerStyle.Line` | - \\ \| / |
| `SpinnerStyle.Arrow` | ← ↖ ↑ ↗ → ↘ ↓ ↙ |
| `SpinnerStyle.Bounce` | ⠁ ⠂ ⠄ ⠂ |
| `SpinnerStyle.Box` | ▖ ▘ ▝ ▗ |
| `SpinnerStyle.Circle` | ◐ ◓ ◑ ◒ |

### Operations on Tracked Segments

```csharp
var stream = StreamingTextNode.Create();
var spinnerId = new SegmentId(1);

// 1. Append a tracked segment
var spinner = new SpinnerSegment(SpinnerStyle.Dots, Color.Red);
stream.AppendTracked(spinnerId, spinner);

// 2. Remove the tracked segment
stream.Remove(spinnerId);

// 3. Replace with another tracked segment (stays tracked)
stream.Replace(spinnerId, new SpinnerSegment(SpinnerStyle.Arrow), keepTracked: true);

// 4. Replace with untracked content (stops tracking, frees ID)
stream.Replace(
    spinnerId,
    new StaticTextSegment("Done!"),
    keepTracked: false
);
```

### ID Reuse

When `keepTracked: false`, the segment ID is freed and can be reused:

```csharp
private static readonly SegmentId ThinkingSpinnerId = new(1);

// First use
stream.AppendTracked(ThinkingSpinnerId, new SpinnerSegment(...));
// ... later ...
stream.Replace(ThinkingSpinnerId, new StaticTextSegment(""), keepTracked: false);

// ID is now free, can reuse for next operation
stream.AppendTracked(ThinkingSpinnerId, new SpinnerSegment(...));
```

### Real-World Example: Chat with Thinking Indicator

A complete example showing a spinner that appears while waiting for an LLM response:

```csharp
public class ChatViewModel : ReactiveViewModel
{
    private static readonly SegmentId ThinkingSpinnerId = new(1);
    private SpinnerSegment? _currentSpinner;

    public void HandleUserMessage(string message)
    {
        // Show user message
        ChatHistory.Append("You: ", Color.Cyan, decoration: TextDecoration.Bold);
        ChatHistory.AppendLine(message);
        ChatHistory.AppendLine("");

        // Show "Assistant: " with inline spinner
        ChatHistory.Append("Assistant: ", Color.Green, decoration: TextDecoration.Bold);
        _currentSpinner = new SpinnerSegment(SpinnerStyle.Dots, Color.Red);
        ChatHistory.AppendTracked(ThinkingSpinnerId, _currentSpinner);

        // Start async response
        _ = StreamResponseAsync();
    }

    private async Task StreamResponseAsync()
    {
        var firstChunk = true;

        await foreach (var token in GetLlmResponse())
        {
            // On first text, replace spinner with empty content
            if (firstChunk)
            {
                ChatHistory.Replace(
                    ThinkingSpinnerId,
                    new StaticTextSegment(""),
                    keepTracked: false
                );
                _currentSpinner?.Dispose();
                _currentSpinner = null;
                firstChunk = false;
            }

            // Append LLM text
            ChatHistory.Append(token);
        }

        ChatHistory.AppendLine("");
        ChatHistory.AppendLine("");
    }
}
```

### Custom Animated Segments

Implement `IAnimatedTextSegment` to create custom animations:

```csharp
public class BlinkSegment : IAnimatedTextSegment
{
    private readonly Timer _timer;
    private readonly Subject<Unit> _invalidated = new();
    private bool _visible = true;
    private readonly string _text;
    private readonly TextStyle _style;

    public BlinkSegment(string text, Color color, int intervalMs = 500)
    {
        _text = text;
        _style = new TextStyle(color, Color.Default, TextDecoration.None);
        _timer = new Timer(intervalMs);
        _timer.Elapsed += (_, _) =>
        {
            _visible = !_visible;
            _invalidated.OnNext(Unit.Default);
        };
        Start();
    }

    public IObservable<Unit> Invalidated => _invalidated;
    public bool IsAnimating => _timer.Enabled;

    public StyledSegment GetCurrentSegment()
    {
        return _visible
            ? new StyledSegment(_text, _style)
            : new StyledSegment(new string(' ', _text.Length), _style);
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
    }
}

// Usage
var blink = new BlinkSegment("URGENT", Color.Red);
stream.AppendTracked(new SegmentId(1), blink);
```

### Performance Considerations

- **Untracked appends**: O(1), zero overhead
- **Tracked appends**: O(1), minimal tracking overhead
- **Remove/Replace**: O(n) buffer rebuild, but only when mutating
- **Animation frames**: No rebuild, just invalidation for redraw

Most content should be untracked. Only use tracked segments for dynamic elements that need mutation.

### Use Cases

- **Spinners**: Loading indicators while waiting for async operations
- **Timers**: Countdown or elapsed time displays
- **Progress bars**: Text-based progress indicators
- **Blinking text**: Attention-grabbing alerts
- **Status indicators**: Live updating connection status, typing indicators
- **Placeholders**: Temporary content replaced when data loads

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

#### Untracked Content

| Method | Description |
|--------|-------------|
| `.Append(string)` | Append plain text |
| `.Append(string, foreground?, background?, decoration?)` | Append styled text |
| `.Append(StyledSegment)` | Append a styled segment |
| `.AppendLine(string)` | Append plain text line |
| `.AppendLine(string, foreground?, background?, decoration?)` | Append styled line |
| `.Clear()` | Clear all content (tracked and untracked) |

#### Tracked Segments

| Method | Description |
|--------|-------------|
| `.AppendTracked(SegmentId, ITextSegment)` | Append tracked segment with caller-provided ID |
| `.Remove(SegmentId)` | Remove tracked segment by ID |
| `.Replace(SegmentId, ITextSegment, keepTracked)` | Replace segment; `keepTracked: false` converts to untracked |

#### Scrolling

| Method | Description |
|--------|-------------|
| `.ScrollUp(lines, width)` | Scroll up |
| `.ScrollDown(lines)` | Scroll down |
| `.ScrollToBottom()` | Jump to bottom |
| `.HandleInput(key, height, width)` | Handle scroll keys |

#### Configuration

| Method | Description |
|--------|-------------|
| `.WithForeground(Color)` | Set default text color |
| `.WithBackground(Color)` | Set default background color |
| `.WithPrefix(string, Color?)` | Set line prefix |

## Source Code

::: details View StreamingTextNode implementation
<<< @/../src/Termina/Layout/StreamingTextNode.cs{csharp}
:::
