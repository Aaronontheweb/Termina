# DeferredNode

A layout node that delegates to another node without owning or disposing it. Essential for showing/hiding modals and other components that should persist across layout changes.

## The Problem

When using reactive layouts with `AsLayout()`, the previous layout node is disposed whenever a new value is emitted:

```csharp
// Problem: Modal gets disposed when hidden!
ViewModel.ShowModalChanged
    .Select(show => show ? ViewModel.Modal : Layouts.Empty())
    .AsLayout()  // When show becomes false, Modal is disposed!
```

This causes `ObjectDisposedException` when you try to show the modal again.

## The Solution

`DeferredNode` wraps access to a node without taking ownership:

```csharp
// Solution: Use Layouts.Deferred()
ViewModel.ShowModalChanged
    .Select(show => show
        ? Layouts.Deferred(() => ViewModel.Modal)  // Doesn't dispose the modal
        : (ILayoutNode)Layouts.Empty())
    .AsLayout()
```

## Basic Usage

```csharp
// Create a node you want to reuse
var modal = Layouts.Modal()
    .WithTitle("My Modal")
    .WithContent(content);

// In your layout, wrap it with Deferred
var layout = Layouts.Stack()
    .WithChild(mainContent)
    .WithChild(
        showModalObservable
            .Select(show => show
                ? Layouts.Deferred(() => modal)
                : (ILayoutNode)Layouts.Empty())
            .AsLayout());
```

## How It Works

- `DeferredNode` calls the provided factory function on each render
- When `DeferredNode` is disposed (layout changes), it does NOT dispose the underlying node
- The underlying node's lifecycle is managed by its owner (typically a ViewModel)

## Complete Example

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    [Reactive] private bool _showConfirmation;

    private ModalNode? _confirmModal;

    public ModalNode? ConfirmModal => _confirmModal;

    public override void OnActivated()
    {
        _confirmModal = Layouts.Modal()
            .WithTitle("Confirm")
            .WithContent(new TextNode("Are you sure?"));

        _confirmModal.Dismissed
            .Subscribe(_ => ShowConfirmation = false)
            .DisposeWith(Subscriptions);
    }

    protected override void Dispose(bool disposing)
    {
        // ViewModel owns the modal and disposes it here
        _confirmModal?.Dispose();
        base.Dispose(disposing);
    }
}

public class MyPage : ReactivePage<MyViewModel>
{
    public override ILayoutNode BuildLayout()
    {
        return Layouts.Stack()
            .WithChild(BuildMainContent())
            .WithChild(
                // Deferred prevents disposal when modal is hidden
                ViewModel.ShowConfirmationChanged
                    .Select(show => show
                        ? Layouts.Deferred(() => ViewModel.ConfirmModal)
                        : (ILayoutNode)Layouts.Empty())
                    .AsLayout());
    }
}
```

## When to Use DeferredNode

Use `Layouts.Deferred()` when:

1. **Modals** - Components that show/hide but should retain state
2. **Tabs or panels** - Content that switches but shouldn't reset
3. **Cached views** - Pre-rendered content you want to reuse
4. **Any node owned by ViewModel** - Where lifecycle is managed externally

## When NOT to Use DeferredNode

Don't use `Layouts.Deferred()` when:

1. **Creating new nodes each time** - Just return the node directly
2. **Nodes should be recreated** - Fresh state on each show
3. **Simple conditional content** - Use `When.True()` or `ConditionalNode`

## API Reference

### Factory Method

```csharp
Layouts.Deferred(Func<ILayoutNode?> getNode)
```

### Behavior

| Method | DeferredNode Behavior |
|--------|----------------------|
| `Measure()` | Delegates to underlying node |
| `Render()` | Delegates to underlying node |
| `Dispose()` | Does nothing - node is NOT disposed |

### Properties

| Property | Behavior |
|----------|----------|
| `WidthConstraint` | Returns underlying node's constraint (or AutoSize if null) |
| `HeightConstraint` | Returns underlying node's constraint (or AutoSize if null) |

## Source Code

::: details View DeferredNode implementation
<<< @/../src/Termina/Layout/DeferredNode.cs{csharp}
:::
