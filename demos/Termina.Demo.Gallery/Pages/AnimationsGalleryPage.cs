// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Components.Streaming;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;
using LayoutSpinnerStyle = Termina.Layout.SpinnerStyle;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// Gallery page showcasing animated components.
/// Demonstrates interactive spinner style selection.
/// </summary>
public class AnimationsGalleryPage : ReactivePage<AnimationsGalleryViewModel>
{
    private SelectionListNode<SpinnerStyleItem> _styleList = null!;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Page-level key binding for navigation (capture phase)
        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));

        // Spacebar also updates the preview (same as Enter for this single-select use case)
        KeyBindings.Register(ConsoleKey.Spacebar, () =>
        {
            var highlighted = _styleList.HighlightedItem;
            if (highlighted != null)
            {
                ViewModel.SelectedStyle.Value = highlighted.Value.Style;
            }
        });

        // When user selects a style with Enter, update the ViewModel
        _styleList.SelectionConfirmed
            .Subscribe(items =>
            {
                var item = items.FirstOrDefault();
                if (item != null)
                {
                    ViewModel.SelectedStyle.Value = item.Style;
                }
            })
            .DisposeWith(Subscriptions);

        Focus.PushFocus(_styleList);
    }

    public override ILayoutNode BuildLayout()
    {
        _styleList = new SelectionListNode<SpinnerStyleItem>(
            ViewModel.SpinnerStyles,
            item => new SelectionItemContent()
                .AddLine(
                    new StaticTextSegment(item.Name, item.Color, decoration: TextDecoration.Bold),
                    new StaticTextSegment($"  {item.Description}", Color.Gray)))
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan)
            .WithVisibleRows(8);

        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Animations Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Yellow)
                    .WithContent(BuildContent())
                    .Fill())
            .WithChild(
                new TextNode("[↑/↓] Navigate  [Enter] Preview Style  [Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .Height(1));
    }

    private ILayoutNode BuildContent()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Percentage(40), SizeConstraint.Percentage(60))
            .WithRows(SizeConstraint.FillRemaining())
            .WithGridLines(BorderStyle.Single)
            .WithGridLineColor(Color.BrightBlack);

        // Left column: Style selector
        grid.SetCell(0, 0,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Select Spinner Style")
                        .WithForeground(Color.BrightCyan)
                        .Bold()
                        .Height(2))
                .WithChild(_styleList));

        // Right column: Live preview that updates when selection changes
        grid.SetCell(0, 1,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Live Preview")
                        .WithForeground(Color.BrightYellow)
                        .Bold()
                        .Height(2))
                .WithChild(BuildPreviewArea()));

        return grid;
    }

    private ILayoutNode BuildPreviewArea()
    {
        // Create a reactive layout that swaps spinners based on selection
        return ViewModel.SelectedStyle
            .Select(style => BuildSpinnerPreview(style))
            .AsLayout(RenderFrameProvider)
            .Fill();
    }

    private ILayoutNode BuildSpinnerPreview(LayoutSpinnerStyle style)
    {
        var (name, color, _) = GetStyleInfo(style);

        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle($"Style: {name}")
                    .WithBorder(BorderStyle.Rounded)
                    .WithBorderColor(color)
                    .WithContent(
                        Layouts.Vertical()
                            .WithChild(new TextNode(" ").Height(1))
                            .WithChild(
                                Layouts.Horizontal()
                                    .WithChild(new TextNode("  ").WidthAuto())
                                    .WithChild(
                                        new SpinnerNode(style, intervalMs: 80)
                                            .WithSpinnerColor(color))
                                    .Height(1))
                            .WithChild(new TextNode(" ").Height(1))
                            .WithChild(
                                Layouts.Horizontal()
                                    .WithChild(new TextNode("  ").WidthAuto())
                                    .WithChild(
                                        new SpinnerNode(style, intervalMs: 80)
                                            .WithLabel("Loading...")
                                            .WithSpinnerColor(color)
                                            .WithLabelColor(Color.White))
                                    .Height(1))
                            .WithChild(new TextNode(" ").Height(1))
                            .WithChild(
                                Layouts.Horizontal()
                                    .WithChild(new TextNode("  ").WidthAuto())
                                    .WithChild(
                                        new SpinnerNode(style, intervalMs: 120)
                                            .WithLabel("Processing request...")
                                            .WithSpinnerColor(color)
                                            .WithLabelColor(Color.Gray))
                                    .Height(1)))
                    .Fill())
            .WithChild(
                new TextNode($"  Interval: 80ms (fast), 120ms (slow)")
                    .WithForeground(Color.DarkGray)
                    .Height(2));
    }

    private static (string Name, Color Color, string Description) GetStyleInfo(LayoutSpinnerStyle style) => style switch
    {
        LayoutSpinnerStyle.Dots => ("Dots", Color.Blue, "Braille dots pattern"),
        LayoutSpinnerStyle.Line => ("Line", Color.Green, "Classic line spinner"),
        LayoutSpinnerStyle.Arrow => ("Arrow", Color.Yellow, "Rotating arrow"),
        LayoutSpinnerStyle.Bounce => ("Bounce", Color.Magenta, "Bouncing dot"),
        LayoutSpinnerStyle.Box => ("Box", Color.Cyan, "Rotating box corners"),
        LayoutSpinnerStyle.Circle => ("Circle", Color.Red, "Rotating circle"),
        _ => ("Unknown", Color.White, "")
    };
}

/// <summary>
/// Represents a spinner style option.
/// </summary>
public record SpinnerStyleItem(string Name, LayoutSpinnerStyle Style, Color Color, string Description);
