# GraphNode

A reactive, self-invalidating layout node that renders live scrolling graphs with optional gradient coloring. Implements `IAnimatedNode` so only the graph region repaints — not the parent layout.

::: details Demo — Live graph with gradient coloring, cycling through all 4 styles

![Graph Gallery demo cycling through Blocks, Outline, Braille, and ASCII styles with gradient coloring](/graphs-progress-gallery-panorama.gif)

:::

## Basic Usage

```csharp
var graph = new GraphNode()
    .WithColor(Color.Cyan)
    .WithRange(0, 100);

graph.SetData([10, 40, 70, 100, 60, 30]);
```

## Graph Styles

Four rendering styles are available:

```csharp
new GraphNode().WithStyle(GraphStyle.Blocks)   // ▁▂▃▄▅▆▇█ filled columns
new GraphNode().WithStyle(GraphStyle.Outline)  // Only the top edge drawn
new GraphNode().WithStyle(GraphStyle.Braille)  // Double vertical resolution
new GraphNode().WithStyle(GraphStyle.Ascii)    // _ . - ~ ^ * # @ fallback
```

| Style | Characters | Best for |
|-------|-----------|----------|
| `Blocks` | `▁▂▃▄▅▆▇█` | General use, high contrast |
| `Outline` | Top edge only | Sparse data, sparkline feel |
| `Braille` | `⠀⢀⣀⣤⣶⣿` | Double vertical resolution per row |
| `Ascii` | `_ . - ~ ^ * # @` | Terminals without Unicode |

## Gradient Coloring

Apply a gradient that maps color by row height:

```csharp
var gradient = Gradient.Create(
    Color.FromRgb(0, 100, 255),   // blue at the bottom
    Color.FromRgb(0, 255, 100),   // green in the middle
    Color.FromRgb(255, 255, 0));  // yellow at the top

var graph = new GraphNode()
    .WithGradient(gradient)
    .WithRange(0, 100);
```

For a single color, use the convenience method:

```csharp
new GraphNode().WithColor(Color.Green)
```

## Data-Driven Updates

`SetData` fires invalidation immediately so the graph repaints as soon as data arrives, independent of the internal timer interval:

```csharp
Observable.Interval(TimeSpan.FromMilliseconds(200), TimeProvider.System)
    .ObserveOn(RenderFrameProvider)
    .Subscribe(_ =>
    {
        dataPoints.Add(GetNextValue());
        graph.SetData(dataPoints.ToArray());
    });
```

The graph renders the rightmost `width` data points (or `width * 2` for Braille style). Earlier data scrolls off the left edge.

## Internal Timer

The constructor accepts an interval for periodic self-invalidation. Set `intervalMs: 0` to disable the internal timer entirely and rely only on `SetData` for repaints:

```csharp
// Timer-driven refresh (default 500ms), invalidated on the Termina loop
new GraphNode(intervalMs: 500)

// Data-driven only — no timer overhead
new GraphNode(intervalMs: 0)
```

## Testable Timing

`GraphNode` receives timing through `LayoutRuntimeContext`. In app code this happens automatically when the graph is attached to a page layout tree. For component-level tests, set a test runtime context before activation.

## API Reference

### Constructor

```csharp
public GraphNode(int intervalMs = 500)
```

### Methods

| Method | Description |
|--------|-------------|
| `.WithStyle(GraphStyle)` | Set rendering style |
| `.WithGradient(Gradient)` | Apply gradient coloring by row |
| `.WithColor(Color)` | Single-color convenience |
| `.WithRange(double min, double max)` | Set data value range (default 0–100) |
| `.SetData(double[])` | Push data and trigger repaint |
| `.Start()` | Start internal timer |
| `.Stop()` | Stop internal timer |

### GraphStyle Enum

| Style | Description |
|-------|-------------|
| `Blocks` | Filled block columns (default) |
| `Outline` | Top edge only |
| `Braille` | Double resolution braille dots |
| `Ascii` | ASCII fallback characters |

## Source Code

::: details View GraphNode implementation
<<< @/../src/Termina/Layout/GraphNode.cs{csharp}
:::
