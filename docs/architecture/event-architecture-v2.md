# Termina Event Architecture v2 - Duplex Event Loop

## Decision

We're going with **Option A: Pure Duplex Event Loop** rather than MVVM/MVC with injected services.

### Why Option A?

1. **Impossible to block render loop** - Handlers are pure event transformers, no async/await
2. **No shared mutable state** - Handlers are stateless; state lives in Model layer (actors, services)
3. **Can't footgun yourself** - No way to accidentally call `.Result` or `.Wait()` because there's nothing to await
4. **Concurrent by design** - Multiple backend sources can publish events simultaneously, channel serializes them
5. **Familiar to actor-model users** - Natural fit for Akka.NET integration

Option B (DI + convention) was rejected because:
- Can't prevent developers from adding state fields to handlers
- Analyzers can catch blocking calls but can't catch state mutation races
- Invites familiar .NET patterns that don't work well in concurrent TUI

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         MODEL LAYER                                      │
│  (Actors, Services - own ALL state and async work)                      │
│                                                                          │
│  - Subscribes to domain events from the application bus                 │
│  - Publishes domain events when state changes                           │
│  - Has ZERO knowledge of UI                                             │
│                                                                          │
│  Example: LlmActor receives SubmitPrompt, publishes PromptCompleted     │
│                                                                          │
└────────────────────────────┬────────────────────────────────────────────┘
                             │ IModelEvent (domain events)
                             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       TERMINA APPLICATION                                │
│  (Top-level orchestrator - the "app host")                              │
│                                                                          │
│  Responsibilities:                                                       │
│  - Owns the event bus / channel infrastructure                          │
│  - Routes model events → appropriate handler(s)                         │
│  - Routes UI events → current page's handler                            │
│  - Manages page registration and navigation                             │
│  - Dead letter logging for unhandled events                             │
│  - Controls which page is "active" and can render                       │
│                                                                          │
└───────────┬─────────────────────────────────────────┬───────────────────┘
            │                                         │
     TModelEvent                               TUIEvent
     (from backend)                           (from frontend)
            │                                         │
            ▼                                         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         PAGE HANDLER                                     │
│  (Stateless event transformer - NO state, NO injected services)         │
│                                                                          │
│  HandleFrontend(TUIEvent):                                              │
│    - Receives typed UI events (user interactions)                       │
│    - May call Send(TCommand) to update UI                               │
│    - May call Publish(IModelEvent) to trigger backend work              │
│                                                                          │
│  HandleBackend(TModelEvent):                                            │
│    - Receives typed model events (backend state changes)                │
│    - Calls Send(TCommand) to update UI                                  │
│                                                                          │
│  Available methods:                                                      │
│    - Send(TCommand) → push command to page (triggers re-render)         │
│    - Publish(IModelEvent) → push event to application bus               │
│    - Navigate(pageKey) → request navigation                             │
│    - Shutdown() → request application shutdown                          │
│                                                                          │
└──────────────────────────┬──────────────────────────────────────────────┘
                           │ TCommand
                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              PAGE                                        │
│  (View layer - components and rendering)                                │
│                                                                          │
│  MapToUIEvent(IInputEvent raw) → TUIEvent?                              │
│    - REQUIRED: Transform raw input into typed domain UI events          │
│    - Return null to swallow/ignore events                               │
│    - Forces developer to think about what each input MEANS              │
│                                                                          │
│  ApplyCommand(TCommand):                                                │
│    - REQUIRED: Apply commands to update component state                 │
│    - Called by framework when handler calls Send()                      │
│                                                                          │
│  Components: SelectList, TextInput, etc.                                │
│    - Hold their own UI state (selected index, text value)               │
│    - Render themselves                                                   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Type Parameters

Each page requires four type definitions:

```csharp
// 1. UI Events - user interactions, transformed from raw input
public abstract record ChatUIEvent
{
    public sealed record MessageSubmitted(string Text) : ChatUIEvent;
    public sealed record CancelRequested() : ChatUIEvent;
}

// 2. UI Commands - handler → page updates
public abstract record ChatCommand
{
    public sealed record ShowMessage(string Role, string Text) : ChatCommand;
    public sealed record ShowTypingIndicator() : ChatCommand;
    public sealed record HideTypingIndicator() : ChatCommand;
    public sealed record ClearInput() : ChatCommand;
}

// 3. Model Events - backend → handler notifications
public abstract record ChatModelEvent : IModelEvent
{
    public sealed record PromptCompleted(string Response) : ChatModelEvent;
    public sealed record StreamChunkReceived(string Chunk) : ChatModelEvent;
    public sealed record ErrorOccurred(string Message) : ChatModelEvent;
}

// 4. Page and Handler implementations
public class ChatPage : Page<ChatUIEvent, ChatCommand> { ... }
public class ChatHandler : PageHandler<ChatPage, ChatUIEvent, ChatCommand, ChatModelEvent> { ... }
```

---

## Event Routing

### Frontend Events (UI → Handler)

