// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Akka.Actor;
using Akka.Hosting;
using Termina.Demo.Streaming.Actors;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Demo.Streaming.Pages;

/// <summary>
/// ViewModel for the streaming chat demo.
/// Uses the new StreamingTextNode and TextInputNode for rendering.
/// Uses IAsyncEnumerable from Akka Streams for token consumption.
/// </summary>
public partial class StreamingChatViewModel : ReactiveViewModel
{
    private readonly IRequiredActor<LlmSimulatorActor> _llmActorProvider;
    private readonly List<string> _promptHistory = new();
    private int _historyIndex = -1;
    private IActorRef? _llmActor;
    private CancellationTokenSource? _generationCts;

    /// <summary>
    /// Chat history component - renders all content with scrolling.
    /// </summary>
    public StreamingTextNode ChatHistory { get; } = StreamingTextNode.Create()
        .WithPrefix("  ", Color.Gray);

    /// <summary>
    /// Thinking indicator component (windowed, rolling).
    /// </summary>
    public StreamingTextNode ThinkingIndicator { get; } = StreamingTextNode.CreateWindowed(windowSize: 3)
        .WithPrefix("💭 ", Color.Yellow)
        .WithForeground(Color.Gray);

    /// <summary>
    /// Text input component - handles its own keyboard input.
    /// </summary>
    public TextInputNode PromptInput { get; } = new TextInputNode()
        .WithPlaceholder("Enter your question...")
        .WithForeground(Color.Cyan);

    // Reactive properties for UI state
    [Reactive] private bool _isGenerating = false;
    [Reactive] private string _statusMessage = "Ready. Enter a question to begin.";

    public StreamingChatViewModel(IRequiredActor<LlmSimulatorActor> llmActorProvider)
    {
        _llmActorProvider = llmActorProvider;

        // Add initial welcome message with styled text
        ChatHistory.Append("🤖 ", foreground: Color.Yellow);
        ChatHistory.Append("Assistant: ", foreground: Color.Green, decoration: TextDecoration.Bold);
        ChatHistory.AppendLine("Hello! I'm a simulated LLM demo.", foreground: Color.White);
        ChatHistory.AppendLine("   Ask me anything and watch the streaming response!", foreground: Color.BrightBlack);
        ChatHistory.AppendLine("");

        // Wire up submit observable from the text input component
        PromptInput.Submitted
            .Subscribe(HandleSubmit)
            .DisposeWith(Subscriptions);
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
            if (IsGenerating)
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
                    PromptInput.Text = "";
                    StatusMessage = "Input cleared. Press Esc again to quit.";
                }
            }
            return;
        }

        // Page Up/Down always scroll chat history (works during generation too)
        // Use reasonable defaults for viewport (scrolls ~10 lines per page)
        if (ChatHistory.HandleInput(keyInfo, viewportHeight: 10, viewportWidth: 80))
        {
            return;
        }

        // When not generating, handle input
        if (!IsGenerating)
        {
            // Handle history navigation (Up/Down arrows)
            if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                NavigateHistoryUp();
                return;
            }
            if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                NavigateHistoryDown();
                return;
            }

            // Let the text input handle other keys
            PromptInput.HandleInput(keyInfo);
        }
    }

    private void NavigateHistoryUp()
    {
        if (_promptHistory.Count == 0)
            return;

        if (_historyIndex < 0)
        {
            _historyIndex = _promptHistory.Count - 1;
        }
        else if (_historyIndex > 0)
        {
            _historyIndex--;
        }

        PromptInput.Text = _promptHistory[_historyIndex];
    }

    private void NavigateHistoryDown()
    {
        if (_historyIndex < 0)
            return;

        if (_historyIndex < _promptHistory.Count - 1)
        {
            _historyIndex++;
            PromptInput.Text = _promptHistory[_historyIndex];
        }
        else
        {
            _historyIndex = -1;
            PromptInput.Text = "";
        }
    }

    private void HandleSubmit(string prompt)
    {
        prompt = prompt.Trim();
        if (string.IsNullOrEmpty(prompt))
            return;

        // Add to history and reset index
        _promptHistory.Add(prompt);
        _historyIndex = -1;

        // Clear the input
        PromptInput.Clear();

        // Add user message to chat with styled text
        ChatHistory.Append("👤 ", foreground: Color.Cyan);
        ChatHistory.Append("You: ", foreground: Color.Cyan, decoration: TextDecoration.Bold);
        ChatHistory.AppendLine(prompt, foreground: Color.White);
        ChatHistory.AppendLine("");
        ChatHistory.Append("🤖 ", foreground: Color.Yellow);
        ChatHistory.Append("Assistant: ", foreground: Color.Green, decoration: TextDecoration.Bold);

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
            return;
        }

        var completedNormally = false;
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
                        ThinkingIndicator.AppendLine(thinking.Text, foreground: Color.BrightBlack, decoration: TextDecoration.Italic);
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
                        completedNormally = true;
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
            CleanupGenerationWithError(ex.Message);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            // Ensure IsGenerating is always reset, even if stream ends unexpectedly
            if (!completedNormally && IsGenerating)
            {
                CleanupGeneration();
                StatusMessage = "Ready. Enter another question.";
            }
        }
    }

    private void CancelGeneration()
    {
        _generationCts?.Cancel();
        ChatHistory.AppendLine(" [cancelled]", foreground: Color.Yellow, decoration: TextDecoration.Italic);
        CleanupGeneration();
        StatusMessage = "Generation cancelled.";
    }

    private void CleanupGenerationWithError(string errorMessage)
    {
        ChatHistory.Append(" [error: ", foreground: Color.Red);
        ChatHistory.Append(errorMessage, foreground: Color.Red, decoration: TextDecoration.Bold);
        ChatHistory.AppendLine("]", foreground: Color.Red);
        CleanupGeneration();
    }

    private void CleanupGeneration()
    {
        ChatHistory.AppendLine("");

        ThinkingIndicator.Clear();
        IsGenerating = false;

        _generationCts?.Dispose();
        _generationCts = null;
    }
}
