using System.Reactive.Linq;
using Akka.Actor;
using Akka.Hosting;
using Termina.Components;
using Termina.Components.Streaming;
using Termina.Demo.Streaming.Actors;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Streaming.Pages;

/// <summary>
/// ViewModel for the streaming chat demo.
/// Components own their state - ViewModel just wires up events.
/// Uses IAsyncEnumerable from Akka Streams for token consumption.
/// </summary>
public partial class StreamingChatViewModel : ReactiveViewModel
{
    private readonly IRequiredActor<LlmSimulatorActor> _llmActorProvider;
    private IActorRef? _llmActor;
    private CancellationTokenSource? _generationCts;

    /// <summary>
    /// Chat history component (persisted, scrollable).
    /// </summary>
    public StreamingText ChatHistory { get; } = StreamingText.CreatePersisted();

    /// <summary>
    /// Thinking indicator component (windowed, rolling).
    /// </summary>
    public StreamingText ThinkingIndicator { get; } = StreamingText.CreateWindowed(windowSize: 3);

    /// <summary>
    /// Text input component - handles its own keyboard input.
    /// </summary>
    public TextInput PromptInput { get; } = new()
    {
        Label = "You",
        Placeholder = "Enter your question...",
        IsFocused = true,
        FocusedColor = Spectre.Console.Color.Cyan1
    };

    // Reactive properties for UI state
    [Reactive] private bool _isGenerating = false;
    [Reactive] private string _statusMessage = "Ready. Enter a question to begin.";

    public StreamingChatViewModel(IRequiredActor<LlmSimulatorActor> llmActorProvider)
    {
        _llmActorProvider = llmActorProvider;

        // Configure streaming components
        ChatHistory.ViewportHeight = 15;
        ChatHistory.ViewportWidth = 78;
        ChatHistory.Prefix = "  ";
        ChatHistory.PrefixStyle = new Spectre.Console.Style(Spectre.Console.Color.Grey);

        ThinkingIndicator.ViewportHeight = 3;
        ThinkingIndicator.ViewportWidth = 60;
        ThinkingIndicator.Prefix = "💭 ";
        ThinkingIndicator.PrefixStyle = new Spectre.Console.Style(Spectre.Console.Color.Yellow);
        ThinkingIndicator.TextStyle = new Spectre.Console.Style(Spectre.Console.Color.Grey);

        // Add initial welcome message
        ChatHistory.AppendLine("🤖 Assistant: Hello! I'm a simulated LLM demo.");
        ChatHistory.AppendLine("   Ask me anything and watch the streaming response!");
        ChatHistory.AppendLine("");

        // Wire up submit event from the text input component
        PromptInput.OnSubmit += HandleSubmit;
    }

    public override void OnActivated()
    {
        // Subscribe to component content changes to trigger UI redraws
        // This is the marshalling mechanism for async backend updates to the UI
        ChatHistory.ContentChanged
            .Subscribe(_ => RequestRedraw())
            .DisposeWith(Subscriptions);

        ThinkingIndicator.ContentChanged
            .Subscribe(_ => RequestRedraw())
            .DisposeWith(Subscriptions);

        // Wait for the actor to be available, then subscribe to input
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Wait for the LLM actor to be registered by Akka.Hosting
        _llmActor = await _llmActorProvider.GetAsync();

        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);
    }

    private void HandleKeyPress(KeyPressed key)
    {
        var keyInfo = key.KeyInfo;

        // Ctrl+Q always quits
        if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            Shutdown();
            return;
        }

        // Escape handling
        if (keyInfo.Key == ConsoleKey.Escape)
        {
            if (_isGenerating)
            {
                CancelGeneration();
            }
            else
            {
                // When not generating, Escape clears input or quits if input is empty
                if (string.IsNullOrEmpty(PromptInput.Text))
                {
                    Shutdown();
                }
                else
                {
                    PromptInput.Clear();
                    StatusMessage = "Input cleared. Press Esc again to quit.";
                }
            }
            return;
        }

        // Scrolling during generation
        if (_isGenerating)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    ChatHistory.ScrollUp();
                    return;

                case ConsoleKey.DownArrow:
                    ChatHistory.ScrollDown();
                    return;
            }
        }

        // When not generating, let the text input handle keys
        if (!_isGenerating)
        {
            PromptInput.HandleKey(keyInfo);
        }
    }

    private void HandleSubmit(string prompt)
    {
        prompt = prompt.Trim();
        if (string.IsNullOrEmpty(prompt))
            return;

        // Scroll to bottom to see new content and re-enable auto-scroll
        ChatHistory.ScrollToBottom();

        // Add user message to chat
        ChatHistory.AppendLine($"👤 You: {prompt}");
        ChatHistory.AppendLine("");
        ChatHistory.Append("🤖 Assistant: ");

        // Clear input
        PromptInput.Clear();
        PromptInput.IsFocused = false;

        // Start generation
        IsGenerating = true;
        StatusMessage = "Generating response...";

        // Request stream from actor and consume it
        _ = ConsumeResponseStreamAsync(prompt);
    }

    private async Task ConsumeResponseStreamAsync(string prompt)
    {
        if (_llmActor is null)
        {
            StatusMessage = "Error: Actor not initialized";
            IsGenerating = false;
            PromptInput.IsFocused = true;
            return;
        }

        try
        {
            // Ask actor for the stream
            var response = await _llmActor.Ask<LlmMessages.GenerateResponse>(
                new LlmMessages.GenerateRequest(prompt),
                TimeSpan.FromSeconds(30));

            _generationCts = response.Cancellation;

            // Consume the IAsyncEnumerable from the actor
            await foreach (var token in response.TokenStream.WithCancellation(_generationCts.Token))
            {
                switch (token)
                {
                    case LlmMessages.ThinkingToken thinking:
                        ThinkingIndicator.AppendLine(thinking.Text);
                        break;

                    case LlmMessages.TextChunk chunk:
                        // Clear thinking when text starts
                        if (ThinkingIndicator.Buffer.HasContent)
                        {
                            ThinkingIndicator.Clear();
                        }
                        ChatHistory.Append(chunk.Text);
                        break;

                    case LlmMessages.GenerationComplete:
                        CleanupGeneration();
                        StatusMessage = "Ready. Enter another question.";
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation - already handled in CancelGeneration
        }
        catch (Exception ex)
        {
            CleanupGeneration($" [error: {ex.Message}]");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void CancelGeneration()
    {
        _generationCts?.Cancel();
        CleanupGeneration(" [cancelled]");
        StatusMessage = "Generation cancelled.";
    }

    private void CleanupGeneration(string? suffix = null)
    {
        if (suffix != null)
        {
            ChatHistory.AppendLine(suffix);
        }
        ChatHistory.AppendLine("");

        ThinkingIndicator.Clear();
        IsGenerating = false;
        PromptInput.IsFocused = true;

        _generationCts?.Dispose();
        _generationCts = null;
    }
}
