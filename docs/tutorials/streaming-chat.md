# Streaming Chat Tutorial

Build a chat interface with streaming responses, simulating an LLM conversation using Akka.NET.

## What You'll Build

A streaming chat application with:
- Real-time streaming text responses
- Text input with prompt history
- "Thinking" indicator during processing
- Cancellation support
- Akka.NET actor integration

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

**Streaming Components**

```csharp
public StreamingTextNode ChatHistory { get; } = StreamingTextNode.Create()
    .WithPrefix("  ", Color.Gray);

public StreamingTextNode ThinkingIndicator { get; } = StreamingTextNode.CreateWindowed(3)
    .WithPrefix("💭 ", Color.Yellow);
```

`StreamingTextNode` handles character-by-character updates efficiently:
- `Create()` - Full scrolling buffer
- `CreateWindowed(n)` - Rolling window of last n lines

**Text Input Component**

```csharp
public TextInputNode PromptInput { get; } = new TextInputNode()
    .WithPlaceholder("Enter your question...")
    .WithForeground(Color.Cyan);

// Wire up submit observable
PromptInput.Submitted
    .Subscribe(HandleSubmit)
    .DisposeWith(Subscriptions);
```

`TextInputNode` provides full text editing with cursor, selection, and history.

**Content Change Notifications**

```csharp
public override void OnActivated()
{
    ChatHistory.ContentChanged
        .Subscribe(_ => RequestRedraw())
        .DisposeWith(Subscriptions);
}
```

Subscribe to `ContentChanged` and call `RequestRedraw()` to trigger UI updates for streaming content.

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

**Shared Components**

```csharp
.WithChild(
    new PanelNode()
        .WithContent(ViewModel.ChatHistory)  // Shared instance
        .Fill())
```

Pass component instances directly - they manage their own state.

**Conditional Panels**

```csharp
ViewModel.IsGeneratingChanged
    .Select(isGenerating => isGenerating && ViewModel.ThinkingIndicator.HasContent
        ? BuildThinkingPanel()
        : new EmptyNode())
    .AsLayout()
```

Show/hide panels based on state.

**Dynamic Status Bar**

```csharp
ViewModel.IsGeneratingChanged
    .Select(isGenerating => new TextNode(
        isGenerating
            ? "[Esc] Cancel [PgUp/PgDn] Scroll"
            : "[Enter] Send [↑/↓] History [Esc] Quit"))
    .AsLayout()
```

Change help text based on current state.

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

### Input Component Observables

```csharp
PromptInput.Submitted
    .Subscribe(HandleSubmit)
    .DisposeWith(Subscriptions);

// In input handling:
if (ChatHistory.HandleInput(keyInfo, viewportHeight: 10, viewportWidth: 80))
{
    return;  // Component handled the input
}
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
