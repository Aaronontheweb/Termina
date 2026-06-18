# Render Loop Threading

Termina runs navigation, input routing, frame callbacks, and rendering through a single application loop owned by `TerminaApplication`.

That loop is not a .NET `SynchronizationContext`. If a ViewModel awaits network or file I/O, the continuation usually resumes on a thread-pool thread. SignalR callbacks, background probes, and timer callbacks can also arrive off-loop.

## The Rule

Mutate UI-facing state on the Termina loop.

This includes `ReactiveProperty<T>.Value`, layout-node state, collection state read by `BuildLayout()` or `Render()`, and `Subject<T>.OnNext` calls that are consumed by pages or layout nodes.

## Observable Streams

Use the application render frame provider as the R3 delivery boundary:

```csharp
daemonOutput
    .ObserveOn(RenderFrameProvider)
    .Subscribe(output =>
    {
        StatusMessage.Value = "Generating...";
        UiVersion.Value++;
    })
    .DisposeWith(Subscriptions);
```

For pages, `RenderFrameProvider` is available as a protected property. For ViewModels, it is available as `RenderFrameProvider` after the ViewModel is wired to the application.

Observable layout helpers also have frame-provider overloads:

```csharp
ViewModel.StatusMessage
    .Select<string, ILayoutNode>(status => new TextNode(status))
    .AsLayout(RenderFrameProvider)
```

## One-Shot Async Results

Use `InvokeAsync` when an async operation needs to apply a final UI state change:

```csharp
var result = await LoadAsync(cancellationToken);

await InvokeAsync(() =>
{
    StatusMessage.Value = result.Message;
    IsSaved.Value = true;
}, cancellationToken);
```

Use `Post` when you do not need to await completion:

```csharp
Post(() => StatusMessage.Value = "Refresh complete.");
```

## RequestRedraw Is Not Marshaling

`RequestRedraw()` only asks the application loop to render again. It does not make previous state writes safe.

Avoid this pattern from background callbacks:

```csharp
StatusMessage.Value = "Done"; // may be off-loop
RequestRedraw();              // only schedules render
```

Use `ObserveOn(RenderFrameProvider)`, `Post`, or `InvokeAsync` instead.

## Built-In Components

Built-in animated and input components receive the app runtime context automatically when they are part of a page layout tree. Their internal timers use the app `TimeProvider` and deliver invalidation through the app `RenderFrameProvider`:

```csharp
new SpinnerNode()
new GraphNode()
new TextInputNode()
new TextAreaNode()
```

Runtime services are supplied through `LayoutRuntimeContext`, not component constructors. In app code this happens automatically when nodes are attached to a page layout tree.

Custom components can use the protected `LayoutNode.RuntimeContext` property after the node is attached to the layout tree. See [Custom Components](/advanced/custom-components#runtime-context).

## Default ObservableSystem

`ObservableSystem.DefaultFrameProvider` is process-global. If you want R3 frame operators without passing a provider each time, scope the default provider:

```csharp
using var defaults = app.SetDefaultObservableSystem();
```

Prefer explicit `RenderFrameProvider` arguments in libraries and tests to avoid process-global state leaks.

## Migration Checklist

1. Find subscriptions to daemon, SignalR, channel, timer, or probe output streams.
2. Add `.ObserveOn(RenderFrameProvider)` before subscribers that mutate UI state.
3. Replace off-loop final state writes after `await` with `InvokeAsync`.
4. Keep `AsLayout(RenderFrameProvider)` for layout bindings that may receive off-loop emissions.
5. Do not use custom `IInputSource` implementations as a loop-dispatch workaround.
6. Keep `TimeProvider` for elapsed-time behavior; use `FrameProvider` for loop-affined delivery.
