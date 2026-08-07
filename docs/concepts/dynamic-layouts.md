# Dynamic Layouts

`DynamicLayoutNode` evaluates a factory on the first render. It evaluates the factory again after a call to `Invalidate()`.

`KeyedDynamicLayoutNode<TKey>` preserves content while a key stays active. Its cache policy controls retention of inactive content.

## When to Use

| Scenario | Use |
|----------|-----|
| Tabs or wizard steps must keep state after a return | `Layouts.KeyedDynamic<TKey>()` with the default `AllKeys` policy |
| A workflow screen must be fresh after a return | `Layouts.KeyedDynamic<TKey>()` with `CurrentOnly` |
| Observable-driven content updates | `ReactiveProperty<T>` + `.AsLayout()` or `factory.AsDynamicLayout(trigger)` |
| Advanced: custom factory with manual caching | `Layouts.Dynamic()` — low-level, you manage caching |

## Basic Usage

### KeyedDynamic (Recommended)

The natural way to write content that switches based on a key. No manual caching needed — child state (highlights, typed text, focus) is preserved across key changes:

```csharp
public override ILayoutNode BuildLayout()
{
    var currentTab = 0;

    var layout = Layouts.KeyedDynamic(
        () => currentTab,
        tab => tab switch
        {
            0 => new TextNode("Welcome home"),
            1 => new TextNode("Settings panel"),
            _ => new TextNode("Unknown tab")
        });

    // Change tab and invalidate:
    currentTab = 1;
    layout.Invalidate();

    return layout;
}
```

### Current-Only Cache

Use `CurrentOnly` for a workflow state machine. The node preserves the active screen during same-key invalidation.

A new key replaces the active screen. A return to a prior key creates fresh content.

```csharp
var layout = Layouts.KeyedDynamic(
    () => currentStep,
    step => BuildStep(step),
    KeyedDynamicCachePolicy.CurrentOnly);
```

Termina deactivates replaced content during invalidation. It disposes the content on the next layout pass.

### Dynamic (Low-Level)

For cases where you need full control over the factory:

```csharp
var dynamicNode = Layouts.Dynamic(() => BuildCurrentContent());
// ... later, when state changes:
dynamicNode.Invalidate();
```

## Choosing the Right Dynamic Node

| Need | Use |
|------|-----|
| Tabs or wizard steps must keep state after a return | `Layouts.KeyedDynamic<TKey>()` with `AllKeys` |
| A workflow screen must be fresh after a return | `Layouts.KeyedDynamic<TKey>()` with `CurrentOnly` |
| Observable-driven content updates | `observable.AsLayout()` or `factory.AsDynamicLayout(trigger)` |
| Advanced: custom factory with manual caching | `Layouts.Dynamic()` — low-level, you manage caching |

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

**After** (one-liner with `Layouts.KeyedDynamic`):

```csharp
// No ViewModel property needed — page-local state
var tab = 0;
var layout = Layouts.KeyedDynamic(
    () => tab,
    t => t switch
    {
        0 => BuildHomeTab(),
        1 => BuildSettingsTab(),
        _ => new EmptyNode()
    });
// Call layout.Invalidate() when tab changes
```

## How It Works

- The factory is called once on first `Measure()`/`Render()`, then only when `Invalidate()` is called
- `Invalidate()` eagerly evaluates the factory so the new child is immediately available for tree traversal (e.g., focus propagation)
- **Reference equality** detects child changes — returning the same instance avoids lifecycle transitions
- When the child changes: the old child is deactivated, the new child is activated (mirrors `ReactiveLayoutNode` behavior)
- The default `AllKeys` policy reuses content after a return to a prior key
- The `CurrentOnly` policy reuses content only while the key stays active
- `CurrentOnly` disposes replaced content on the next layout pass
- `GetChildNodes()` returns the current child for focus tree traversal
- Extends `LayoutNode` so fluent sizing (`.Fill()`, `.Width()`, `.Height()`) works

## Fluent Sizing

```csharp
Layouts.KeyedDynamic(() => currentTab, tab => BuildTab(tab))
    .Fill()        // Fill remaining height
    .WidthFill()   // Fill available width
```

::: tip
For page-level state that doesn't need to be in the ViewModel, `Layouts.KeyedDynamic()` avoids the overhead of creating a `ReactiveProperty<T>` and observable pipeline. For state that _should_ be in the ViewModel, use [Reactive Properties](../concepts/reactive-properties.md) instead.
:::
