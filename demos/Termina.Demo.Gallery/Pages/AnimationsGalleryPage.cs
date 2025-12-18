// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;
using LayoutSpinnerStyle = Termina.Layout.SpinnerStyle;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// Gallery page showcasing animated components.
/// </summary>
public class AnimationsGalleryPage : ReactivePage<AnimationsGalleryViewModel>
{
    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Animations Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Yellow)
                    .WithContent(BuildAnimationsShowcase())
                    .Fill())
            .WithChild(
                new TextNode("[Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .Height(1));
    }

    private static ILayoutNode BuildAnimationsShowcase()
    {
        return Layouts.Vertical()
            .WithChild(
                new TextNode("\n  Spinner Styles\n")
                    .WithForeground(Color.BrightCyan)
                    .Bold())
            .WithChild(BuildSpinnerRow("Dots", LayoutSpinnerStyle.Dots, Color.Blue))
            .WithChild(BuildSpinnerRow("Line", LayoutSpinnerStyle.Line, Color.Green))
            .WithChild(BuildSpinnerRow("Arrow", LayoutSpinnerStyle.Arrow, Color.Yellow))
            .WithChild(BuildSpinnerRow("Bounce", LayoutSpinnerStyle.Bounce, Color.Magenta))
            .WithChild(BuildSpinnerRow("Box", LayoutSpinnerStyle.Box, Color.Cyan))
            .WithChild(BuildSpinnerRow("Circle", LayoutSpinnerStyle.Circle, Color.Red))
            .WithChild(new TextNode(" ").Height(1))
            .WithChild(
                new TextNode("  Spinners with Labels\n")
                    .WithForeground(Color.BrightCyan)
                    .Bold())
            .WithChild(
                new SpinnerNode(LayoutSpinnerStyle.Dots, intervalMs: 80)
                    .WithLabel("Loading data...")
                    .WithSpinnerColor(Color.Blue)
                    .WithLabelColor(Color.White))
            .WithChild(
                new SpinnerNode(LayoutSpinnerStyle.Arrow, intervalMs: 100)
                    .WithLabel("Processing request...")
                    .WithSpinnerColor(Color.Green)
                    .WithLabelColor(Color.White));
    }

    private static ILayoutNode BuildSpinnerRow(string name, LayoutSpinnerStyle style, Color color)
    {
        return Layouts.Horizontal()
            .WithChild(new TextNode($"  {name,-12}").WithForeground(Color.Gray).Width(14))
            .WithChild(new SpinnerNode(style, intervalMs: 100).WithSpinnerColor(color))
            .Height(1);
    }
}
