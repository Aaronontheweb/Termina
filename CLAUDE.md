# Termina Development Guidelines

## Architectural Principles

### Reactive-Only Pattern

**Do NOT mix System.Reactive with .NET events.** Termina uses System.Reactive (Rx) throughout. All asynchronous communication, state changes, and notifications must use observables.

- Use `IObservable<T>` for all event-like patterns
- Use `Subject<T>`, `BehaviorSubject<T>`, or `ReplaySubject<T>` for event sources
- Do NOT use `event Action` or `event EventHandler<T>`
- The only exception is the existing `IInvalidatingNode.Invalidated` event (legacy - consider migrating)

**Correct:**
```csharp
public IObservable<IReadOnlyList<T>> SelectionConfirmed => _selectionConfirmed.AsObservable();
private readonly Subject<IReadOnlyList<T>> _selectionConfirmed = new();

// To emit:
_selectionConfirmed.OnNext(selectedItems);
```

**Incorrect:**
```csharp
public event Action<IReadOnlyList<T>>? SelectionConfirmed;  // NO - don't use events
```

### Fluent API Pattern

All layout nodes use fluent builder pattern with `With*` methods that return `this`.

### Constraint-Based Layout

Use `SizeConstraint` (Fixed, Fill, Auto, Percent) for sizing rather than hardcoded values.

### ITextSegment as Universal Text Type

**Any API that accepts text should accept `ITextSegment`, not raw `string`.** This enables composition of multiple styled segments (different colors, decorations) within a single text element.

- `ITextSegment` is the universal currency for styled text
- `StyledSegment` provides text + style (foreground, background, decoration)
- `CompositeTextSegment` combines multiple segments with different styles
- Convenience overloads accepting `string` are acceptable but should convert to `ITextSegment` internally

**Correct:**
```csharp
public TextNode WithContent(ITextSegment segment)
{
    _segment = segment;
    return this;
}

// Convenience overload
public TextNode WithContent(string text) => WithContent(new StaticTextSegment(text));
```

**Why this matters:**
```csharp
// A table cell with mixed styling
var cell = new CompositeTextSegment(
    new StaticTextSegment("Status: "),
    new StaticTextSegment("Online", new TextStyle(Foreground: Color.Green, Bold: true))
);
tableNode.SetCell(0, 1, new TextNode().WithContent(cell));
```

This principle ensures all text-accepting components (TextNode, table cells, status bars, etc.) can display rich, multi-styled content without requiring specialized node types.

## Testing Guidelines

### Deterministic Observable Testing

**Do NOT use `Thread.Sleep` to test time-based or event-driven behavior.** Instead, await the actual observable events using Rx operators.

- Use `FirstAsync()` to await the first emission from an observable
- Use `Timeout()` to prevent tests from hanging if events don't fire
- Tests should observe **actual state changes**, not assume timing

**Correct:**
```csharp
[Fact]
public async Task AnimationChangesFrame()
{
    var spinner = new SpinnerSegment(SpinnerStyle.Line, intervalMs: 10);
    var frame1 = spinner.GetCurrentSegment().Text;

    // Wait for actual invalidation event
    await spinner.Invalidated
        .FirstAsync()
        .Timeout(TimeSpan.FromSeconds(1));

    var frame2 = spinner.GetCurrentSegment().Text;
    Assert.NotEqual(frame1, frame2);
}
```

**Incorrect:**
```csharp
[Fact]
public void AnimationChangesFrame()
{
    var spinner = new SpinnerSegment(SpinnerStyle.Line, intervalMs: 10);
    var frame1 = spinner.GetCurrentSegment().Text;

    Thread.Sleep(50);  // NO - non-deterministic, flaky on Windows

    var frame2 = spinner.GetCurrentSegment().Text;
    Assert.NotEqual(frame1, frame2);
}
```

## Release Process

### Tag Naming Convention

**Do NOT use the `v` prefix for release tags.** Tags should be numeric version only.

**Correct:** `0.2.0`, `1.0.0`, `2.1.3`

**Incorrect:** `v0.2.0`, `v1.0.0`, `v2.1.3`
