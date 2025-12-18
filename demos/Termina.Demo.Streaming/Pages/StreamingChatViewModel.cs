// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Hosting;
using Termina.Components.Streaming;
using Termina.Demo.Streaming.Actors;
using Termina.Layout;
using Termina.Reactive;
using Termina.Terminal;

namespace Termina.Demo.Streaming.Pages;

/// <summary>
/// Base interface for chat messages.
/// </summary>
public interface IChatMessage;

/// <summary>
/// Append regular text to the chat history.
/// </summary>
public sealed record AppendText(
    string Text,
    Color? Foreground = null,
    TextDecoration Decoration = TextDecoration.None,
    bool IsNewLine = false) : IChatMessage;

/// <summary>
/// Append a tracked text segment that can be manipulated later.
/// </summary>
public sealed record AppendTrackedSegment(SegmentId Id, ITextSegment Segment) : IChatMessage;

/// <summary>
/// Remove a tracked segment by ID.
/// </summary>
public sealed record RemoveTrackedSegment(SegmentId Id) : IChatMessage;

/// <summary>
/// Replace a tracked segment with new content.
/// </summary>
public sealed record ReplaceTrackedSegment(SegmentId Id, ITextSegment NewSegment, bool KeepTracked = false) : IChatMessage;

/// <summary>
/// Show a decision point with choices for the user.
/// </summary>
public sealed record ShowDecisionPoint(string Question, IReadOnlyList<LlmMessages.DecisionChoice> Choices) : IChatMessage;

/// <summary>
/// Hide the decision list (after selection or cancellation).
/// </summary>
public sealed record HideDecisionPoint : IChatMessage;

/// <summary>
/// ViewModel for the streaming chat demo.
/// Contains only state and business logic - no UI concerns.
/// Exposes observables for chat content that the Page subscribes to.
/// </summary>
public partial class StreamingChatViewModel : ReactiveViewModel
{
    // Predefined segment IDs for tracked elements
    private static readonly SegmentId ThinkingSpinnerId = new(1);

    private readonly IRequiredActor<LlmSimulatorActor> _llmActorProvider;
    private readonly List<string> _promptHistory = new();
    private int _historyIndex = -1;
    private IActorRef? _llmActor;
    private CancellationTokenSource? _generationCts;
    private SpinnerSegment? _currentSpinner;

    // Subjects for chat output - Page subscribes to these
    private readonly Subject<IChatMessage> _chatOutput = new();
    private readonly Subject<string> _promptTextChanged = new();

    /// <summary>
    /// Observable for chat messages.
    /// Includes text appends and tracked segment operations.
    /// </summary>
    public IObservable<IChatMessage> ChatOutput => _chatOutput.AsObservable();

    /// <summary>
    /// Observable for prompt text changes (for history navigation).
    /// </summary>
    public IObservable<string> PromptTextChanged => _promptTextChanged.AsObservable();

    // Reactive properties for UI state
    [Reactive] private bool _isGenerating = false;
    [Reactive] private bool _hasReceivedText = false; // Tracks if any text has arrived yet
    [Reactive] private string _statusMessage = "Ready. Enter a question to begin.";
    [Reactive] private bool _showDecisionList = false; // Whether to show the decision list

    // Track current decision context for follow-up
    private string? _pendingDecisionContext;

    public StreamingChatViewModel(IRequiredActor<LlmSimulatorActor> llmActorProvider)
    {
        _llmActorProvider = llmActorProvider;
    }

