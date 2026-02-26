// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Akka.Actor;
using Akka.Hosting;
using R3;
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
public sealed record ReplaceTrackedSegment(SegmentId Id, ITextSegment NewSegment, bool KeepTracked = false)
    : IChatMessage;

/// <summary>
/// Show a decision point with choices for the user.
/// </summary>
public sealed record ShowDecisionPoint(string Question, IReadOnlyList<LlmMessages.DecisionChoice> Choices)
    : IChatMessage;

/// <summary>
/// Hide the decision list (after selection or cancellation).
/// </summary>
public sealed record HideDecisionPoint : IChatMessage;

/// <summary>
/// ViewModel for the streaming chat demo.
/// Contains only state and business logic - no UI concerns.
/// Exposes observables for chat content that the Page subscribes to.
/// </summary>
public class StreamingChatViewModel : ReactiveViewModel
{
    // Predefined segment IDs for tracked elements
    private static readonly SegmentId ThinkingSpinnerId = new(1);
    private static readonly SegmentId ThinkingBlockId = new(2);

    private readonly IRequiredActor<LlmSimulatorActor> _llmActorProvider;
    private IActorRef? _llmActor;
    private CancellationTokenSource? _generationCts;
    private SpinnerSegment? _currentSpinner;
    private bool _thinkingBlockShown;

    // Subjects for chat output - Page subscribes to these
    private readonly Subject<IChatMessage> _chatOutput = new();

    /// <summary>
    /// Observable for chat messages.
    /// Includes text appends and tracked segment operations.
    /// </summary>
    public Observable<IChatMessage> ChatOutput => _chatOutput.AsObservable();

    // Reactive properties for UI state
    public ReactiveProperty<bool> IsGenerating { get; } = new(false);
    public ReactiveProperty<bool> HasReceivedText { get; } = new(false); // Tracks if any text has arrived yet
    public ReactiveProperty<string> StatusMessage { get; } = new("Ready. Enter a question to begin.");
    public ReactiveProperty<bool> ShowDecisionList { get; } = new(false); // Whether to show the decision list

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
        _chatOutput.OnNext(new AppendText("   Ask me anything and watch the streaming response!", Color.BrightBlack,
            IsNewLine: true));
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
    /// Cancel the current generation.
    /// </summary>
    public void CancelGeneration()
    {
        _generationCts?.Cancel();
        _chatOutput.OnNext(new AppendText(" [cancelled]", Color.Yellow, TextDecoration.Italic, IsNewLine: true));
        CleanupGeneration();
        StatusMessage.Value = "Generation cancelled.";
    }

    /// <summary>
    /// Handle prompt submission.
    /// </summary>
    public void HandleSubmit(string prompt)
    {
        prompt = prompt.Trim();
        if (string.IsNullOrEmpty(prompt))
            return;

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
        ShowDecisionList.Value = false;
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
        ShowDecisionList.Value = false;
        _chatOutput.OnNext(new HideDecisionPoint());
        _chatOutput.OnNext(new AppendText(" [decision skipped]", Color.Yellow, TextDecoration.Italic, IsNewLine: true));
        CleanupGeneration();
        StatusMessage.Value = "Ready. Enter another question.";
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
        ShowDecisionList.Value = false;
        _chatOutput.OnNext(new HideDecisionPoint());

        // Show the custom prompt as user input
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
        _chatOutput.OnNext(new AppendText("👤 ", Color.Cyan));
        _chatOutput.OnNext(new AppendText("You: ", Color.Cyan, TextDecoration.Bold));
        _chatOutput.OnNext(new AppendText(customPrompt, Color.White, IsNewLine: true));
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));

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
        IsGenerating.Value = true;
        HasReceivedText.Value = false;
        _thinkingBlockShown = false;
        StatusMessage.Value = "Generating response...";

        _ = ConsumeResponseStreamAsync(prompt, decisionContext);
    }

    private async Task ConsumeResponseStreamAsync(string prompt, string? decisionContext = null)
    {
        if (_llmActor is null)
        {
            StatusMessage.Value = "Error: Actor not initialized";
            IsGenerating.Value = false;
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
                    case LlmMessages.ThinkingToken thinking:
                        // On first thinking token, replace spinner with thinking block
                        if (!_thinkingBlockShown)
                        {
                            _chatOutput.OnNext(new ReplaceTrackedSegment(
                                ThinkingSpinnerId,
                                new StaticTextSegment("", TextStyle.Default),
                                KeepTracked: false));
                            _currentSpinner?.Dispose();
                            _currentSpinner = null;

                            // Add thinking block that will update in place
                            var thinkingSegment = new StaticTextSegment(
                                $"💭 {thinking.Text}",
                                new TextStyle { Foreground = Color.BrightBlack, Decoration = TextDecoration.Italic }).AsBlock();
                            _chatOutput.OnNext(new AppendTrackedSegment(ThinkingBlockId, thinkingSegment));
                            _thinkingBlockShown = true;
                        }
                        else
                        {
                            // Update existing thinking block
                            var thinkingSegment = new StaticTextSegment(
                                $"💭 {thinking.Text}",
                                new TextStyle { Foreground = Color.BrightBlack, Decoration = TextDecoration.Italic }).AsBlock();
                            _chatOutput.OnNext(new ReplaceTrackedSegment(ThinkingBlockId, thinkingSegment, KeepTracked: true));
                        }
                        break;

                    case LlmMessages.TextChunk chunk:
                        // On first text chunk, remove thinking block
                        if (!HasReceivedText.Value)
                        {
                            _chatOutput.OnNext(new RemoveTrackedSegment(ThinkingBlockId));
                            HasReceivedText.Value = true;
                        }

                        _chatOutput.OnNext(new AppendText(chunk.Text));
                        break;

                    case LlmMessages.GenerationComplete:
                        CleanupGeneration();
                        StatusMessage.Value = "Ready. Enter another question.";
                        completedNormally = true;
                        break;

                    case LlmMessages.DecisionPointToken decision:
                        // On first content (decision point), remove thinking block
                        if (!HasReceivedText.Value)
                        {
                            _chatOutput.OnNext(new RemoveTrackedSegment(ThinkingBlockId));
                            HasReceivedText.Value = true;
                        }

                        // Show the decision list
                        _chatOutput.OnNext(new ShowDecisionPoint(decision.Question, decision.Choices));
                        ShowDecisionList.Value = true;
                        StatusMessage.Value = "Make a selection below...";

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
            StatusMessage.Value = $"Error: {ex.Message}";
        }
        finally
        {
            if (!completedNormally && IsGenerating.Value)
            {
                CleanupGeneration();
                StatusMessage.Value = "Ready. Enter another question.";
            }
        }
    }

    private void CleanupGeneration()
    {
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
        _chatOutput.OnNext(new AppendText("", IsNewLine: true));
        IsGenerating.Value = false;
        _generationCts?.Dispose();
        _generationCts = null;
    }

    public override void Dispose()
    {
        _currentSpinner?.Dispose();
        _chatOutput.Dispose();
        IsGenerating.Dispose();
        HasReceivedText.Dispose();
        StatusMessage.Dispose();
        ShowDecisionList.Dispose();
        base.Dispose();
    }
}
