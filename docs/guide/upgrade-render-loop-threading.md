# Upgrading Render Loop Threading

Termina exposes an application-owned R3 `FrameProvider` so background, timer, and async work can deliver UI updates on the Termina render loop.

This is an additive change. Existing applications continue to compile, but applications that update UI-facing state from background callbacks should opt into the new marshaling APIs.

## Summary

| Scenario | Recommended API |
|----------|-----------------|
| Observable stream updates UI state | `.ObserveOn(RenderFrameProvider)` |
| Observable drives a layout node | `.AsLayout(RenderFrameProvider)` |
| One-shot async result updates UI state | `await InvokeAsync(...)` |
| Fire-and-forget async callback updates UI state | `Post(...)` |
| Built-in animated/input nodes use timers | pass `frameProvider: RenderFrameProvider` |

## What Changed

`TerminaApplication` now owns a render-loop `FrameProvider` and exposes it through:

```csharp
app.RenderFrameProvider
```

Pages and ViewModels receive the same provider after framework binding:

```csharp
RenderFrameProvider
```

They also receive loop-dispatch helpers:

```csharp
Post(() => StatusMessage.Value = "Done");
await InvokeAsync(() => StatusMessage.Value = "Done", cancellationToken);
```

## Why It Matters

Termina's render loop is single-threaded, but it is not a .NET `SynchronizationContext`. After `await`, callbacks often resume on the thread pool. `Observable.Interval`, SignalR, channels, subprocess output, and actor streams can also publish from non-render-loop threads.

Do not mutate UI-facing state from those callbacks directly. This includes:

- `ReactiveProperty<T>.Value`
- `Subject<T>.OnNext` consumed by pages or layout nodes
- state read by `BuildLayout()` or `Render()`
- layout-node state such as graph data, progress values, or streaming text buffers

## Observable Streams

Before:

```csharp
Observable.Interval(TimeSpan.FromSeconds(2))
    .Subscribe(_ => UpdateMetrics())
    .DisposeWith(Subscriptions);
```

After:

```csharp
Observable.Interval(TimeSpan.FromSeconds(2))
    .ObserveOn(RenderFrameProvider)
    .Subscribe(_ => UpdateMetrics())
    .DisposeWith(Subscriptions);
```

Use the same pattern for backend event streams:

```csharp
daemon.Output
    .ObserveOn(RenderFrameProvider)
    .Subscribe(line => LogLines.Value = [.. LogLines.Value, line])
    .DisposeWith(Subscriptions);
```

## Reactive Layout Bindings

If a source can publish off-loop, pass the frame provider to the layout helper:

```csharp
ViewModel.StatusMessage
    .Select<string, ILayoutNode>(status => new TextNode(status))
    .AsLayout(RenderFrameProvider)
```

The overloads are available for `AsLayout`, `AsTextLayout`, and `AsDynamicLayout`.

## Async Streams and One-Shot Results

Before:

```csharp
var result = await LoadAsync(cancellationToken);
StatusMessage.Value = result.Message;
```

After:

```csharp
var result = await LoadAsync(cancellationToken);

await InvokeAsync(() =>
{
    StatusMessage.Value = result.Message;
}, cancellationToken);
```

For streaming async data, marshal each UI mutation:

```csharp
await foreach (var token in response.TokenStream.WithCancellation(cancellationToken))
{
    await InvokeAsync(() => ChatOutput.OnNext(new AppendText(token.Text)), cancellationToken);
}
```

## Built-In Components

Pass `RenderFrameProvider` to built-in nodes that have their own timers:

```csharp
new SpinnerNode(frameProvider: RenderFrameProvider)
new GraphNode(frameProvider: RenderFrameProvider)
new TextInputNode(frameProvider: RenderFrameProvider)
new TextAreaNode(frameProvider: RenderFrameProvider)
```

Existing constructors still work. The frame provider is recommended for app UI so timer-driven invalidation is serialized with rendering.

## RequestRedraw Is Not Enough

`RequestRedraw()` schedules rendering. It does not make previous writes safe.

Avoid:

```csharp
StatusMessage.Value = "Done";
RequestRedraw();
```

Use:

```csharp
Post(() => StatusMessage.Value = "Done");
```

## Migration Checklist

1. Search for `Observable.Interval`, daemon callbacks, SignalR subscriptions, channel readers, and `Task.Run` callbacks.
2. Add `.ObserveOn(RenderFrameProvider)` before subscribers that mutate UI state.
3. Replace UI writes after `await` with `InvokeAsync`.
4. Pass `frameProvider: RenderFrameProvider` to animated or input nodes in app pages.
5. Use `AsLayout(RenderFrameProvider)` for layout bindings that may receive off-loop emissions.

See [Render Loop Threading](/concepts/render-loop-threading) for conceptual background.
