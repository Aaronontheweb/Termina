# SpinnerNode

An animated loading indicator with configurable styles and label.

![SpinnerNode demo: cycling through spinner styles with a live animated preview](/gallery/gallery-spinners.gif)

*The Animations gallery, cycling through spinner styles.*

## Basic Usage

```csharp
new SpinnerNode()
    .WithLabel("Loading...")
```

## Spinner Styles

Termina includes six spinner animation styles:

```csharp
new SpinnerNode(SpinnerStyle.Dots)    // ⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏
new SpinnerNode(SpinnerStyle.Line)    // - \ | /
new SpinnerNode(SpinnerStyle.Arrow)   // ← ↖ ↑ ↗ → ↘ ↓ ↙
new SpinnerNode(SpinnerStyle.Bounce)  // ⠁ ⠂ ⠄ ⠂
new SpinnerNode(SpinnerStyle.Box)     // ▖ ▘ ▝ ▗
new SpinnerNode(SpinnerStyle.Circle)  // ◐ ◓ ◑ ◒
```

## Animation Speed

Control the animation interval:

```csharp
// Default: 80ms between frames
new SpinnerNode(SpinnerStyle.Dots)

// Slower animation
new SpinnerNode(SpinnerStyle.Dots, intervalMs: 150, frameProvider: RenderFrameProvider)

// Faster animation
new SpinnerNode(SpinnerStyle.Dots, intervalMs: 50, frameProvider: RenderFrameProvider)
```

## Styling

```csharp
new SpinnerNode(SpinnerStyle.Dots)
    .WithLabel("Processing...")
    .WithSpinnerColor(Color.Cyan)
    .WithLabelColor(Color.Gray)
```

## Start/Stop Animation

Spinners auto-start by default but can be controlled:

```csharp
var spinner = new SpinnerNode().WithLabel("Working...");

// Stop animation
spinner.Stop();

// Resume animation
spinner.Start();
```

## Conditional Display

Show spinner only when loading:

```csharp
ViewModel.IsLoadingChanged
    .Select(loading => loading
        ? (ILayoutNode)new SpinnerNode().WithLabel("Loading...")
        : new EmptyNode())
    .AsLayout(RenderFrameProvider)
```

Pass `frameProvider: RenderFrameProvider` when using spinners in pages so timer-driven invalidation is delivered on the Termina render loop.

## API Reference

### Constructor

```csharp
public SpinnerNode(
    SpinnerStyle style = SpinnerStyle.Dots,
    int intervalMs = 80,
    TimeProvider? timeProvider = null,
    FrameProvider? frameProvider = null)
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Label` | `string?` | `null` | Text after spinner |
| `SpinnerColor` | `Color?` | `null` | Spinner color |
| `LabelColor` | `Color?` | `null` | Label color |
| `IsAnimating` | `bool` | `true` | Animation running |

### Methods

| Method | Description |
|--------|-------------|
| `.WithLabel(string)` | Set label text |
| `.WithSpinnerColor(Color)` | Set spinner color |
| `.WithLabelColor(Color)` | Set label color |
| `.Start()` | Start animation |
| `.Stop()` | Stop animation |

### SpinnerStyle Enum

| Style | Frames | Description |
|-------|--------|-------------|
| `Dots` | ⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏ | Braille dots |
| `Line` | - \ \| / | Classic line spinner |
| `Arrow` | ← ↖ ↑ ↗ → ↘ ↓ ↙ | Rotating arrow |
| `Bounce` | ⠁ ⠂ ⠄ ⠂ | Bouncing dot |
| `Box` | ▖ ▘ ▝ ▗ | Rotating box |
| `Circle` | ◐ ◓ ◑ ◒ | Rotating circle |

## Source Code

::: details View SpinnerNode implementation
<<< @/../src/Termina/Layout/SpinnerNode.cs{csharp}
:::
