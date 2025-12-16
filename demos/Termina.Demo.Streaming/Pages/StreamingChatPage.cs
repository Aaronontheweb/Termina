// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Streaming.Pages;

/// <summary>
/// Demo page showing streaming text components with LLM simulation.
/// Uses the new declarative layout API.
/// </summary>
public class StreamingChatPage : ReactivePage<StreamingChatViewModel>
{
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
                    .WithContent(ViewModel.ChatHistory)
                    .Fill())
            // Thinking indicator - conditionally shown
            .WithChild(
                ViewModel.IsGeneratingChanged
                    .Select(isGenerating => isGenerating && ViewModel.ThinkingIndicator.Buffer.HasContent
                        ? BuildThinkingPanel()
                        : (ILayoutNode)new EmptyNode())
                    .AsLayout())
            .WithChild(new EmptyNode().Height(1))
            // Input panel
            .WithChild(
                ViewModel.IsGeneratingChanged
                    .Select(isGenerating => BuildInputPanel(isGenerating))
                    .AsLayout()
                    .Height(3))
            // Status bar with dynamic hints
            .WithChild(
                ViewModel.IsGeneratingChanged
                    .Select(isGenerating => new TextNode(
                        isGenerating
                            ? "[Esc] Cancel generation [↑/↓] Scroll [Ctrl+Q] Quit"
                            : "[Enter] Send [↑/↓] Scroll [←/→] Edit [Esc] Clear/Quit [Ctrl+Q] Quit")
                        .WithForeground(Color.BrightBlack))
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
                    .WithContent(ViewModel.ThinkingIndicator)
                    .Height(5));
    }

    private ILayoutNode BuildInputPanel(bool isGenerating)
    {
        var borderColor = isGenerating ? Color.Gray : Color.Cyan;
        return new PanelNode()
            .WithTitle("Your Prompt")
            .WithTitleColor(Color.Cyan)
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(borderColor)
            .WithContent(ViewModel.PromptInput);
    }
}
