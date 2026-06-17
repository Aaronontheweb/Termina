# ProgressBarNode

A single-row progress bar with gradient fill, customizable characters, and an optional label.

## Basic Usage

```csharp
new ProgressBarNode()
    .WithColor(Color.Green)
    .WithValue(0.5)
```

## Gradient Fill

Apply a gradient that maps color across the bar width:

```csharp
var gradient = Gradient.Create(
    Color.FromRgb(255, 50, 50),   // red at start
    Color.FromRgb(255, 200, 0),   // yellow in the middle
    Color.FromRgb(50, 255, 50));  // green at end

new ProgressBarNode()
    .WithGradient(gradient)
    .WithValue(0.75)
```

## Labels

Add a formatted label after the bar. The format string receives the normalized value (0–1):

```csharp
new ProgressBarNode()
    .WithColor(Color.Cyan)
    .WithValue(0.42)
    .WithLabel("{0:P0}")    // renders "42%"
```

## Custom Characters

Change the fill and empty characters:

```csharp
new ProgressBarNode()
    .WithFillChar('━')
    .WithEmptyChar('─')
    .WithEmptyColor(Color.DarkGray)
    .WithColor(Color.BrightGreen)
    .WithValue(0.6)
```

## Custom Range

By default the value range is 0–1. Override with `WithRange`:

```csharp
new ProgressBarNode()
    .WithRange(0, 200)
    .WithValue(150)       // 75% filled
    .WithLabel("{0:P0}")  // "75%"
```

## Reactive Updates

`WithValue` fires invalidation, so the bar repaints immediately:

```csharp
Observable.Interval(TimeSpan.FromMilliseconds(100), TimeProvider.System)
    .Subscribe(_ => progressBar.WithValue(GetProgress()));
```

## API Reference

### Constructor

```csharp
public ProgressBarNode()
```

Defaults to `Height(1)` and `WidthFill()`.

### Methods

| Method | Description |
|--------|-------------|
| `.WithGradient(Gradient)` | Apply gradient across filled portion |
| `.WithColor(Color)` | Single-color convenience |
| `.WithValue(double)` | Set current value and trigger repaint |
| `.WithRange(double min, double max)` | Set value range (default 0–1) |
| `.WithLabel(string format)` | Format string for label (receives normalized 0–1) |
| `.WithFillChar(char)` | Filled character (default `█`) |
| `.WithEmptyChar(char)` | Empty character (default `░`) |
| `.WithEmptyColor(Color)` | Color for empty portion (default `DarkGray`) |

## Source Code

::: details View ProgressBarNode implementation
<<< @/../src/Termina/Layout/ProgressBarNode.cs{csharp}
:::
