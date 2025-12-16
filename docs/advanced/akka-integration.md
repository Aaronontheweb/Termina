# Akka.NET Integration

Termina integrates seamlessly with Akka.NET for streaming data, background processing, and distributed systems.

## Overview

Akka.NET provides:
- **Actor Model** - Concurrent, isolated state management
- **Akka.Streams** - Backpressured streaming
- **Akka.Hosting** - DI integration
- **Clustering** - Distributed applications

## Setup

```bash
dotnet add package Akka.Hosting
```

```csharp
builder.Services.AddAkka("my-app", configurationBuilder =>
{
    configurationBuilder.WithActors((system, registry) =>
    {
        var myActor = system.ActorOf(MyActor.Props(), "my-actor");
        registry.Register<MyActor>(myActor);
    });
});

builder.Services.AddTermina("/", termina =>
{
    termina.RegisterRoute<MainPage, MainViewModel>("/");
});
```

## Injecting Actors

Use `IRequiredActor<T>` in ViewModels:

```csharp
public class MyViewModel : ReactiveViewModel
{
    private readonly IRequiredActor<DataActor> _dataActorProvider;
    private IActorRef? _dataActor;

    public MyViewModel(IRequiredActor<DataActor> dataActorProvider)
    {
        _dataActorProvider = dataActorProvider;
    }

    public override void OnActivated()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _dataActor = await _dataActorProvider.GetAsync();
        // Now use _dataActor
    }
}
```

## Streaming Pattern

The streaming chat demo shows the recommended pattern:

### 1. Define Messages

```csharp
public static class StreamMessages
{
    public record StartStream(string Query);

    public record StreamResponse(
        IAsyncEnumerable<IStreamToken> Tokens,
        CancellationTokenSource Cancellation);

    public interface IStreamToken { }
    public record TextChunk(string Text) : IStreamToken;
    public record StreamComplete : IStreamToken;
}
```

### 2. Create Streaming Actor

```csharp
public class StreamingActor : ReceiveActor
{
    public StreamingActor()
    {
        ReceiveAsync<StartStream>(HandleStartStream);
    }

    private async Task HandleStartStream(StartStream request)
    {
        var cts = new CancellationTokenSource();
        var stream = GenerateTokens(request.Query, cts.Token);
        Sender.Tell(new StreamResponse(stream, cts));
    }

    private async IAsyncEnumerable<IStreamToken> GenerateTokens(
        string query,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Simulate processing
        foreach (var word in GetResponse(query).Split(' '))
        {
            ct.ThrowIfCancellationRequested();
            yield return new TextChunk(word + " ");
            await Task.Delay(50, ct);
        }
        yield return new StreamComplete();
    }

    private string GetResponse(string query) => "This is a simulated response.";

    public static Props Props() => Akka.Actor.Props.Create<StreamingActor>();
}
```

### 3. Consume in ViewModel

```csharp
public partial class StreamViewModel : ReactiveViewModel
{
    public StreamingTextNode Output { get; } = StreamingTextNode.Create();

    [Reactive] private bool _isStreaming;

    private CancellationTokenSource? _streamCts;

    private async Task StartStreaming(string query)
    {
        IsStreaming = true;

        try
        {
            var response = await _actor.Ask<StreamResponse>(
                new StartStream(query),
                TimeSpan.FromSeconds(30));

            _streamCts = response.Cancellation;

            await foreach (var token in response.Tokens.WithCancellation(_streamCts.Token))
            {
                switch (token)
                {
                    case TextChunk chunk:
                        Output.Append(chunk.Text);
                        break;
                    case StreamComplete:
                        Output.AppendLine("\n[Complete]");
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Output.AppendLine("\n[Cancelled]");
        }
        finally
        {
            IsStreaming = false;
            _streamCts?.Dispose();
            _streamCts = null;
        }
    }

    private void CancelStream()
    {
        _streamCts?.Cancel();
    }
}
```

### 4. Wire Up Redraw

```csharp
public override void OnActivated()
{
    Output.ContentChanged
        .Subscribe(_ => RequestRedraw())
        .DisposeWith(Subscriptions);
}
```

## Background Processing

For long-running operations:

```csharp
public class ProcessorActor : ReceiveActor
{
    public ProcessorActor()
    {
        Receive<ProcessRequest>(req =>
        {
            // Process in background
            Context.System.Scheduler.ScheduleTellOnce(
                TimeSpan.Zero,
                Self,
                new DoWork(req),
                Self);

            Sender.Tell(new Acknowledged());
        });

        ReceiveAsync<DoWork>(async work =>
        {
            var result = await DoExpensiveWork(work);
            // Notify completion via pub-sub or direct tell
        });
    }
}
```

## Pub-Sub for Updates

Broadcast updates to ViewModels:

```csharp
// In actor
Context.System.EventStream.Publish(new DataUpdated(newData));

// In ViewModel
public override void OnActivated()
{
    var eventStream = Context.System.EventStream;
    eventStream.Subscribe<DataUpdated>(Self);
}
```

## Error Handling

Handle actor failures gracefully:

```csharp
private async Task SafeActorCall()
{
    try
    {
        var result = await _actor.Ask<Response>(
            new Request(),
            TimeSpan.FromSeconds(5));
        // Handle result
    }
    catch (AskTimeoutException)
    {
        StatusMessage = "Request timed out";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Error: {ex.Message}";
    }
}
```

## Complete Example

See the streaming chat demo for a full implementation:

::: code-group
<<< @/../demos/Termina.Demo.Streaming/Pages/StreamingChatViewModel.cs [ViewModel]
<<< @/../demos/Termina.Demo.Streaming/Actors/LlmSimulatorActor.cs [Actor]
<<< @/../demos/Termina.Demo.Streaming/Program.cs [Program]
:::
