// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Streaming.Pages;

/// <summary>
/// Demo page showing streaming text components with LLM simulation.
/// Handles all UI concerns including layout nodes, focus, and input routing.
/// </summary>
public class StreamingChatPage : ReactivePage<StreamingChatViewModel>
{
    // Layout nodes owned by the Page
    private StreamingTextNode _chatHistory = null!;
    private StreamingTextNode _thinkingIndicator = null!;
    private TextInputNode _promptInput = null!;
    private SpinnerNode _thinkingSpinner = null!;

    protected override void OnBound()
    {
        // Create layout nodes
        _chatHistory = StreamingTextNode.Create()
            .WithPrefix("  ", Color.Gray);

        _thinkingIndicator = StreamingTextNode.CreateWindowed(windowSize: 3)
            .WithPrefix("💭 ", Color.Yellow)
            .WithForeground(Color.Gray);

        _promptInput = new TextInputNode()
            .WithPlaceholder("Enter your question...")
            .WithForeground(Color.Cyan);

        _thinkingSpinner = new SpinnerNode(SpinnerStyle.Dots)
            .WithLabel("Thinking...")
            .WithSpinnerColor(Color.Yellow)
            .WithLabelColor(Color.Gray);

        // Subscribe to ViewModel chat output and update nodes
        ViewModel.ChatOutput
            .Subscribe(segment =>
            {
                if (segment.IsNewLine)
                    _chatHistory.AppendLine(segment.Text, segment.Foreground, null, segment.Decoration);
                else
                    _chatHistory.Append(segment.Text, segment.Foreground, null, segment.Decoration);
            })
            .DisposeWith(Subscriptions);

        ViewModel.ThinkingOutput
            .Subscribe(segment =>
            {
                if (segment.IsNewLine)
                    _thinkingIndicator.AppendLine(segment.Text, segment.Foreground, null, segment.Decoration);
                else
                    _thinkingIndicator.Append(segment.Text, segment.Foreground, null, segment.Decoration);
            })
            .DisposeWith(Subscriptions);

        ViewModel.ClearThinking
            .Subscribe(_ => _thinkingIndicator.Clear())
            .DisposeWith(Subscriptions);

        ViewModel.PromptTextChanged
            .Subscribe(text => _promptInput.Text = text)
            .DisposeWith(Subscriptions);

        // Subscribe to prompt input submission
        _promptInput.Submitted
            .Subscribe(text =>
            {
                ViewModel.HandleSubmit(text);
                _promptInput.Clear();
            })
            .DisposeWith(Subscriptions);

        // Handle keyboard input - Page routes to interactive layout nodes
        ViewModel.Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);

        // Emit welcome message
        ViewModel.EmitWelcomeMessage();
    }

    private void HandleKeyPress(KeyPressed key)
    {
        var keyInfo = key.KeyInfo;

        // Ctrl+Q always quits
        if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            ViewModel.RequestShutdown();
            return;
        }

        // Escape handling
        if (keyInfo.Key == ConsoleKey.Escape)
        {
            if (ViewModel.IsGenerating)
            {
                ViewModel.CancelGeneration();
            }
            else
            {
                ViewModel.RequestShutdown();
            }
            return;
        }

        // Page Up/Down scroll chat history
        if (_chatHistory.HandleInput(keyInfo, viewportHeight: 10, viewportWidth: 80))
        {
            return;
        }

        // When not generating, handle other input
        if (!ViewModel.IsGenerating)
        {
            if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                ViewModel.NavigateHistoryUp();
                return;
            }
            if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                ViewModel.NavigateHistoryDown();
                return;
            }

            // Let the text input handle other keys
            _promptInput.HandleInput(keyInfo);
        }
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            // Header
            .WithChild(
                new TextNode("🤖 Streaming Chat Demo - Simulated LLM with Akka.NET")
                    .WithForeground(Color.Cyan)
                    .Bold()
                    .Height(1))
            .WithChild(new EmptyNode().Height(1))
            // Chat history panel
            .WithChild(
                new PanelNode()
                    .WithTitle("Chat History")
                    .WithTitleColor(Color.Yellow)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Gray)
                    .WithContent(_chatHistory)
                    .Fill())
            // Thinking indicator - conditionally shown
            .WithChild(
                ViewModel.IsGeneratingChanged
                    .Select(isGenerating => isGenerating
                        ? BuildThinkingPanel()
                        : (ILayoutNode)new EmptyNode())
                    .AsLayout())
            .WithChild(new EmptyNode().Height(1))
            // Input panel
            .WithChild(
                new PanelNode()
                    .WithTitle("Your Prompt")
                    .WithTitleColor(Color.Cyan)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Cyan)
                    .WithContent(_promptInput)
                    .Height(3))
            // Status bar
            .WithChild(
                ViewModel.IsGeneratingChanged
                    .Select(isGenerating => new TextNode(
                        isGenerating
                            ? "[Esc] Cancel [PgUp/PgDn] Scroll [Ctrl+Q] Quit"
                            : "[Enter] Send [↑/↓] History [PgUp/PgDn] Scroll [Esc] Clear/Quit [Ctrl+Q] Quit")
                        .WithForeground(Color.BrightBlack)
                        .NoWrap())
                    .AsLayout()
                    .Height(1))
            .WithChild(
                ViewModel.StatusMessageChanged
                    .Select(msg => new TextNode(msg).WithForeground(Color.White))
                    .AsLayout()
                    .Height(1));
    }

    private ILayoutNode BuildThinkingPanel()
    {
        return Layouts.Vertical()
            .WithChild(new EmptyNode().Height(1))
            .WithChild(
                new PanelNode()
                    .WithTitle("Thinking...")
                    .WithTitleColor(Color.Yellow)
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(Color.Yellow)
                    .WithContent(
                        Layouts.Vertical()
                            .WithChild(_thinkingSpinner.Height(1))
                            .WithChild(new EmptyNode().Height(1))
                            .WithChild(_thinkingIndicator))
                    .Height(6));
    }
}
