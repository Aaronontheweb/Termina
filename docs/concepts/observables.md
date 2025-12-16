# Observables & Rx

Termina uses [System.Reactive (Rx.NET)](https://github.com/dotnet/reactive) for reactive programming. This page covers the basics needed to use Termina effectively.

## What is IObservable?

`IObservable<T>` represents a stream of values over time. Unlike `IEnumerable<T>` (pull-based), observables push values to subscribers.

```csharp
// Observable emits values over time
IObservable<int> countChanges = ViewModel.CountChanged;

// Subscribe to receive values
countChanges.Subscribe(count => Console.WriteLine($"Count is now: {count}"));
```

## Essential Operators

### Select (Transform)

Transform each value:

```csharp
ViewModel.CountChanged
    .Select(count => new TextNode($"Count: {count}"))
    .AsLayout()
```

### OfType (Filter by Type)

Filter to specific event types:

```csharp
Input.OfType<KeyPressed>()
    .Subscribe(key => HandleKey(key));
```

### Where (Filter)

Filter values by condition:

```csharp
ViewModel.CountChanged
    .Where(count => count > 0)
    .Subscribe(count => UpdatePositiveDisplay(count));
```

### CombineLatest (Combine Streams)

Combine multiple streams:

```csharp
Observable.CombineLatest(
    ViewModel.UserNameChanged,
    ViewModel.IsLoggedInChanged,
    (name, loggedIn) => loggedIn ? $"Welcome, {name}!" : "Please log in")
    .Select(msg => new TextNode(msg))
    .AsLayout()
```

### StartWith (Initial Value)

Provide an initial value:

```csharp
ViewModel.StatusChanged
    .StartWith("Ready")
    .Select(s => new TextNode(s))
    .AsLayout()
```

## Subscription Management

### DisposeWith

The `DisposeWith` extension method adds subscriptions to a `CompositeDisposable`:

```csharp
public override void OnActivated()
{
    Input.OfType<KeyPressed>()
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

## BehaviorSubject

`[Reactive]` properties use `BehaviorSubject<T>`, which:

1. **Holds current value** - Can be read synchronously
2. **Emits on subscribe** - New subscribers immediately get current value
3. **Emits on change** - Existing subscribers get updates

```csharp
// Generated from [Reactive] private int _count;
private BehaviorSubject<int> _countSubject = new(default);

public int Count
{
    get => _countSubject.Value;           // Read current value
    set => _countSubject.OnNext(value);   // Emit new value
}

public IObservable<int> CountChanged => _countSubject.AsObservable();
```

## Hot vs Cold Observables

### Hot (Termina uses these)

- Values are shared among all subscribers
- New subscribers get future values (plus current for BehaviorSubject)
- Examples: `CountChanged`, `Input`

### Cold

- Each subscriber gets their own sequence
- Subscribe triggers the sequence to start
- Example: `Observable.Interval()`

## Common Patterns

### Reactive UI Binding

```csharp
ViewModel.ItemsChanged
    .Select(items => Layouts.Vertical(
        items.Select(i => new TextNode(i.Name)).ToArray()))
    .AsLayout()
```

### Conditional Display

```csharp
ViewModel.IsLoadingChanged
    .Select(loading => loading
        ? (ILayoutNode)new SpinnerNode()
        : new TextNode("Ready"))
    .AsLayout()
```

### Debounce Input

```csharp
ViewModel.SearchTermChanged
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Subscribe(term => PerformSearch(term));
```

## Learning Resources

- [ReactiveX Introduction](https://reactivex.io/intro.html) - Concepts and patterns
- [Rx Marbles](https://rxmarbles.com/) - Interactive operator diagrams
- [System.Reactive GitHub](https://github.com/dotnet/reactive) - Official documentation
- [Introduction to Rx](http://introtorx.com/) - Free online book
