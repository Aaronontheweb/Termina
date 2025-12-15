// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.V2;

/// <summary>
/// Page for the counter demo.
/// Demonstrates the tree-based declarative layout API with reactive bindings.
/// </summary>
public class CounterPage : ReactivePage<CounterViewModel>
{
    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            // Header panel
            .WithChild(
                new PanelNode()
                    .WithTitle("Termina v2 Demo")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Blue)
                    .WithTitleColor(Color.BrightCyan)
                    .WithContent(
                        new TextNode("Reactive Region-Based Rendering")
                            .WithForeground(Color.Cyan))
                    .Height(3))
            // Counter display - reactive binding
            .WithChild(
                new PanelNode()
                    .WithTitle("Counter")
                    .WithBorder(BorderStyle.Single)
                    .WithBorderColor(Color.Green)
                    .WithContent(
                        ViewModel.CountChanged
                            .Select(count => new TextNode($"Count: {count}")
                                .WithForeground(Color.BrightCyan))
                            .AsLayout())
                    .Height(3))
            // Input panel - reactive binding
            .WithChild(
                new PanelNode()
                    .WithTitle("Input")
                    .WithBorder(BorderStyle.Single)
                    .WithBorderColor(Color.Yellow)
                    .WithContent(
                        ViewModel.InputTextChanged
                            .Select(text => new TextNode($"> {text}_")
                                .WithForeground(Color.White))
                            .AsLayout())
                    .Height(3))
            // Messages panel - reactive binding
            .WithChild(
                new PanelNode()
                    .WithTitle("Messages")
                    .WithBorder(BorderStyle.Single)
                    .WithBorderColor(Color.Magenta)
                    .WithContent(
                        ViewModel.MessagesChanged
                            .Select(messages => new TextNode(messages.Count > 0
                                ? string.Join("\n", messages)
                                : "(no messages yet)")
                                .WithForeground(Color.Gray))
                            .AsLayout())
                    .Fill())
            // Status bar at the bottom - reactive binding
            .WithChild(
                ViewModel.StatusMessageChanged
                    .Select(status => new TextNode(status)
                        .WithForeground(Color.BrightYellow))
                    .AsLayout()
                    .Height(1));
    }
}
