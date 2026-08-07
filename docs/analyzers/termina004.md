# TERMINA004: Stateful node recreation

## Summary

| Property | Value |
| --- | --- |
| Severity | Warning |
| Category | `Termina.State` |
| First known version | 0.16.0 |
| Code fix | None in the current source |

## Bad behavior

The rule reports a call to `Invalidate()` on a `DynamicLayoutNode` field when its factory creates a stateful layout node. The current source treats these types as stateful:

- `ScrollableContainerNode`
- `SelectionListNode<T>`
- `TextInputNode`
- `StreamingTextNode`

The analyzer also checks a private helper that the factory calls one level deep. It treats a `??=` or `??` fallback as a reuse pattern.

## Risk

`DynamicLayoutNode` runs its factory again when code calls `Invalidate()`. A new stateful node loses state such as scroll offset, selected index, input text, or cursor position. The user can lose their current UI position or input.

## Bad example

```csharp
using Termina.Layout;

public sealed class ResultsPage
{
    private DynamicLayoutNode? _content;

    public ILayoutNode BuildLayout()
    {
        _content = new DynamicLayoutNode(
            () => new ScrollableContainerNode());
        return _content;
    }

    public void Refresh()
        => _content?.Invalidate();
}
```

Each `Invalidate()` call creates a new `ScrollableContainerNode`. The new node does not keep the old scroll state.

## Good example

Create the stateful node once and reuse it in the factory.

```csharp
using Termina.Layout;

public sealed class ResultsPage
{
    private DynamicLayoutNode? _content;
    private ScrollableContainerNode? _scroll;

    public ILayoutNode BuildLayout()
    {
        _content = new DynamicLayoutNode(() =>
        {
            _scroll ??= new ScrollableContainerNode();
            return _scroll;
        });
        return _content;
    }

    public void Refresh()
        => _content?.Invalidate();
}
```

You can also invalidate a smaller child node or use `KeyedDynamicLayoutNode` when the content changes by key.

## Fix guidance

1. Create the stateful node outside the factory, or cache it with `??=` inside the factory.
2. Return the same stateful node instance on each factory call.
3. Invalidate a smaller child when only that child content changed.
4. Use `KeyedDynamicLayoutNode` when keyed content needs state preservation.

## Code-fix status

The current analyzer source defines a `DiagnosticAnalyzer`. It does not define a `CodeFixProvider`. No automatic code fix exists.

## Suppression guidance

Do not suppress this rule when a factory creates a stateful node on every run. If the factory preserves state through another mechanism that the analyzer cannot detect, suppress the diagnostic at the `Invalidate()` call and document that mechanism:

```csharp
#pragma warning disable TERMINA004
_content?.Invalidate();
#pragma warning restore TERMINA004
```

Review the state-preservation mechanism when the factory changes.
