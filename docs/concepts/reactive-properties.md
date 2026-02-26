# Reactive Properties

`ReactiveProperty<T>` is the foundation of Termina's reactive state management. It provides a value holder that is also an `Observable<T>`, enabling automatic UI updates when state changes.

## Basic Usage

```csharp
public class MyViewModel : ReactiveViewModel
{
    public ReactiveProperty<int> Count { get; } = new(0);
    public ReactiveProperty<string> Status { get; } = new("Ready");

    public override void Dispose()
    {
        Count.Dispose();
        Status.Dispose();
        base.Dispose();
    }
}
```

::: warning
You **must** dispose all `ReactiveProperty<T>` instances in your ViewModel's `Dispose()` method.
:::

## How It Works

`ReactiveProperty<T>` combines three capabilities:

1. **Holds current value** — read/write via `.Value`
2. **Is an Observable** — subscribers receive updates when `.Value` changes
3. **DistinctUntilChanged** — only emits when the value actually changes (built-in)

```csharp
// Declare in ViewModel
public ReactiveProperty<int> Count { get; } = new(0);

// Read/write the value
Count.Value++;
var current = Count.Value;

// Subscribe in a Page (ReactiveProperty IS Observable<T>)
Count.Select<int, ILayoutNode>(c => new TextNode($"Count: {c}")).AsLayout()
```

## Naming Convention

Properties follow standard C# naming:

| Declaration | Read/Write | Subscribe |
|------------|-----------|-----------|
| `ReactiveProperty<int> Count` | `Count.Value` | `Count.Select(...)` |
| `ReactiveProperty<string> UserName` | `UserName.Value` | `UserName.Select(...)` |
| `ReactiveProperty<bool> IsVisible` | `IsVisible.Value` | `IsVisible.Select(...)` |

Since `ReactiveProperty<T>` is itself an `Observable<T>`, you subscribe directly to the property — there is no separate `*Changed` observable.

## Default Values

Set default values in the constructor:

```csharp
public ReactiveProperty<int> Count { get; } = new(10);
public ReactiveProperty<string> Status { get; } = new("Ready");
public ReactiveProperty<List<string>> Items { get; } = new(new());
```

## Using in Pages

Subscribe directly to the `ReactiveProperty` in your page layout:

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    public override ILayoutNode BuildLayout()
    {
        return ViewModel.Count
            .Select<int, ILayoutNode>(count => new TextNode($"Count: {count}"))
            .AsLayout();
    }
}
```

::: tip
R3 sometimes requires explicit type parameters on operators like `Select`. Use `Select<TIn, TOut>(...)` when the compiler can't infer the types.
:::

## Using in ViewModel Logic

Access the current value via `.Value`:

```csharp
private void HandleIncrement()
{
    Count.Value++;  // Read, increment, write
}

private void HandleReset()
{
    if (Count.Value != 0)  // Read current value
    {
        Count.Value = 0;   // Set new value
    }
}
```

## Collection Properties

For collections, replace the entire collection to trigger updates:

```csharp
public ReactiveProperty<List<string>> Messages { get; } = new(new());

private void AddMessage(string msg)
{
    // Create new list with added item
    Messages.Value = new List<string>(Messages.Value) { msg };

    // Or using LINQ
    Messages.Value = Messages.Value.Append(msg).ToList();
}
```

::: tip
Mutating a collection in-place (e.g., `Messages.Value.Add(msg)`) won't trigger an update because the reference hasn't changed. Always assign a new collection.
:::

## ReactiveProperty vs Subject

Use `ReactiveProperty<T>` for **state** (has a current value, subscribers need the latest):

```csharp
// State — use ReactiveProperty<T>
public ReactiveProperty<string> Status { get; } = new("Ready");
```

Use `Subject<T>` for **events** (fire-and-forget notifications, no "current value"):

```csharp
// Events — use Subject<T>
private readonly Subject<Unit> _shutdown = new();
public Observable<Unit> ShutdownRequested => _shutdown;
```

## Disposal

::: tip
For page-level state that doesn't need to be in the ViewModel, see [Dynamic Layouts](../concepts/dynamic-layouts.md). `Layouts.Dynamic()` lets you drive UI from imperative local state without creating an observable pipeline.
:::

All `ReactiveProperty<T>` instances must be disposed to prevent leaks:

```csharp
public class MyViewModel : ReactiveViewModel
{
    public ReactiveProperty<int> Count { get; } = new(0);
    public ReactiveProperty<string> Status { get; } = new("Ready");

    public override void Dispose()
    {
        Count.Dispose();
        Status.Dispose();
        base.Dispose();
    }
}
```
