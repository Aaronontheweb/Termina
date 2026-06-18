# Custom Components

Build reusable layout nodes that integrate with Termina's rendering system.

## ILayoutNode Interface

All layout nodes implement `ILayoutNode`:

```csharp
public interface ILayoutNode : IDisposable
{
    SizeConstraint WidthConstraint { get; }
    SizeConstraint HeightConstraint { get; }
    Size Measure(Size available);
    void Render(IRenderContext context, Rect bounds);
}
```

## Accessing Dimensions

During rendering, you can access available dimensions from two sources:

### From Rect bounds (Recommended)

The `Render` method receives a `Rect bounds` parameter containing the allocated space:

```csharp
public void Render(IRenderContext context, Rect bounds)
{
    int width = bounds.Width;   // Allocated width
    int height = bounds.Height; // Allocated height
    int x = bounds.X;           // Left position
    int y = bounds.Y;           // Top position
}
```

### From IRenderContext

The render context also exposes the total region dimensions:

```csharp
public void Render(IRenderContext context, Rect bounds)
{
    int regionWidth = context.Width;   // Total region width
    int regionHeight = context.Height; // Total region height
}
```

### From Size available (During Measurement)

The `Measure` method receives available space to determine desired size:

```csharp
public Size Measure(Size available)
{
    int maxWidth = available.Width;
    int maxHeight = available.Height;

    // Return desired size (should not exceed available)
    return new Size(Math.Min(desiredWidth, maxWidth), Math.Min(desiredHeight, maxHeight));
}
```

## Creating a Simple Node

Here's a minimal custom node:

```csharp
public class ProgressBarNode : LayoutNode
{
    private double _progress;
    private Color _fillColor = Color.Green;
    private Color _emptyColor = Color.Gray;

    public ProgressBarNode()
    {
        WidthConstraint = new SizeConstraint.Fill();
        HeightConstraint = new SizeConstraint.Fixed(1);
    }

    public ProgressBarNode WithProgress(double value)
    {
        _progress = Math.Clamp(value, 0, 1);
        return this;
    }

    public ProgressBarNode WithColors(Color fill, Color empty)
    {
        _fillColor = fill;
        _emptyColor = empty;
        return this;
    }

    public override Size Measure(Size available)
    {
        // Take full width, 1 row height
        return new Size(available.Width, 1);
    }

    public override void Render(IRenderContext context, Rect bounds)
    {
        var filledWidth = (int)(bounds.Width * _progress);

        // Render filled portion
        context.SetForeground(_fillColor);
        for (int x = 0; x < filledWidth; x++)
        {
            context.WriteAt(bounds.X + x, bounds.Y, '█');
        }

        // Render empty portion
        context.SetForeground(_emptyColor);
        for (int x = filledWidth; x < bounds.Width; x++)
        {
            context.WriteAt(bounds.X + x, bounds.Y, '░');
        }

        context.ResetColors();
    }
}
```

## Usage

```csharp
return Layouts.Vertical()
    .WithChild(new TextNode("Download Progress:"))
    .WithChild(
        new ProgressBarNode()
            .WithProgress(0.75)
            .WithColors(Color.Cyan, Color.DarkGray)
            .Height(1));
```

## Fluent Size Methods

The `LayoutNode` base class provides fluent methods for size constraints:

```csharp
// Fixed sizes
node.Width(20);      // Fixed width of 20 columns
node.Height(5);      // Fixed height of 5 rows

// Fill remaining space
node.WidthFill();    // Fill available width
node.Fill();         // Fill available height (note: uses Fill(), not HeightFill())

// Auto-size based on content
node.WidthAuto();
node.HeightAuto();

// Percentage-based
node.WidthPercent(50);   // 50% of available width
node.HeightPercent(25);  // 25% of available height
```

You can also set constraints directly in your constructor:

```csharp
public MyNode()
{
    WidthConstraint = new SizeConstraint.Fixed(20);
    HeightConstraint = new SizeConstraint.Fill { Weight = 1 };
}
```

## Container Nodes

For nodes that contain children:

```csharp
public class BorderedContainer : LayoutNode
{
    private ILayoutNode? _content;
    private BorderStyle _style = BorderStyle.Single;

    public BorderedContainer()
    {
        WidthConstraint = new SizeConstraint.Fill();
        HeightConstraint = new SizeConstraint.Fill();
    }

    public BorderedContainer WithContent(ILayoutNode content)
    {
        _content = content;
        return this;
    }

    public override Size Measure(Size available)
    {
        // Account for border (2 chars width, 2 chars height)
        var contentSpace = new Size(
            Math.Max(0, available.Width - 2),
            Math.Max(0, available.Height - 2));

        if (_content != null)
        {
            var contentSize = _content.Measure(contentSpace);
            return new Size(contentSize.Width + 2, contentSize.Height + 2);
        }

        return new Size(2, 2);
    }

    public override void Render(IRenderContext context, Rect bounds)
    {
        // Draw border
        DrawBorder(context, bounds, _style);

        // Render content in inner area
        if (_content != null)
        {
            var innerBounds = new Rect(
                bounds.X + 1,
                bounds.Y + 1,
                bounds.Width - 2,
                bounds.Height - 2);
            _content.Render(context, innerBounds);
        }
    }

    private void DrawBorder(IRenderContext context, Rect bounds, BorderStyle style)
    {
        // Border drawing implementation...
    }
}
```