1. Raw input (KeyPressed) arrives in channel
2. TerminaApp routes to active page's `MapToUIEvent()`
3. Page transforms to typed `TUIEvent` (or null to ignore)
4. TerminaApp routes to handler's `HandleFrontend()`
5. Handler may call `Send()` and/or `Publish()`

### Backend Events (Model → Handler)

1. Model layer publishes `IModelEvent` to application bus
2. TerminaApp looks up which handler(s) are registered for that event type
3. **Routing decision** (see below)
4. Handler's `HandleBackend()` is invoked
5. Handler calls `Send()` to update UI

### Backend Event Routing Rules

**Question**: When a model event arrives, who receives it?

**Answer**: Only the active page's handler, with these rules:

1. **Event type must be registered** - Handler declares what `TModelEvent` types it handles
2. **Handler's page must be active** - If page is not active, event goes to dead letter
3. **Dead letter logging** - Unrouted events are logged for debugging

**Rationale**:
- Simpler than queueing events for inactive pages
- Prevents state sync issues when navigating
- If backend work is page-specific, it should cancel on navigation anyway
- Global/cross-cutting concerns should be handled at Model layer, not UI

**Future consideration**: Could add event persistence/replay for specific event types if needed.

---

## TerminaApplication API

```csharp
public class TerminaApplication
{
    public TerminaApplication(IAnsiConsole console);

    // Page registration - handler declares its model event type
    public void RegisterPage<TPage, THandler, TUIEvent, TCommand, TModelEvent>(
        string pageKey,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : Page<TUIEvent, TCommand>, new()
        where THandler : PageHandler<TPage, TUIEvent, TCommand, TModelEvent>, new()
        where TModelEvent : IModelEvent;

    // Navigation
    public void NavigateTo(string pageKey);

    // Model layer integration - returns the bus for actors/services to publish to
    public IApplicationBus Bus { get; }

    // Run the application
    public Task RunAsync(CancellationToken cancellationToken = default);
}

public interface IApplicationBus
{
    // Model layer publishes events here
    void Publish<T>(T evt) where T : IModelEvent;

    // Dead letter event for logging/debugging
    event Action<object, string>? DeadLetter; // (event, reason)
}
```

---

## Handler Base Class

```csharp
public abstract class PageHandler<TPage, TUIEvent, TCommand, TModelEvent>
    where TPage : Page<TUIEvent, TCommand>
    where TUIEvent : IUIEvent
    where TCommand : IUICommand
    where TModelEvent : IModelEvent
{
    // Set by framework
    internal TPage Page { get; set; }
    internal IApplicationBus Bus { get; set; }
    internal Action<string> NavigateAction { get; set; }
    internal Action ShutdownAction { get; set; }

    // Implement these - both are synchronous, no async allowed
    protected abstract void HandleFrontend(TUIEvent evt);
    protected abstract void HandleBackend(TModelEvent evt);

    // Lifecycle hooks
    protected virtual void OnNavigatedTo() { }
    protected virtual void OnNavigatingFrom() { }

    // Available to handler implementations
    protected void Send(TCommand command) => Page.ApplyCommand(command);
    protected void Publish<T>(T evt) where T : IModelEvent => Bus.Publish(evt);
    protected void Navigate(string pageKey) => NavigateAction(pageKey);
    protected void Shutdown() => ShutdownAction();
}
```

---

## Page Base Class

```csharp
public abstract class Page<TUIEvent, TCommand> : IPage
    where TUIEvent : IUIEvent
    where TCommand : IUICommand
{
    // Components owned by this page
    public abstract IEnumerable<Component> Components { get; }

    // REQUIRED: Transform raw input to typed UI event
    protected abstract TUIEvent? MapToUIEvent(IInputEvent raw);

    // REQUIRED: Apply command to update component state
    protected abstract void ApplyCommand(TCommand command);

    // Rendering
    public abstract IRenderable Render();

    // Lifecycle
    public virtual void OnNavigatedTo() { }
    public virtual void OnNavigatingFrom() { }

    // Called by framework
    internal TUIEvent? TransformInput(IInputEvent raw) => MapToUIEvent(raw);
    internal void ReceiveCommand(TCommand command) => ApplyCommand(command);
}
```

---

## Example: Chat Page

