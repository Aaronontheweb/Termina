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
