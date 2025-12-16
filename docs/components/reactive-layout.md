# ReactiveLayoutNode

Updates its content based on an observable stream. This is the core building block for reactive UI binding.

## Basic Usage

```csharp
// From observable of layout nodes
new ReactiveLayoutNode(
    ViewModel.CountChanged
        .Select(c => new TextNode($"Count: {c}")))

// Using the AsLayout() extension method (preferred)
ViewModel.CountChanged
    .Select(c => new TextNode($"Count: {c}"))
    .AsLayout()
```

## How It Works

1. Subscribe to the source observable
2. When a new value is emitted, dispose the old child node
3. Replace with the new layout node from the transform
4. Signal invalidation to trigger re-render

## Extension Method

The `AsLayout()` extension method provides a cleaner syntax:

```csharp
// These are equivalent:
new ReactiveLayoutNode(observable)
observable.AsLayout()

// With transform:
new ReactiveLayoutNode<int>(source, value => new TextNode($"{value}"))
source.Select(value => new TextNode($"{value}")).AsLayout()
```

## Common Patterns

### Reactive Text

```csharp
ViewModel.StatusChanged
    .Select(status => new TextNode(status)
        .WithForeground(Color.Yellow))
    .AsLayout()
```

### Conditional Content

```csharp
ViewModel.HasErrorChanged
    .Select(hasError => hasError
        ? new TextNode("Error occurred!").WithForeground(Color.Red)
        : (ILayoutNode)new EmptyNode())
    .AsLayout()
```

### Dynamic Lists

```csharp
ViewModel.ItemsChanged
    .Select(items => Layouts.Vertical(
        items.Select(i => new TextNode(i.Name)).ToArray()))
    .AsLayout()
```

### Styled Based on Value

```csharp
ViewModel.HealthChanged
    .Select(health => new TextNode($"Health: {health}%")
        .WithForeground(health > 50 ? Color.Green : Color.Red))
    .AsLayout()
```

## Size Constraints

Apply constraints to the reactive node itself:

```csharp
ViewModel.CountChanged
    .Select(c => new TextNode($"{c}"))
    .AsLayout()
    .Height(1)          // Fixed height
    .WidthFill()        // Fill available width
```

## Performance Considerations

::: warning
Each emission creates a new layout node and disposes the old one. For frequently updating content, consider:

1. **Stateful nodes outside reactive** - Keep `TextInputNode`, `StreamingTextNode` etc. as properties, not inside reactive wrappers
2. **Debounce high-frequency updates** - Use `.Throttle()` or `.Sample()` operators
3. **Minimize node creation** - Update properties rather than recreating entire subtrees
:::

### Good: Stateful node as property

```csharp
public class MyViewModel : ReactiveViewModel
{
    // Node lives outside reactive wrapper
    public TextInputNode Input { get; } = new TextInputNode();
}

// In page:
new PanelNode()
    .WithContent(ViewModel.Input)  // Not wrapped in reactive
```

### Avoid: Recreating stateful nodes

```csharp
// Bad - TextInputNode is recreated on every change
ViewModel.SomethingChanged
    .Select(_ => new TextInputNode())  // State is lost!
    .AsLayout()
```

## API Reference

### Constructors

```csharp
// From observable of layout nodes
public ReactiveLayoutNode(
    IObservable<ILayoutNode> source,
    ILayoutNode? initialChild = null)

// From observable with transform
public ReactiveLayoutNode<T>(
    IObservable<T> source,
    Func<T, ILayoutNode> transform)
```

### Extension Methods

```csharp
// Convert observable of layout nodes
IObservable<ILayoutNode>.AsLayout() -> ReactiveLayoutNode

// Observable transform (via Select) + AsLayout
observable.Select(transform).AsLayout()
```

## Source Code

::: details View ReactiveLayoutNode implementation
<<< @/../src/Termina/Layout/ReactiveLayoutNode.cs{csharp}
:::