## Reactive Custom Nodes

For nodes that need to update based on observables:

```csharp
public class LiveValueNode : LayoutNode
{
    private string _currentValue = "";
    private IDisposable? _subscription;

    public LiveValueNode()
    {
        WidthConstraint = new SizeConstraint.Fill();
        HeightConstraint = new SizeConstraint.Fixed(1);
    }

    public LiveValueNode BindTo(Observable<string> source, Action requestRedraw)
    {
        _subscription?.Dispose();
        _subscription = source.Subscribe(value =>
        {
            _currentValue = value;
            requestRedraw();
        });
        return this;
    }

    public override Size Measure(Size available)
    {
        return new Size(
            Math.Min(_currentValue.Length, available.Width),
            1);
    }

    public override void Render(IRenderContext context, Rect bounds)
    {
        var text = _currentValue.Length > bounds.Width
            ? _currentValue[..bounds.Width]
            : _currentValue;

        context.WriteAt(bounds.X, bounds.Y, text);
    }

    public override void Dispose()
    {
        _subscription?.Dispose();
        base.Dispose();
    }
}
```

## Runtime Context

App-owned layout nodes receive a `LayoutRuntimeContext` before activation. If you inherit from `LayoutNode`, access it through the protected `RuntimeContext` property.

The context contains:

- `RenderFrameProvider` for R3 render-loop delivery
- `TimeProvider` configured on the owning application
- `RequestRedraw` for manual redraw requests

Use it for timers, background observable delivery, and custom invalidation. Constructors should configure the node; runtime work should start in `OnActivate()` after context is available.

```csharp
public sealed class BlinkingStatusNode : LayoutNode, IInvalidatingNode
{
    private readonly Subject<Unit> _invalidated = new();
    private IDisposable? _timer;
    private bool _visible = true;

    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    public override void OnActivate()
    {
        var context = RuntimeContext
            ?? throw new InvalidOperationException("Node has not been attached to a Termina layout tree.");

        _timer ??= Observable.Interval(TimeSpan.FromMilliseconds(500), context.TimeProvider)
            .ObserveOn(context.RenderFrameProvider)
            .Subscribe(_ =>
            {
                _visible = !_visible;
                _invalidated.OnNext(Unit.Default);
            });

        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        _timer?.Dispose();
        _timer = null;
        base.OnDeactivate();
    }

    public override Size Measure(Size available) => new(Math.Min(6, available.Width), 1);

    public override void Render(IRenderContext context, Rect bounds)
    {
        if (_visible)
            context.WriteAt(bounds.X, bounds.Y, "ONLINE"[..Math.Min(bounds.Width, 6)]);
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        base.Dispose();
    }
}
```

If your node implements `ILayoutNode` directly instead of inheriting from `LayoutNode`, implement `ILayoutRuntimeContextAware` to receive the same context.

If your `LayoutNode` subclass owns child nodes outside `ContainerNode`, propagate context when the child is assigned:

```csharp
public override void SetRuntimeContext(LayoutRuntimeContext context)
{
    base.SetRuntimeContext(context);

    if (_content != null)
        ApplyRuntimeContextToChild(_content);
}
```

`ContainerNode` handles this automatically for children added through `AddChild` and `AddChildren`.

## Component Lifecycle

When using `NavigationBehavior.PreserveState`, layout nodes are preserved across navigations and go through activation/deactivation cycles instead of being disposed. This prevents race conditions with in-flight events and allows components to maintain state.

### Implementing Lifecycle Methods

Lifecycle dispatch goes through the `IActivatableNode` interface. `LayoutNode` implements it, so subclasses simply override `OnActivate()` and `OnDeactivate()`. Components that implement the layout interfaces directly — without extending `LayoutNode` — should implement `IActivatableNode` themselves to participate (this is what `FilePickerNode` and `SelectionListNode` do).

