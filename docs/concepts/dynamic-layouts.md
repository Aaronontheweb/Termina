# Dynamic Layouts

`DynamicLayoutNode` re-evaluates a factory function on every render cycle, making it ideal for imperative, page-local state that doesn't need to live in a ViewModel as an observable.

## When to Use

| Scenario | Use |
|----------|-----|
| ViewModel state that is naturally observable | `ReactiveProperty<T>` + `.AsLayout()` |
| Page-local state (switch/case, enum-driven UI) | `Layouts.Dynamic()` |
| Observable trigger with imperative factory | `factory.AsDynamicLayout(trigger)` |

## Basic Usage

```csharp
public override ILayoutNode BuildLayout()
{
    // Page-local state — no need for a ViewModel property
    var currentTab = "home";

    return Layouts.Dynamic(() => currentTab switch
    {
        "home" => new TextNode("Welcome home"),
        "settings" => new TextNode("Settings panel"),
        _ => new TextNode("Unknown tab")
    });
}
```

## With Invalidation Trigger

When your factory depends on state that changes over time, use `Invalidate()` or the `AsDynamicLayout` extension to trigger re-evaluation:

```csharp
// Manual invalidation
var dynamicNode = Layouts.Dynamic(() => BuildCurrentContent());
// ... later, when state changes:
dynamicNode.Invalidate();

// Or subscribe an observable trigger
Func<ILayoutNode> factory = () => BuildCurrentContent();
var node = factory.AsDynamicLayout(someObservable.Select(_ => Unit.Default));
```

## Before / After Comparison

**Before** (manual `Subject<Unit>` merge trick):

```csharp
// ViewModel
public ReactiveProperty<int> CurrentTab { get; } = new(0);

// Page
ViewModel.CurrentTab
    .Select<int, ILayoutNode>(tab => tab switch
    {
        0 => BuildHomeTab(),
        1 => BuildSettingsTab(),
        _ => new EmptyNode()
    })
    .AsLayout()
```

**After** (one-liner with `Layouts.Dynamic`):

```csharp
// No ViewModel property needed — page-local state
var tab = 0;
var wizard = Layouts.Dynamic(() => tab switch
{
    0 => BuildHomeTab(),
    1 => BuildSettingsTab(),
    _ => new EmptyNode()
});
// Call wizard.Invalidate() when tab changes
```

## How It Works

- The factory is called on each `Measure()` and `Render()` cycle
- **Reference equality** detects child changes — returning the same instance avoids lifecycle transitions
- When the child changes: the old child is deactivated, the new child is activated (mirrors `ReactiveLayoutNode` behavior)
- `GetChildNodes()` returns the current child for focus tree traversal
- Extends `LayoutNode` so fluent sizing (`.Fill()`, `.Width()`, `.Height()`) works

## Fluent Sizing

```csharp
Layouts.Dynamic(() => BuildContent())
    .Fill()        // Fill remaining height
    .WidthFill()   // Fill available width
```

::: tip
For page-level state that doesn't need to be in the ViewModel, `Layouts.Dynamic()` avoids the overhead of creating a `ReactiveProperty<T>` and observable pipeline. For state that _should_ be in the ViewModel, use [Reactive Properties](../concepts/reactive-properties.md) instead.
:::
