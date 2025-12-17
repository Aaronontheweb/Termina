// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Hosting;
using Termina.Demo.Streaming.Actors;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Demo.Streaming.Pages;

/// <summary>
/// Represents a styled text segment for chat display.
/// </summary>
public readonly record struct ChatTextSegment(
    string Text,
    Color? Foreground = null,
    TextDecoration Decoration = TextDecoration.None,
    bool IsNewLine = false);

/// <summary>
/// ViewModel for the streaming chat demo.
/// Contains only state and business logic - no UI concerns.
/// Exposes observables for chat content that the Page subscribes to.
/// </summary>
public partial class StreamingChatViewModel : ReactiveViewModel
{
    private readonly IRequiredActor<LlmSimulatorActor> _llmActorProvider;
    private readonly List<string> _promptHistory = new();
    private int _historyIndex = -1;
    private IActorRef? _llmActor;
    private CancellationTokenSource? _generationCts;

    // Subjects for chat output - Page subscribes to these
    private readonly Subject<ChatTextSegment> _chatOutput = new();
    private readonly Subject<string> _promptTextChanged = new();

    /// <summary>
    /// Observable for chat history content.
    /// </summary>
    public IObservable<ChatTextSegment> ChatOutput => _chatOutput.AsObservable();

    /// <summary>
    /// Observable for prompt text changes (for history navigation).
    /// </summary>
    public IObservable<string> PromptTextChanged => _promptTextChanged.AsObservable();

    // Reactive properties for UI state
    [Reactive] private bool _isGenerating = false;
    [Reactive] private string _statusMessage = "Ready. Enter a question to begin.";

    public StreamingChatViewModel(IRequiredActor<LlmSimulatorActor> llmActorProvider)
    {
        _llmActorProvider = llmActorProvider;
    }

    /// <summary>
    /// Emits the initial welcome message.
    /// </summary>
    public void EmitWelcomeMessage()
    {
        _chatOutput.OnNext(new ChatTextSegment("🤖 ", Color.Yellow));
        _chatOutput.OnNext(new ChatTextSegment("Assistant: ", Color.Green, TextDecoration.Bold));
        _chatOutput.OnNext(new ChatTextSegment("Hello! I'm a simulated LLM demo.", Color.White, IsNewLine: true));
        _chatOutput.OnNext(new ChatTextSegment("   Ask me anything and watch the streaming response!", Color.BrightBlack, IsNewLine: true));
        _chatOutput.OnNext(new ChatTextSegment("", IsNewLine: true));
    }

    public override void OnActivated()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _llmActor = await _llmActorProvider.GetAsync();
    }

    /// <summary>
    /// Navigate up through prompt history.
    /// </summary>
    public void NavigateHistoryUp()
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

        _promptTextChanged.OnNext(_promptHistory[_historyIndex]);
    }

    /// <summary>
    /// Navigate down through prompt history.
    /// </summary>
    public void NavigateHistoryDown()
    {
        if (_historyIndex < 0)
            return;

        if (_historyIndex < _promptHistory.Count - 1)
        {
            _historyIndex++;
            _promptTextChanged.OnNext(_promptHistory[_historyIndex]);
        }
        else
        {
            _historyIndex = -1;
            _promptTextChanged.OnNext("");
        }
    }

    /// <summary>
    /// Cancel the current generation.
    /// </summary>
    public void CancelGeneration()
    {
        _generationCts?.Cancel();
        _chatOutput.OnNext(new ChatTextSegment(" [cancelled]", Color.Yellow, TextDecoration.Italic, IsNewLine: true));
        CleanupGeneration();
        StatusMessage = "Generation cancelled.";
    }

    /// <summary>
    /// Handle prompt submission.
    /// </summary>
    public void HandleSubmit(string prompt)
    {
        prompt = prompt.Trim();
        if (string.IsNullOrEmpty(prompt))
            return;

        // Add to history and reset index
        _promptHistory.Add(prompt);
        _historyIndex = -1;

        // Add user message to chat
        _chatOutput.OnNext(new ChatTextSegment("👤 ", Color.Cyan));
        _chatOutput.OnNext(new ChatTextSegment("You: ", Color.Cyan, TextDecoration.Bold));
        _chatOutput.OnNext(new ChatTextSegment(prompt, Color.White, IsNewLine: true));
        _chatOutput.OnNext(new ChatTextSegment("", IsNewLine: true));
        _chatOutput.OnNext(new ChatTextSegment("🤖 ", Color.Yellow));
        _chatOutput.OnNext(new ChatTextSegment("Assistant: ", Color.Green, TextDecoration.Bold));

        // Start generation
        IsGenerating = true;
        StatusMessage = "Generating response...";

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
            var response = await _llmActor.Ask<LlmMessages.GenerateResponse>(
                new LlmMessages.GenerateRequest(prompt),
                TimeSpan.FromSeconds(30));

            _generationCts = response.Cancellation;

            await foreach (var token in response.TokenStream.WithCancellation(_generationCts.Token))
            {
                switch (token)
                {
                    case LlmMessages.ThinkingToken:
                        // Skip thinking tokens - status bar already shows "Generating response..."
                        break;

                    case LlmMessages.TextChunk chunk:
                        _chatOutput.OnNext(new ChatTextSegment(chunk.Text));
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
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _chatOutput.OnNext(new ChatTextSegment(" [error: ", Color.Red));
            _chatOutput.OnNext(new ChatTextSegment(ex.Message, Color.Red, TextDecoration.Bold));
            _chatOutput.OnNext(new ChatTextSegment("]", Color.Red, IsNewLine: true));
            CleanupGeneration();
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            if (!completedNormally && IsGenerating)
            {
                CleanupGeneration();
                StatusMessage = "Ready. Enter another question.";
            }
        }
    }

    private void CleanupGeneration()
    {
        _chatOutput.OnNext(new ChatTextSegment("", IsNewLine: true));
        _chatOutput.OnNext(new ChatTextSegment("", IsNewLine: true));
        IsGenerating = false;
        _generationCts?.Dispose();
        _generationCts = null;
    }

    public override void Dispose()
    {
        _chatOutput.Dispose();
        _promptTextChanged.Dispose();
        DisposeReactiveFields();
        base.Dispose();
    }
}