    /// <summary>
    /// Emits the initial welcome message.
    /// </summary>
    public void EmitWelcomeMessage()
    {
        _chatOutput.OnNext(new AppendText("🤖 ", Color.Yellow));
        _chatOutput.OnNext(new AppendText("Assistant: ", Color.Green, TextDecoration.Bold));
        _chatOutput.OnNext(new AppendText("Hello! I'm a simulated LLM demo.", Color.White, IsNewLine: true));
        _chatOutput.OnNext(new AppendText("   Ask me anything and watch the streaming response!", Color.BrightBlack, IsNewLine: true));
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
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
        _chatOutput.OnNext(new AppendText(" [cancelled]", Color.Yellow, TextDecoration.Italic, IsNewLine: true));
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
        _chatOutput.OnNext(new AppendText("👤 ", Color.Cyan));
        _chatOutput.OnNext(new AppendText("You: ", Color.Cyan, TextDecoration.Bold));
        _chatOutput.OnNext(new AppendText(prompt, Color.White, IsNewLine: true));
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));

        StartGeneration(prompt, decisionContext: null);
    }

    /// <summary>
    /// Handle decision selection from the SelectionListNode.
    /// </summary>
    public void HandleDecisionSelection(string choiceTitle)
    {
        // Hide the decision list
        ShowDecisionList = false;
        _chatOutput.OnNext(new HideDecisionPoint());

        // Show the user's choice in the chat
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
        _chatOutput.OnNext(new AppendText("   → ", Color.BrightBlack));
        _chatOutput.OnNext(new AppendText(choiceTitle, Color.Cyan, TextDecoration.Bold, IsNewLine: true));
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));

        // Start follow-up generation with the decision context
        StartGeneration("continue", decisionContext: choiceTitle);
    }

    /// <summary>
    /// Handle decision cancellation.
    /// </summary>
    public void HandleDecisionCancelled()
    {
        ShowDecisionList = false;
        _chatOutput.OnNext(new HideDecisionPoint());
        _chatOutput.OnNext(new AppendText(" [decision skipped]", Color.Yellow, TextDecoration.Italic, IsNewLine: true));
        CleanupGeneration();
        StatusMessage = "Ready. Enter another question.";
    }

    /// <summary>
    /// Handle custom prompt from "Something else..." option.
    /// </summary>
    public void HandleCustomPrompt(string customPrompt)
    {
        customPrompt = customPrompt.Trim();
        if (string.IsNullOrEmpty(customPrompt))
        {
            HandleDecisionCancelled();
            return;
        }

        // Hide the decision list
        ShowDecisionList = false;
        _chatOutput.OnNext(new HideDecisionPoint());

        // Show the custom prompt as user input
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
        _chatOutput.OnNext(new AppendText("👤 ", Color.Cyan));
        _chatOutput.OnNext(new AppendText("You: ", Color.Cyan, TextDecoration.Bold));
        _chatOutput.OnNext(new AppendText(customPrompt, Color.White, IsNewLine: true));
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));

        // Add to history
        _promptHistory.Add(customPrompt);
        _historyIndex = -1;

        // Start generation with the custom prompt (not as decision context)
        StartGeneration(customPrompt, decisionContext: null);
    }

    private void StartGeneration(string prompt, string? decisionContext)
    {
        // Add Assistant prefix and append animated spinner
        _chatOutput.OnNext(new AppendText("🤖 ", Color.Yellow));
        _chatOutput.OnNext(new AppendText("Assistant: ", Color.Green, TextDecoration.Bold));

        // Append tracked spinner - will be replaced when first text arrives
        _currentSpinner = new SpinnerSegment(Components.Streaming.SpinnerStyle.Dots, Color.Red);
        _chatOutput.OnNext(new AppendTrackedSegment(ThinkingSpinnerId, _currentSpinner));

        // Start generation
        IsGenerating = true;
        HasReceivedText = false;
        StatusMessage = "Generating response...";

        _ = ConsumeResponseStreamAsync(prompt, decisionContext);
    }

    private async Task ConsumeResponseStreamAsync(string prompt, string? decisionContext = null)
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
                new LlmMessages.GenerateRequest(prompt, decisionContext),
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
                        // On first text chunk, replace the spinner with static empty segment
                        if (!HasReceivedText)
                        {
                            _chatOutput.OnNext(new ReplaceTrackedSegment(
                                ThinkingSpinnerId,
                                new StaticTextSegment("", TextStyle.Default),
                                KeepTracked: false));
                            _currentSpinner?.Dispose();
                            _currentSpinner = null;
                            HasReceivedText = true;
                        }

                        _chatOutput.OnNext(new AppendText(chunk.Text));
                        break;

                    case LlmMessages.GenerationComplete:
                        CleanupGeneration();
                        StatusMessage = "Ready. Enter another question.";
                        completedNormally = true;
                        break;

                    case LlmMessages.DecisionPointToken decision:
                        // On first content (decision point), replace spinner
                        if (!HasReceivedText)
                        {
                            _chatOutput.OnNext(new ReplaceTrackedSegment(
                                ThinkingSpinnerId,
                                new StaticTextSegment("", TextStyle.Default),
                                KeepTracked: false));
                            _currentSpinner?.Dispose();
                            _currentSpinner = null;
                            HasReceivedText = true;
                        }

                        // Show the decision list
                        _chatOutput.OnNext(new ShowDecisionPoint(decision.Question, decision.Choices));
                        ShowDecisionList = true;
                        StatusMessage = "Make a selection below...";

                        // We don't mark this as complete - we wait for user to make a decision
                        return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _chatOutput.OnNext(new AppendText(" [error: ", Color.Red));
            _chatOutput.OnNext(new AppendText(ex.Message, Color.Red, TextDecoration.Bold));
            _chatOutput.OnNext(new AppendText("]", Color.Red, IsNewLine: true));
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
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
        IsGenerating = false;
        _generationCts?.Dispose();
        _generationCts = null;
    }

    public override void Dispose()
    {
        _currentSpinner?.Dispose();
        _chatOutput.Dispose();
        _promptTextChanged.Dispose();
        DisposeReactiveFields();
        base.Dispose();
    }
}