```csharp
public class AnimatedNode : LayoutNode, IInvalidatingNode
{
    private readonly Subject<Unit> _invalidated = new();
    private IDisposable? _timer;
    private int _frame;

    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    public override void OnActivate()
    {
        var context = RuntimeContext
            ?? throw new InvalidOperationException("Node has not been attached to a Termina layout tree.");

        // Resume animation when page becomes active
        _timer ??= Observable.Interval(TimeSpan.FromMilliseconds(100), context.TimeProvider)
            .ObserveOn(context.RenderFrameProvider)
            .Subscribe(_ =>
            {
                _frame++;
                _invalidated.OnNext(Unit.Default);
            });
        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        // Pause animation when navigating away
        _timer?.Dispose();
        _timer = null;
        base.OnDeactivate();
    }

    public override void Dispose()
    {
        // Final cleanup when component is destroyed
        _timer?.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        base.Dispose();
    }
}
```

### When to Implement Lifecycle

Implement `OnActivate()` and `OnDeactivate()` when your component:

- **Uses timers** - Stop/start timers to avoid background work
- **Subscribes to observables** - Pause subscriptions to prevent processing events while inactive
- **Animates** - Pause animations when not visible
- **Holds expensive resources** - Temporarily release resources when inactive

### Lifecycle vs Disposal

Key differences:

| Method | When Called | Purpose | Subjects/Observables |
|--------|-------------|---------|---------------------|
| `OnDeactivate()` | Navigating away (PreserveState) | Pause, don't destroy | Keep alive, pause subscriptions |
| `Dispose()` | Page destroyed or app shutdown | Final cleanup | Complete and dispose |

**Important**: `OnDeactivate()` should **not** dispose Subjects or complete observables. It should only pause active resources like timers and subscriptions.

### Example: Reactive Node with Lifecycle

```csharp
public class LiveDataNode : LayoutNode, IInvalidatingNode
{
    private readonly Observable<string> _source;
    private readonly FrameProvider _frameProvider;
    private readonly Subject<Unit> _invalidated = new();
    private IDisposable? _subscription;
    private string _currentValue = "";

    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    public LiveDataNode(Observable<string> source, FrameProvider frameProvider)
    {
        _source = source;
        _frameProvider = frameProvider;
        _subscription = SubscribeToSource();
    }

    private IDisposable SubscribeToSource() =>
        _source.ObserveOn(_frameProvider).Subscribe(value =>
        {
            _currentValue = value;
            _invalidated.OnNext(Unit.Default);
        });

    public override void OnActivate()
    {
        // Recreate the subscription if it was paused during deactivation
        _subscription ??= SubscribeToSource();
        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        // Pause subscription - don't dispose the Subject!
        _subscription?.Dispose();
        _subscription = null;
        base.OnDeactivate();
    }

    public override void Dispose()
    {
        // Final cleanup
        _subscription?.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        base.Dispose();
    }
}
```

### Container Lifecycle Propagation

If your custom container holds child nodes, propagate lifecycle calls. Dispatch on `IActivatableNode` — not the concrete `LayoutNode` class — so interface-based children (like `FilePickerNode`) receive lifecycle calls too:

```csharp
public class CustomContainer : LayoutNode
{
    private readonly List<ILayoutNode> _children = new();

    public override void OnActivate()
    {
        // Activate all children
        foreach (var child in _children)
        {
            if (child is IActivatableNode node)
                node.OnActivate();
        }
        base.OnActivate();
    }

    public override void OnDeactivate()
    {
        // Deactivate all children
        foreach (var child in _children)
        {
            if (child is IActivatableNode node)
                node.OnDeactivate();
        }
        base.OnDeactivate();
    }

    public override void Dispose()
    {
        // Dispose all children
        foreach (var child in _children)
        {
            child.Dispose();
        }
        base.Dispose();
    }
}
```

## Best Practices

### Measurement

- Return sizes that fit within `available`
- Account for borders, padding, and decorations
- Handle zero-size gracefully

### Rendering

- Only render within your `bounds`
- Use `context.WriteAt()` for character-level control
- Use `context.SetForeground()` and `context.SetBackground()` for colors
- Call `context.ResetColors()` after custom styling
- Check bounds before rendering to avoid overflow

### Immutability

- Fluent methods should return `this` for chaining
- Create new instances for fundamentally different configurations
- Avoid mutation during measure/render

### Performance

- Cache expensive calculations
- Minimize allocations in hot paths
- Use `Span<char>` for string operations when possible

## Integrating with Built-in Nodes

Compose with existing nodes:

```csharp
public class LabeledProgressBar : LayoutNode
{
    private readonly VerticalLayout _layout;

    public LabeledProgressBar(string label, double progress)
    {
        _layout = Layouts.Vertical()
            .WithChild(new TextNode(label).Height(1))
            .WithChild(new ProgressBarNode().WithProgress(progress).Height(1));

        HeightConstraint = new SizeConstraint.Fixed(2);
    }

    public override Size Measure(Size available) => _layout.Measure(available);

    public override void Render(IRenderContext context, Rect bounds) =>
        _layout.Render(context, bounds);
}
```
