// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Extensions;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// Gallery page showcasing layout components.
/// </summary>
public class LayoutGalleryPage : ReactivePage<LayoutGalleryViewModel>
{
    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Page-level key binding for navigation (capture phase)
        KeyBindings.Register(ConsoleKey.Escape, () => ViewModel.Navigate("/menu"));
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Layout Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Green)
                    .WithContent(BuildLayoutShowcase())
                    .Fill())
            .WithChild(
                new TextNode("[Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .Height(1));
    }

    private static ILayoutNode BuildLayoutShowcase()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Percentage(50), SizeConstraint.Percentage(50))
            .WithRows(SizeConstraint.Percentage(50), SizeConstraint.Percentage(50))
            .WithGridLines(BorderStyle.Single)
            .WithGridLineColor(Color.BrightBlack);

        // Top-left: Vertical layout
        grid.SetCell(0, 0,
            new PanelNode()
                .WithTitle("VerticalLayout")
                .WithBorder(BorderStyle.Rounded)
                .WithBorderColor(Color.Cyan)
                .WithContent(
                    Layouts.Vertical()
                        .WithChild(new TextNode("Item 1").WithForeground(Color.White).Height(1))
                        .WithChild(new TextNode("Item 2").WithForeground(Color.Gray).Height(1))
                        .WithChild(new TextNode("Item 3").WithForeground(Color.BrightBlack).Height(1))));

        // Top-right: Horizontal layout
        grid.SetCell(0, 1,
            new PanelNode()
                .WithTitle("HorizontalLayout")
                .WithBorder(BorderStyle.Rounded)
                .WithBorderColor(Color.Yellow)
                .WithContent(
                    Layouts.Horizontal()
                        .WithChild(new TextNode("[A]").WithForeground(Color.Red).WidthAuto())
                        .WithChild(new TextNode(" [B] ").WithForeground(Color.Green).WidthAuto())
                        .WithChild(new TextNode("[C]").WithForeground(Color.Blue).WidthAuto())));

        // Bottom-left: Panel styles
        grid.SetCell(1, 0,
            Layouts.Vertical()
                .WithChild(
                    new PanelNode()
                        .WithTitle("Single Border")
                        .WithBorder(BorderStyle.Single)
                        .WithBorderColor(Color.Magenta)
                        .WithContent(new TextNode("Content").WithForeground(Color.Gray)))
                .WithChild(
                    new PanelNode()
                        .WithTitle("Double Border")
                        .WithBorder(BorderStyle.Double)
                        .WithBorderColor(Color.Blue)
                        .WithContent(new TextNode("Content").WithForeground(Color.Gray))));

        // Bottom-right: Nested layout
        grid.SetCell(1, 1,
            new PanelNode()
                .WithTitle("Nested Layouts")
                .WithBorder(BorderStyle.Rounded)
                .WithBorderColor(Color.Green)
                .WithContent(
                    Layouts.Vertical()
                        .WithChild(
                            Layouts.Horizontal()
                                .WithChild(new TextNode("Left").WithForeground(Color.Cyan).Fill())
                                .WithChild(new TextNode("Right").WithForeground(Color.Yellow).Fill())
                                .Height(1))
                        .WithChild(
                            new TextNode("Full Width")
                                .WithForeground(Color.Magenta)
                                .Height(1))));

        return grid;
    }
}