```csharp
// Events from UI
public abstract record ChatUIEvent : IUIEvent
{
    public sealed record MessageSubmitted(string Text) : ChatUIEvent;
    public sealed record CancelClicked() : ChatUIEvent;
}

// Commands to UI
public abstract record ChatCommand : IUICommand
{
    public sealed record AppendMessage(string Role, string Text) : ChatCommand;
    public sealed record ShowTyping() : ChatCommand;
    public sealed record HideTyping() : ChatCommand;
    public sealed record ClearInput() : ChatCommand;
}

// Events from Model
public abstract record ChatModelEvent : IModelEvent
{
    public sealed record LlmResponseReceived(string Text) : ChatModelEvent;
    public sealed record LlmStreamChunk(string Chunk) : ChatModelEvent;
    public sealed record LlmError(string Message) : ChatModelEvent;
}

// Page
public class ChatPage : Page<ChatUIEvent, ChatCommand>
{
    public TextInput Input { get; } = new();
    public MessageList Messages { get; } = new();
    public TypingIndicator Typing { get; } = new();

    public override IEnumerable<Component> Components => [Input, Messages, Typing];

    protected override ChatUIEvent? MapToUIEvent(IInputEvent raw)
    {
        return raw switch
        {
            KeyPressed { KeyInfo.Key: ConsoleKey.Enter } when Input.HasText
                => new ChatUIEvent.MessageSubmitted(Input.Text),
            KeyPressed { KeyInfo.Key: ConsoleKey.Escape }
                => new ChatUIEvent.CancelClicked(),
            KeyPressed key => Input.HandleKey(key), // Returns null, component handles internally
            _ => null
        };
    }

    protected override void ApplyCommand(ChatCommand command)
    {
        switch (command)
        {
            case ChatCommand.AppendMessage(var role, var text):
                Messages.Add(role, text);
                break;
            case ChatCommand.ShowTyping():
                Typing.Visible = true;
                break;
            case ChatCommand.HideTyping():
                Typing.Visible = false;
                break;
            case ChatCommand.ClearInput():
                Input.Clear();
                break;
        }
    }

    public override IRenderable Render() => new Rows(Messages, Typing, Input);
}

// Handler - completely stateless
public class ChatHandler : PageHandler<ChatPage, ChatUIEvent, ChatCommand, ChatModelEvent>
{
    protected override void HandleFrontend(ChatUIEvent evt)
    {
        switch (evt)
        {
            case ChatUIEvent.MessageSubmitted(var text):
                Send(new ChatCommand.AppendMessage("user", text));
                Send(new ChatCommand.ClearInput());
                Send(new ChatCommand.ShowTyping());
                Publish(new SubmitPrompt(text)); // Model layer handles this
                break;

            case ChatUIEvent.CancelClicked():
                Publish(new CancelCurrentPrompt());
                Send(new ChatCommand.HideTyping());
                break;
        }
    }

    protected override void HandleBackend(ChatModelEvent evt)
    {
        switch (evt)
        {
            case ChatModelEvent.LlmResponseReceived(var text):
                Send(new ChatCommand.HideTyping());
                Send(new ChatCommand.AppendMessage("assistant", text));
                break;

            case ChatModelEvent.LlmError(var message):
                Send(new ChatCommand.HideTyping());
                Send(new ChatCommand.AppendMessage("error", message));
                break;
        }
    }
}
```

---

## Model Layer Integration (Example with Akka.NET)

```csharp
// Actor has no UI knowledge - just publishes domain events
public class LlmActor : ReceiveActor
{
    private readonly IApplicationBus _bus;
    private readonly ILlmClient _llm;

    public LlmActor(IApplicationBus bus, ILlmClient llm)
    {
        _bus = bus;
        _llm = llm;

        // Subscribe to domain events
        Receive<SubmitPrompt>(async msg =>
        {
            try
            {
                var response = await _llm.CompleteAsync(msg.Text);
                _bus.Publish(new ChatModelEvent.LlmResponseReceived(response));
            }
            catch (Exception ex)
            {
                _bus.Publish(new ChatModelEvent.LlmError(ex.Message));
            }
        });

        Receive<CancelCurrentPrompt>(_ =>
        {
            // Cancel logic...
        });
    }
}

// Wiring it up
var app = new TerminaApplication(AnsiConsole.Console);

app.RegisterPage<ChatPage, ChatHandler, ChatUIEvent, ChatCommand, ChatModelEvent>("chat");
app.RegisterPage<SettingsPage, SettingsHandler, ...>("settings");

// Give the bus to your actor system
var actorSystem = ActorSystem.Create("MyApp");
var llmActor = actorSystem.ActorOf(Props.Create(() => new LlmActor(app.Bus, llmClient)));

// Model layer subscribes to events it cares about
app.Bus.Subscribe<SubmitPrompt>(evt => llmActor.Tell(evt));
app.Bus.Subscribe<CancelCurrentPrompt>(evt => llmActor.Tell(evt));

app.NavigateTo("chat");
await app.RunAsync();
```

---

## Open Questions

1. **Event persistence for inactive pages** - Currently, events for inactive pages go to dead letter. Should we support queueing/replay for specific event types?

2. **Multiple handlers for same event type** - Should multiple pages be able to subscribe to the same model event type? If so, only active one gets routed?

3. **Component-level input handling** - Current design has `MapToUIEvent` at page level. Should components be able to consume input directly (like `TextInput` handling keystrokes)?

4. **Cancellation tokens** - How do we propagate cancellation to the model layer when navigating away? Publish a cancellation event?

---

## Implementation Plan

1. Define marker interfaces: `IUIEvent`, `IUICommand`, `IModelEvent`
2. Create `Page<TUIEvent, TCommand>` base class
3. Create `PageHandler<TPage, TUIEvent, TCommand, TModelEvent>` base class
4. Create `IApplicationBus` interface and implementation
5. Create `TerminaApplication` orchestrator
6. Update `NavigationService` to work with new architecture
7. Migrate spike pages to new pattern
8. Add dead letter logging
9. Write tests

