# Migrating to Termina 0.7.0

Termina 0.7.0 introduces breaking changes as part of the migration from System.Reactive to [R3](https://github.com/Cysharp/R3) and the removal of the `[Reactive]` source generator in favor of explicit `ReactiveProperty<T>`. This guide covers everything you need to update.

## Overview of Changes

| Area | Before (0.6.x) | After (0.7.0) |
|------|----------------|----------------|
| Reactive framework | System.Reactive (Rx.NET) | R3 |
| ViewModel state | `[Reactive] private int _count;` | `ReactiveProperty<int> Count { get; } = new(0);` |
| Observable type | `IObservable<T>` | `Observable<T>` |
| Property access | `Count++` | `Count.Value++` |
| Page binding | `CountChanged.Select(...)` | `Count.Select(...)` |
| Backing store | `BehaviorSubject<T>` | `ReactiveProperty<T>` |
| Class declaration | `public partial class MyVM` | `public class MyVM` (no `partial` needed) |

## 1. Package Changes

Remove System.Reactive and add R3:

```xml
<!-- Before -->
<PackageReference Include="System.Reactive" Version="6.x" />

<!-- After -->
<PackageReference Include="R3" Version="1.3.0" />
```

::: tip
Termina 0.7.0 brings R3 as a transitive dependency, so you may not need to add it explicitly unless you use R3 operators directly.
:::

## 2. Import Changes

Replace System.Reactive imports with R3:

```csharp
// Before
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

// After
using R3;
```

## 3. ViewModel Migration: [Reactive] → ReactiveProperty\<T\>

The `[Reactive]` source generator has been removed. Use `ReactiveProperty<T>` directly.

### Before

```csharp
public partial class CounterViewModel : ReactiveViewModel
{
    [Reactive] private int _count;
    [Reactive] private string _status = "Ready";

    private void Increment()
    {
        Count++;
        Status = $"Count: {Count}";
    }
}
```

### After

```csharp
public class CounterViewModel : ReactiveViewModel
{
    public ReactiveProperty<int> Count { get; } = new(0);
    public ReactiveProperty<string> Status { get; } = new("Ready");

    private void Increment()
    {
        Count.Value++;
        Status.Value = $"Count: {Count.Value}";
    }

    public override void Dispose()
    {
        Count.Dispose();
        Status.Dispose();
        base.Dispose();
    }
}
```

### Key differences

| Aspect | `[Reactive]` (old) | `ReactiveProperty<T>` (new) |
|--------|-------------------|----------------------------|
| Declaration | `[Reactive] private int _count;` | `public ReactiveProperty<int> Count { get; } = new(0);` |
| Read value | `Count` | `Count.Value` |
| Write value | `Count = 5` | `Count.Value = 5` |
| Subscribe | `CountChanged.Select(...)` | `Count.Select(...)` |
| Class modifier | `partial` required | Not required |
| Dispose | Auto-generated | Manual `Dispose()` override required |
| DistinctUntilChanged | Not built-in | Built-in (won't emit duplicate values) |

::: warning Important
You **must** dispose all `ReactiveProperty<T>` instances. Add a `Dispose()` override to your ViewModel that disposes each property before calling `base.Dispose()`.
:::

## 4. Property Access Changes

Every direct property read/write must go through `.Value`:

```csharp
// Before
Count++;
Status = "Done";
if (Count > 10) { ... }

// After
Count.Value++;
Status.Value = "Done";
if (Count.Value > 10) { ... }
```

## 5. Page Subscription Changes

`ReactiveProperty<T>` is itself an `Observable<T>`, so you subscribe directly to the property instead of a separate `*Changed` observable:

```csharp
// Before
ViewModel.CountChanged
    .Select(count => new TextNode($"Count: {count}"))
    .AsLayout()

// After
ViewModel.Count
    .Select<int, ILayoutNode>(count => new TextNode($"Count: {count}"))
    .AsLayout()
```

::: tip
Note the explicit type parameters on `Select<int, ILayoutNode>`. R3 sometimes needs explicit type parameters where System.Reactive could infer them.
:::

## 6. Observable Operator Changes

### OfType

R3's `OfType` requires both source and target type parameters:

```csharp
// Before
Input.OfType<KeyPressed>()

// After
Input.OfType<IInputEvent, KeyPressed>()
```

### Select

R3's `Select` may need explicit type parameters when inference fails:

```csharp
// Before
observable.Select(x => new TextNode($"{x}")).AsLayout()

// After
observable.Select<int, ILayoutNode>(x => new TextNode($"{x}")).AsLayout()
```

### CombineLatest

```csharp
// Before
Observable.CombineLatest(
    ViewModel.UserNameChanged,
    ViewModel.IsLoggedInChanged,
    (name, loggedIn) => ...)

// After
Observable.CombineLatest(
    ViewModel.UserName,
    ViewModel.IsLoggedIn,
    (name, loggedIn) => ...)
```

## 7. StartWith → Removal or Prepend

`StartWith` does not exist in R3. The replacement depends on the source type:

### ReactiveProperty\<T\>: Remove StartWith entirely

`ReactiveProperty<T>` already emits its current value to new subscribers, so `StartWith` is unnecessary:

```csharp
// Before
ViewModel.StateChanged
    .StartWith(ViewModel.State)
    .Select(state => BuildLayout(state))
    .AsLayout()

// After — ReactiveProperty replays current value automatically
ViewModel.State
    .Select(state => BuildLayout(state))
    .AsLayout()
```

### Subject\<T\>: Use Prepend

For `Subject<T>` (which does not replay), use `Prepend` to emit an initial value:

```csharp
// Before
_selectionChanged.StartWith((ILayoutNode?)null)

// After
_selectionChanged.Prepend((ILayoutNode?)null)
```

## 8. BehaviorSubject → ReactiveProperty

If you used `BehaviorSubject<T>` directly (outside of `[Reactive]`), migrate to `ReactiveProperty<T>`:

```csharp
// Before
private readonly BehaviorSubject<string> _status = new("Ready");
public IObservable<string> Status => _status.AsObservable();
public string CurrentStatus => _status.Value;

// Update:
_status.OnNext("Processing");

// After
public ReactiveProperty<string> Status { get; } = new("Ready");

// Update:
Status.Value = "Processing";

// ReactiveProperty IS Observable<T>, no wrapper needed
```

`ReactiveProperty<T>` replaces `BehaviorSubject<T>` for all state that needs both read access and subscriptions.

## 9. Subject\<T\> Migration

`Subject<T>` exists in both System.Reactive and R3. Just change the import:

```csharp
// Before
using System.Reactive.Subjects;
private readonly Subject<Unit> _shutdown = new();
public IObservable<Unit> ShutdownRequested => _shutdown.AsObservable();

// After
using R3;
private readonly Subject<Unit> _shutdown = new();
public Observable<Unit> ShutdownRequested => _shutdown;
```

Note: In R3, `Subject<T>` is already `Observable<T>`, so `.AsObservable()` is not needed (though it still works).

## 10. Subscribe Callback Changes

R3 changes the signature of `Subscribe` callbacks:

```csharp
// Before (System.Reactive)
observable.Subscribe(
    onNext: value => { ... },
    onError: error => { ... },
    onCompleted: () => { ... });

// After (R3)
observable.Subscribe(
    onNext: value => { ... },
    onErrorResume: error => { ... },  // renamed
    onCompleted: result => { ... });  // takes Result parameter
```

For simple subscriptions with just `onNext`, the API is the same:

```csharp
// Works in both
observable.Subscribe(value => HandleValue(value));
```

::: danger Two-Argument Subscribe Trap
The two-argument `Subscribe(onNext, second)` changed semantics silently. In System.Reactive the second argument was `onError`. In R3 the second argument is `onCompleted` (taking `Result`, not `Exception`):

```csharp
// System.Reactive — second arg is onError
observable.Subscribe(_ => { }, ex => Log(ex));

// R3 — second arg is onCompleted (takes Result, not Exception)
// To capture errors, use the three-argument form:
observable.Subscribe(_ => { }, ex => Log(ex), _ => { });
```

This won't just fail to compile — it will give a confusing `cannot convert from 'R3.Result' to 'System.Exception'` error. Always use the three-argument form when you need error handling.
:::

## 11. Timer Changes

Replace `System.Timers.Timer` and `System.Threading.Timer` with `Observable.Interval` and `TimeProvider`:

```csharp
// Before
private readonly Timer _timer = new(80);
_timer.Elapsed += OnTick;
_timer.Start();

// After
Observable.Interval(TimeSpan.FromMilliseconds(80), timeProvider ?? TimeProvider.System)
    .Subscribe(_ => OnTick())
    .DisposeWith(Subscriptions);
```

Components that use timers now accept an optional `TimeProvider` parameter for testable timing. Use `FakeTimeProvider` in tests for deterministic behavior.

## 12. Custom Node Render Methods

If you have custom `LayoutNode` subclasses, update `Render` methods to use `CreateSubContext`:

```csharp
// Before (may render at wrong coordinates)
public override void Render(IRenderContext context, Rect bounds)
{
    context.DrawText(0, 0, "Hello");
}

// After (correct coordinate translation)
public override void Render(IRenderContext context, Rect bounds)
{
    var sub = context.CreateSubContext(bounds);
    sub.DrawText(0, 0, "Hello");
}
```

This ensures content renders at the correct position within the layout tree.

## Migration Checklist

- [ ] Replace `System.Reactive` package with `R3`
- [ ] Update all `using` statements: `System.Reactive.*` → `R3`
- [ ] Convert `[Reactive]` fields to `ReactiveProperty<T>` properties
- [ ] Remove `partial` modifier from ViewModel classes (if only needed for `[Reactive]`)
- [ ] Update all property access: `Prop` → `Prop.Value`
- [ ] Update page bindings: `PropChanged` → `Prop`
- [ ] Remove `.StartWith()` on `ReactiveProperty<T>` (replays automatically); use `.Prepend()` on `Subject<T>`
- [ ] Add explicit type params to `OfType<TSrc, TDest>()` and `Select<TIn, TOut>()`
- [ ] Convert `BehaviorSubject<T>` to `ReactiveProperty<T>`
- [ ] Update `Subject<T>` imports and exposed types (`IObservable<T>` → `Observable<T>`)
- [ ] Update `Subscribe` callbacks: `onError` → `onErrorResume`, `onCompleted` takes `Result`
- [ ] Add `Dispose()` overrides disposing all `ReactiveProperty<T>` instances
- [ ] Replace timers with `Observable.Interval` + `TimeProvider`
- [ ] Update custom `LayoutNode.Render()` methods to use `CreateSubContext`
- [ ] Verify build compiles cleanly
- [ ] Run all tests
