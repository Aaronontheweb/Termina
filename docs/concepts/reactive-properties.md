# Reactive Properties

The `[Reactive]` attribute is the foundation of Termina's reactive state management. It uses source generation to create observable properties from private fields.

## Basic Usage

```csharp
public partial class MyViewModel : ReactiveViewModel
{
    [Reactive] private int _count;
    [Reactive] private string _status = "Ready";
}
```

::: warning
Your class **must** be `partial` for the source generator to work.
:::

## What Gets Generated

For each `[Reactive]` field, the source generator creates:

```csharp
// You write:
[Reactive] private int _count;

// Generator creates:
private readonly BehaviorSubject<int> _countSubject = new(default);

public int Count
{
    get => _countSubject.Value;
    set => _countSubject.OnNext(value);
}

public IObservable<int> CountChanged => _countSubject.AsObservable();
```

## Naming Convention

The field name determines the generated property name:

| Field | Property | Observable |
|-------|----------|------------|
| `_count` | `Count` | `CountChanged` |
| `_userName` | `UserName` | `UserNameChanged` |
| `_isVisible` | `IsVisible` | `IsVisibleChanged` |

The `_` prefix is removed and the first letter is capitalized.

## Default Values

Set default values on the field:

```csharp
[Reactive] private int _count = 10;
[Reactive] private string _status = "Ready";
[Reactive] private List<string> _items = new();
```

## Using in Pages

Subscribe to the generated `*Changed` observable in your page:

```csharp
public class MyPage : ReactivePage<MyViewModel>
{
    protected override ILayoutNode BuildLayout()
    {
        return ViewModel.CountChanged
            .Select(count => new TextNode($"Count: {count}"))
            .AsLayout();
    }
}
```

## Using in ViewModel Logic

Access the current value via the property:

```csharp
private void HandleIncrement()
{
    Count++;  // Read current value, increment, set new value
}

private void HandleReset()
{
    if (Count != 0)  // Read current value
    {
        Count = 0;   // Set new value
    }
}
```

## Collection Properties

For collections, replace the entire collection to trigger updates:

```csharp
[Reactive] private List<string> _messages = new();

private void AddMessage(string msg)
{
    // Create new list with added item
    Messages = new List<string>(Messages) { msg };

    // Or using LINQ
    Messages = Messages.Append(msg).ToList();
}
```

::: tip
Mutating a collection in-place (e.g., `Messages.Add(msg)`) won't trigger an update because the reference hasn't changed. Always assign a new collection.
:::

## BehaviorSubject vs Other Observables

`[Reactive]` properties use `BehaviorSubject<T>` which:

- **Holds current value** - Subscribers immediately receive the latest value
- **Hot observable** - Emits to all subscribers
- **Replays last value** - New subscribers get the current value immediately

This is ideal for UI state because:
1. The page gets the current value when it subscribes
2. All reactive bindings stay in sync
3. You can read the current value synchronously

## Source Generator Details

See [Source Generators](/concepts/source-generators) for implementation details.
