# ScrollableContainerNode

A container that provides vertical scrolling for content that exceeds the viewport.

## Basic Usage

```csharp
new ScrollableContainerNode()
    .WithContent(
        Layouts.Vertical()
            .WithChild(...)  // Many children
            .WithChild(...))
    .Fill()
```

## Auto-Scroll Policies

Control how the container scrolls when content changes:

```csharp
// Scroll to bottom only if already at bottom (default)
.WithAutoScroll(AutoScrollPolicy.TailWhenAtBottom)

// Always scroll to bottom when content changes
.WithAutoScroll(AutoScrollPolicy.AlwaysTail)

// Never auto-scroll
.WithAutoScroll(AutoScrollPolicy.None)
```

| Policy | Behavior |
|--------|----------|
| `TailWhenAtBottom` | Auto-scroll if at bottom, stay in place otherwise |
| `AlwaysTail` | Always scroll to show latest content |
| `None` | Never auto-scroll |

## Scrollbar

The scrollbar is shown automatically when content exceeds viewport:

```csharp
// Show scrollbar (default)
.WithScrollbar(true)

// Hide scrollbar
.WithScrollbar(false)

// Customize scrollbar colors
.WithScrollbarColors(
    track: Color.BrightBlack,
    thumb: Color.White)
```

## Programmatic Scrolling

```csharp
var scroller = new ScrollableContainerNode()
    .WithContent(content);

// Scroll methods
scroller.ScrollUp();
scroller.ScrollDown();
scroller.PageUp();
scroller.PageDown();
scroller.ScrollToTop();
scroller.ScrollToBottom();
scroller.ScrollTo(offset: 10);
```

## Scroll State

Query the current scroll state:

```csharp
var offset = scroller.ScrollOffset;      // Current position
var height = scroller.ContentHeight;     // Total content height
var canUp = scroller.CanScrollUp;        // Can scroll up?
var canDown = scroller.CanScrollDown;    // Can scroll down?
var atBottom = scroller.IsNearBottom;    // At or near bottom?
```

## Example: Chat with Auto-Scroll

```csharp
new ScrollableContainerNode()
    .WithContent(
        ViewModel.MessagesChanged
            .Select(messages =>
                Layouts.Vertical(
                    messages.Select(m =>
                        new TextNode(m.Text)
                            .WithForeground(m.IsUser ? Color.Cyan : Color.Green))
                    .ToArray()))
            .AsLayout())
    .WithAutoScroll(AutoScrollPolicy.TailWhenAtBottom)
    .Fill()
```

## API Reference

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AutoScroll` | `AutoScrollPolicy` | `TailWhenAtBottom` | Auto-scroll behavior |
| `ShowScrollbar` | `bool` | `true` | Show scrollbar |
| `ScrollbarTrackColor` | `Color` | `BrightBlack` | Track color |
| `ScrollbarThumbColor` | `Color` | `White` | Thumb color |
| `ScrollOffset` | `int` | `0` | Current scroll position |
| `ContentHeight` | `int` | - | Total content height |
| `CanScrollUp` | `bool` | - | Can scroll up |
| `CanScrollDown` | `bool` | - | Can scroll down |
| `MaxScroll` | `int` | - | Maximum scroll offset |
| `IsNearBottom` | `bool` | - | Within 2 lines of bottom |

### Methods

| Method | Description |
|--------|-------------|
| `.WithContent(ILayoutNode)` | Set scrollable content |
| `.WithAutoScroll(AutoScrollPolicy)` | Set auto-scroll policy |
| `.WithScrollbar(bool)` | Show/hide scrollbar |
| `.WithScrollbarColors(track, thumb)` | Set scrollbar colors |
| `.ScrollUp()` | Scroll up one line |
| `.ScrollDown()` | Scroll down one line |
| `.PageUp()` | Scroll up one page |
| `.PageDown()` | Scroll down one page |
| `.ScrollTo(int)` | Scroll to offset |
| `.ScrollToTop()` | Scroll to top |
| `.ScrollToBottom()` | Scroll to bottom |

## Source Code

::: details View ScrollableContainerNode implementation
<<< @/../src/Termina/Layout/ScrollableContainerNode.cs{csharp}
:::
