# Streaming Chat Tutorial

Build a chat interface with streaming responses, simulating an LLM conversation using Akka.NET.

## What You'll Build

A streaming chat application with:
- Real-time streaming text responses
- Text input with prompt history
- "Thinking" indicator during processing
- Cancellation support
- Akka.NET actor integration

![Streaming chat demo: a response streaming in token-by-token, then an interactive decision list](/gallery/streaming-chat.gif)

*The finished streaming chat app.*

## Prerequisites

This is an advanced tutorial. You should understand:
- [Reactive properties](/concepts/reactive-properties)
- [Input handling](/concepts/input-handling)
- Basic [Akka.NET](https://getakka.net/) concepts

## Project Setup

```bash
dotnet new console -n StreamingChatDemo
cd StreamingChatDemo
dotnet add package Termina
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Akka.Hosting
```

## Step 1: Create the ViewModel

The ViewModel manages chat state and coordinates with Akka actors.

::: details View complete StreamingChatViewModel.cs
<<< @/../demos/Termina.Demo.Streaming/Pages/StreamingChatViewModel.cs{csharp}
:::

### Key Points

**Streaming Output via Observables**

The ViewModel exposes observables for chat content that the Page subscribes to:

```csharp
// ViewModel exposes output streams
public IObservable<ChatTextSegment> ChatOutput => _chatOutput.AsObservable();
public IObservable<ChatTextSegment> ThinkingOutput => _thinkingOutput.AsObservable();
public IObservable<Unit> ClearThinking => _clearThinking.AsObservable();

// Emit chat content
_chatOutput.OnNext(new ChatTextSegment("Hello!", Color.White, IsNewLine: true));
```

**Text Input Coordination**

The ViewModel exposes an observable for prompt text changes (for history navigation):

```csharp
public IObservable<string> PromptTextChanged => _promptTextChanged.AsObservable();

// Navigate history
_promptTextChanged.OnNext(_promptHistory[_historyIndex]);
```

**Async Stream Consumption**

```csharp
await foreach (var token in response.TokenStream.WithCancellation(cts.Token))
{
    switch (token)
    {
        case TextChunk chunk:
            ChatHistory.Append(chunk.Text);
            break;
    }
}
```

Use `IAsyncEnumerable` to process streaming data.

## Step 2: Create the Page

The Page renders the chat interface.

::: details View complete StreamingChatPage.cs
<<< @/../demos/Termina.Demo.Streaming/Pages/StreamingChatPage.cs{csharp}
:::

### Key Points

**Page Owns Layout Nodes**

The Page creates and owns interactive layout nodes:

```csharp
private StreamingTextNode _chatHistory = null!;
private TextInputNode _promptInput = null!;

protected override void OnBound()
{
    _chatHistory = StreamingTextNode.Create().WithPrefix("  ", Color.Gray);
    _promptInput = new TextInputNode().WithPlaceholder("Enter your question...");

    // Subscribe to ViewModel's output streams
    ViewModel.ChatOutput.Subscribe(segment => _chatHistory.Append(segment.Text));
}
```

**Input Routing via ViewModel.Input**

The Page accesses `ViewModel.Input` to route input to interactive layout nodes:

```csharp
// Page subscribes to ViewModel's input for routing to layout nodes
ViewModel.Input.OfType<KeyPressed>()
    .Subscribe(HandleKeyPress)
    .DisposeWith(Subscriptions);

private void HandleKeyPress(KeyPressed key)
{
    // Route to scrollable chat history
    if (_chatHistory.HandleInput(key.KeyInfo, viewportHeight: 10, viewportWidth: 80))
        return;

    // Route to text input
    _promptInput.HandleInput(key.KeyInfo);
}
```

**Conditional Panels**

```csharp
ViewModel.IsGeneratingChanged
    .Select(isGenerating => isGenerating && _thinkingIndicator.Buffer.HasContent
        ? BuildThinkingPanel()
        : new EmptyNode())
    .AsLayout()
```

Show/hide panels based on state.

## Step 3: Create the Actor

The LLM simulator actor produces streaming tokens.

::: details View complete LlmSimulatorActor.cs
<<< @/../demos/Termina.Demo.Streaming/Actors/LlmSimulatorActor.cs{csharp}
:::

### Key Points

**IAsyncEnumerable Response**

```csharp
public record GenerateResponse(
    IAsyncEnumerable<IStreamToken> TokenStream,
    CancellationTokenSource Cancellation);
```

Return an async enumerable that the ViewModel can consume.

**Simulated Delays**

```csharp
async IAsyncEnumerable<IStreamToken> GenerateTokens(string prompt, CancellationToken ct)
{
    // Thinking phase
    yield return new ThinkingToken("Analyzing...");
    await Task.Delay(500, ct);

    // Token generation
    foreach (var word in response.Split(' '))
    {
        yield return new TextChunk(word + " ");
        await Task.Delay(50, ct);
    }
}
```

## Step 4: Wire Up the Host

::: details View complete Program.cs
<<< @/../demos/Termina.Demo.Streaming/Program.cs{csharp}
:::

### Key Points

**Akka.NET Registration**

```csharp
builder.Services.AddAkka("streaming-demo", configurationBuilder =>
{
    configurationBuilder.WithActors((system, registry) =>
    {
        var llmActor = system.ActorOf(LlmSimulatorActor.Props(), "llm-simulator");
        registry.Register<LlmSimulatorActor>(llmActor);
    });
});
```

**Dependency Injection**

The ViewModel receives the actor via DI:

```csharp
public StreamingChatViewModel(IRequiredActor<LlmSimulatorActor> llmActorProvider)
{
    _llmActorProvider = llmActorProvider;
}
```

## Run the App

```bash
dotnet run
```

Controls:
- Type your question and press `Enter`
- `↑` / `↓` - Navigate prompt history
- `PgUp` / `PgDn` - Scroll chat history
- `Escape` - Cancel generation or quit
- `Ctrl+Q` - Force quit

## Patterns Demonstrated

### Streaming Text Updates

```csharp
// Append character by character
ChatHistory.Append(chunk.Text);

// Append complete lines
ChatHistory.AppendLine("Complete message");

// Clear content
ThinkingIndicator.Clear();
```

### Cancellation

```csharp
private CancellationTokenSource? _generationCts;

private void CancelGeneration()
{
    _generationCts?.Cancel();
    CleanupGeneration(" [cancelled]");
}
```

### Page-ViewModel Communication

```csharp
// Page subscribes to TextInputNode events and calls ViewModel methods
_promptInput.Submitted
    .Subscribe(text => ViewModel.HandleSubmit(text))
    .DisposeWith(Subscriptions);

// Page routes input to layout nodes via ViewModel.Input
ViewModel.Input.OfType<KeyPressed>()
    .Subscribe(key => _chatHistory.HandleInput(key.KeyInfo, 10, 80))
    .DisposeWith(Subscriptions);
```

### State-Based UI Updates

```csharp
IsGenerating = true;
StatusMessage = "Generating...";

// ... async work ...

IsGenerating = false;
StatusMessage = "Ready";
```

## Complete Code

::: code-group
<<< @/../demos/Termina.Demo.Streaming/Pages/StreamingChatViewModel.cs [ViewModel]
<<< @/../demos/Termina.Demo.Streaming/Pages/StreamingChatPage.cs [Page]
<<< @/../demos/Termina.Demo.Streaming/Program.cs [Program]
<<< @/../demos/Termina.Demo.Streaming/Actors/LlmSimulatorActor.cs [Actor]
:::

## Next Steps

- Learn about [testing](/advanced/testing) with `VirtualInputSource`
- Explore [custom components](/advanced/custom-components)
- See [Akka.NET integration patterns](/advanced/akka-integration)
