# Termina Development Guidelines

## Architectural Principles

### Reactive-Only Pattern (R3)

**Do NOT mix R3 with .NET events.** Termina uses R3 (not System.Reactive) throughout. All asynchronous communication, state changes, and notifications must use observables.

- Use `Observable<T>` (R3) for all event-like patterns
- Use `ReactiveProperty<T>` for ViewModel state (built-in `DistinctUntilChanged`, simpler API)
- Use `Subject<T>` for event sources (fire-and-forget notifications)
- Do NOT use `BehaviorSubject<T>` — use `ReactiveProperty<T>` instead
- Do NOT use `event Action` or `event EventHandler<T>`
- Do NOT use `System.Timers.Timer` or `System.Threading.Timer` — use `Observable.Interval` with `TimeProvider`

**ViewModel state with ReactiveProperty:**
```csharp
public class CounterViewModel : ReactiveViewModel
{
    public ReactiveProperty<int> Count { get; } = new(0);
    public ReactiveProperty<string> StatusMessage { get; } = new("Ready");

    void Increment()
    {
        Count.Value++;
        StatusMessage.Value = $"Count: {Count.Value}";
    }

    public override void Dispose()
    {
        Count.Dispose();
        StatusMessage.Dispose();
        base.Dispose();
    }
}

// Page subscribes directly to ReactiveProperty (it IS an Observable<T>):
ViewModel.Count.Select<int, ILayoutNode>(count => new TextNode($"Count: {count}")).AsLayout()
```

**Event sources with Subject:**
```csharp
public Observable<IReadOnlyList<T>> SelectionConfirmed => _selectionConfirmed.AsObservable();
private readonly Subject<IReadOnlyList<T>> _selectionConfirmed = new();

// To emit:
_selectionConfirmed.OnNext(selectedItems);
```

**Testable timing with TimeProvider:**
```csharp
// Production: uses TimeProvider.System by default
public SpinnerNode(int intervalMs = 80, TimeProvider? timeProvider = null)

// Test: use FakeTimeProvider for deterministic control
var timeProvider = new FakeTimeProvider();
var spinner = new SpinnerNode(intervalMs: 80, timeProvider: timeProvider);
timeProvider.Advance(TimeSpan.FromMilliseconds(80)); // Deterministic frame advance
```

**Incorrect:**
```csharp
public event Action<IReadOnlyList<T>>? SelectionConfirmed;  // NO - don't use events
[Reactive] private int _count;  // NO - [Reactive] attribute is removed, use ReactiveProperty<T>
private readonly Timer _timer;  // NO - use Observable.Interval + TimeProvider
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

### Deterministic Testing with FakeTimeProvider

**Do NOT use `Thread.Sleep` or real timers to test time-based behavior.** Use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` for fully deterministic tests.

- All timer-based components accept `TimeProvider?` — pass `FakeTimeProvider` in tests
- Use `timeProvider.Advance(TimeSpan)` to deterministically trigger timer callbacks
- Tests are synchronous (no `async`) — no timing flakiness

**Correct:**
```csharp
[Fact]
public void AnimationChangesFrame()
{
    var timeProvider = new FakeTimeProvider();
    var spinner = new SpinnerSegment(SpinnerStyle.Line, intervalMs: 80, timeProvider: timeProvider);
    var frame1 = spinner.GetCurrentSegment().Text;

    // Deterministically advance time
    timeProvider.Advance(TimeSpan.FromMilliseconds(80));

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
    Thread.Sleep(50);  // NO - non-deterministic, flaky
}
```

## Git Workflow

### NEVER Commit Directly to Protected Branches

**Do NOT commit directly to `dev`, `main`, or `master`.** All changes must go through a feature branch and pull request — no exceptions, even for single-line fixes.

1. **Before committing**, check which branch you're on with `git branch --show-current`
2. If on `dev`, `main`, or `master`, **create a feature branch first**: `git checkout -b fix/descriptive-name` or `git checkout -b feature/descriptive-name`
3. Commit on the feature branch, push, and open a PR to `dev`
4. Never force-push to `dev`, `main`, or `master` unless explicitly correcting a prior mistake

**This applies to ALL changes** — bug fixes, refactors, docs, tests, everything.

## Release Process

### Tag Naming Convention

**Do NOT use the `v` prefix for release tags.** Tags should be numeric version only.

**Correct:** `0.2.0`, `1.0.0`, `2.1.3`

**Incorrect:** `v0.2.0`, `v1.0.0`, `v2.1.3`
