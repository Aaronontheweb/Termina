# Custom Components

Build reusable layout nodes that integrate with Termina's rendering system.

## ILayoutNode Interface

All layout nodes implement `ILayoutNode`:

```csharp
public interface ILayoutNode
{
    SizeConstraint Width { get; }
    SizeConstraint Height { get; }
    Size Measure(Size availableSpace);
    void Render(IRenderContext context, ScreenBounds bounds);
}
```

## Creating a Simple Node

Here's a minimal custom node:

```csharp
public class ProgressBarNode : ILayoutNode
{
    private double _progress;
    private Color _fillColor = Color.Green;
    private Color _emptyColor = Color.Gray;

    public SizeConstraint Width { get; private set; } = SizeConstraint.Fill();
    public SizeConstraint Height { get; private set; } = SizeConstraint.Fixed(1);

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

    public Size Measure(Size availableSpace)
    {
        // Take full width, 1 row height
        return new Size(availableSpace.Width, 1);
    }

    public void Render(IRenderContext context, ScreenBounds bounds)
    {
        var filledWidth = (int)(bounds.Width * _progress);

        // Render filled portion
        for (int x = 0; x < filledWidth; x++)
        {
            context.SetCell(bounds.Left + x, bounds.Top, '█', _fillColor, Color.Default);
        }

        // Render empty portion
        for (int x = filledWidth; x < bounds.Width; x++)
        {
            context.SetCell(bounds.Left + x, bounds.Top, '░', _emptyColor, Color.Default);
        }
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

Add fluent methods for size constraints:

```csharp
public ProgressBarNode Width(int size)
{
    Width = SizeConstraint.Fixed(size);
    return this;
}

public ProgressBarNode Fill(int weight = 1)
{
    Width = SizeConstraint.Fill(weight);
    return this;
}

public ProgressBarNode Height(int size)
{
    Height = SizeConstraint.Fixed(size);
    return this;
}
```

## Container Nodes

For nodes that contain children:

```csharp
public class BorderedContainer : ILayoutNode
{
    private ILayoutNode? _content;
    private BorderStyle _style = BorderStyle.Single;

    public SizeConstraint Width { get; private set; } = SizeConstraint.Fill();
    public SizeConstraint Height { get; private set; } = SizeConstraint.Fill();

    public BorderedContainer WithContent(ILayoutNode content)
    {
        _content = content;
        return this;
    }

    public Size Measure(Size availableSpace)
    {
        // Account for border (2 chars width, 2 chars height)
        var contentSpace = new Size(
            Math.Max(0, availableSpace.Width - 2),
            Math.Max(0, availableSpace.Height - 2));

        if (_content != null)
        {
            var contentSize = _content.Measure(contentSpace);
            return new Size(contentSize.Width + 2, contentSize.Height + 2);
        }

        return new Size(2, 2);
    }

    public void Render(IRenderContext context, ScreenBounds bounds)
    {
        // Draw border
        DrawBorder(context, bounds, _style);

        // Render content in inner area
        if (_content != null)
        {
            var innerBounds = new ScreenBounds(
                bounds.Left + 1,
                bounds.Top + 1,
                bounds.Width - 2,
                bounds.Height - 2);
            _content.Render(context, innerBounds);
        }
    }

    private void DrawBorder(IRenderContext context, ScreenBounds bounds, BorderStyle style)
    {
        // Border drawing implementation...
    }
}
```

## Reactive Custom Nodes

For nodes that need to update based on observables:

```csharp
public class LiveValueNode : ILayoutNode
{
    private string _currentValue = "";
    private IDisposable? _subscription;

    public SizeConstraint Width { get; private set; } = SizeConstraint.Fill();
    public SizeConstraint Height { get; private set; } = SizeConstraint.Fixed(1);

    public LiveValueNode BindTo(IObservable<string> source, Action requestRedraw)
    {
        _subscription?.Dispose();
        _subscription = source.Subscribe(value =>
        {
            _currentValue = value;
            requestRedraw();
        });
        return this;
    }

    public Size Measure(Size availableSpace)
    {
        return new Size(
            Math.Min(_currentValue.Length, availableSpace.Width),
            1);
    }

    public void Render(IRenderContext context, ScreenBounds bounds)
    {
        var text = _currentValue.Length > bounds.Width
            ? _currentValue[..bounds.Width]
            : _currentValue;

        for (int i = 0; i < text.Length; i++)
        {
            context.SetCell(bounds.Left + i, bounds.Top, text[i], Color.Default, Color.Default);
        }
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

## Best Practices

### Measurement

- Return sizes that fit within `availableSpace`
- Account for borders, padding, and decorations
- Handle zero-size gracefully

### Rendering

- Only render within your `bounds`
- Use `context.SetCell()` for character-level control
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
public class LabeledProgressBar : ILayoutNode
{
    private readonly VerticalLayout _layout;

    public LabeledProgressBar(string label, double progress)
    {
        _layout = Layouts.Vertical()
            .WithChild(new TextNode(label).Height(1))
            .WithChild(new ProgressBarNode().WithProgress(progress).Height(1));
    }

    public SizeConstraint Width => _layout.Width;
    public SizeConstraint Height => SizeConstraint.Fixed(2);

    public Size Measure(Size availableSpace) => _layout.Measure(availableSpace);
    public void Render(IRenderContext context, ScreenBounds bounds) =>
        _layout.Render(context, bounds);
}
```
