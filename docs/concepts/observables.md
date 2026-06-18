# Observables & R3

Termina uses [R3](https://github.com/Cysharp/R3) for reactive programming. R3 is a modern reimplementation of Reactive Extensions optimized for .NET, with better performance and AOT compatibility. This page covers the basics needed to use Termina effectively.

## What is Observable?

`Observable<T>` represents a stream of values over time. Unlike `IEnumerable<T>` (pull-based), observables push values to subscribers.

```csharp
// Observable emits values over time
Observable<int> countChanges = ViewModel.Count; // ReactiveProperty IS Observable<T>

// Subscribe to receive values
countChanges.Subscribe(count => Console.WriteLine($"Count is now: {count}"));
```

## Essential Operators

### Select (Transform)

Transform each value:

```csharp
ViewModel.Count
    .Select<int, ILayoutNode>(count => new TextNode($"Count: {count}"))
    .AsLayout()
```

::: tip
R3 sometimes requires explicit type parameters on operators like `Select`. Use `Select<TIn, TOut>(...)` when the compiler can't infer the types.
:::

### OfType (Filter by Type)

Filter to specific event types. R3 requires both source and target type parameters:

```csharp
Input.OfType<IInputEvent, KeyPressed>()
    .Subscribe(key => HandleKey(key));
```

### Where (Filter)

Filter values by condition:

```csharp
ViewModel.Count
    .Where(count => count > 0)
    .Subscribe(count => UpdatePositiveDisplay(count));
```

### CombineLatest (Combine Streams)

Combine multiple streams:

```csharp
Observable.CombineLatest(
    ViewModel.UserName,
    ViewModel.IsLoggedIn,
    (name, loggedIn) => loggedIn ? $"Welcome, {name}!" : "Please log in")
    .Select<string, ILayoutNode>(msg => new TextNode(msg))
    .AsLayout()
```

## Subscription Management

### DisposeWith

The `DisposeWith` extension method adds subscriptions to a `CompositeDisposable`:

```csharp
public override void OnActivated()
{
    Input.OfType<IInputEvent, KeyPressed>()
        .Subscribe(HandleKey)
        .DisposeWith(Subscriptions);  // Auto-disposed on deactivation
}
```

### Manual Disposal

For subscriptions outside `OnActivated`:

```csharp
private IDisposable? _subscription;

public void Start()
{
    _subscription = someObservable.Subscribe(...);
}

public override void Dispose()
{
    _subscription?.Dispose();
    base.Dispose();
}
```

## ReactiveProperty

`ReactiveProperty<T>` is the primary state holder in Termina ViewModels. It combines:

1. **Holds current value** — read/write via `.Value`
2. **Is an Observable** — subscribers receive updates automatically
3. **DistinctUntilChanged** — only emits when the value actually changes

```csharp
public ReactiveProperty<int> Count { get; } = new(0);

// Read/write
Count.Value = 42;
var current = Count.Value;

// Subscribe (ReactiveProperty IS Observable<T>)
Count.Subscribe(value => Console.WriteLine(value));
```

See [Reactive Properties](/concepts/reactive-properties) for full details.

## Hot vs Cold Observables

### Hot (Termina uses these)

- Values are shared among all subscribers
- New subscribers get future values (plus current for `ReactiveProperty`)
- Examples: `Count`, `Input`

### Cold

- Each subscriber gets their own sequence
- Subscribe triggers the sequence to start
- Example: `Observable.Interval()`

## Common Patterns

### Reactive UI Binding

```csharp
ViewModel.Items
    .Select<List<Item>, ILayoutNode>(items => Layouts.Vertical(
        items.Select(i => new TextNode(i.Name)).ToArray()))
    .AsLayout()
```

When the observable can publish from a background callback or async continuation, marshal delivery through the Termina render loop:

```csharp
ViewModel.Items
    .Select<List<Item>, ILayoutNode>(items => Layouts.Vertical(
        items.Select(i => new TextNode(i.Name)).ToArray()))
    .AsLayout(RenderFrameProvider)
```

See [Render Loop Threading](/concepts/render-loop-threading) for the full guidance.

### Conditional Display

```csharp
ViewModel.IsLoading
    .Select<bool, ILayoutNode>(loading => loading
        ? new SpinnerNode()
        : new TextNode("Ready"))
    .AsLayout(RenderFrameProvider)
```

### Debounce Input

```csharp
ViewModel.SearchTerm
    .Debounce(TimeSpan.FromMilliseconds(300))
    .Subscribe(term => PerformSearch(term));
```

## Learning Resources

- [R3 GitHub](https://github.com/Cysharp/R3) - R3 documentation and API reference
- [ReactiveX Introduction](https://reactivex.io/intro.html) - Concepts and patterns (applicable to R3)
- [Rx Marbles](https://rxmarbles.com/) - Interactive operator diagrams
