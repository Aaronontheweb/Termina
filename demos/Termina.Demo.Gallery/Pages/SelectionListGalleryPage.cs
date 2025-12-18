// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Components.Streaming;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// Gallery page showcasing SelectionListNode capabilities.
/// Demonstrates:
/// - 12+ items to show numbering works beyond 9 (Issue #101 fix)
/// - "Other" option with fixed UX (Issue #102 fix)
/// - Both single and multi-select modes
/// - Rich multi-line content with styling
/// </summary>
public class SelectionListGalleryPage : ReactivePage<SelectionListGalleryViewModel>
{
    private SelectionListNode<string> _singleSelectList = null!;
    private SelectionListNode<ServerInfo> _multiSelectRichList = null!;
    private SelectionListNode<string> _numberedList = null!;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Page-level key bindings (capture phase - intercepts before focused components)
        // This is the new recommended pattern for page navigation keys
        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));
        KeyBindings.Register(ConsoleKey.Tab, CycleFocus);

        // Subscribe to selection events
        _singleSelectList.SelectionConfirmed
            .Subscribe(items => ViewModel.OnSingleSelection(items.FirstOrDefault() ?? ""))
            .DisposeWith(Subscriptions);

        _singleSelectList.OtherSelected
            .Subscribe(text => ViewModel.OnOtherSelected(text))
            .DisposeWith(Subscriptions);

        _multiSelectRichList.SelectionConfirmed
            .Subscribe(items => ViewModel.OnMultiSelection(items.Select(s => s.Name).ToList()))
            .DisposeWith(Subscriptions);

        _numberedList.SelectionConfirmed
            .Subscribe(items => ViewModel.OnNumberedSelection(items.FirstOrDefault() ?? ""))
            .DisposeWith(Subscriptions);

        // Default focus to first list
        _focusedListIndex = 0;
        Focus.PushFocus(_singleSelectList);
    }

    private int _focusedListIndex;

    private void CycleFocus()
    {
        _focusedListIndex = (_focusedListIndex + 1) % 3;
        IFocusable targetList = _focusedListIndex switch
        {
            0 => _singleSelectList,
            1 => _multiSelectRichList,
            2 => _numberedList,
            _ => _singleSelectList
        };
        Focus.PushFocus(targetList);
        ViewModel.OnFocusChanged(_focusedListIndex);
    }

    public override ILayoutNode BuildLayout()
    {
        // Single-select list with "Other" option - demonstrates Issue #102 fix
        _singleSelectList = Layouts.SelectionList(
            "Option Alpha", "Option Beta", "Option Gamma", "Option Delta")
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Green)
            .WithOtherOption("Custom value...");

        // Multi-select list with rich content
        _multiSelectRichList = new SelectionListNode<ServerInfo>(
            ViewModel.Servers,
            server => new SelectionItemContent()
                .AddLine(
                    new StaticTextSegment(server.Name, Color.White, decoration: TextDecoration.Bold),
                    new StaticTextSegment($" ({server.Region})", Color.Gray))
                .AddLine(
                    new StaticTextSegment("   Status: ", Color.Gray),
                    new StaticTextSegment(server.Status, GetStatusColor(server.Status)),
                    new StaticTextSegment($"  |  Load: {server.Load}%", Color.Gray)))
            .WithMode(SelectionMode.Multi)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Yellow)
            .WithVisibleRows(8);

        // Scrolling list with 12 items in 6 visible rows
        _numberedList = Layouts.SelectionList(
            Enumerable.Range(1, 12).Select(i => $"Item number {i}"))
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan)
            .WithVisibleRows(6);

        return Layouts.Vertical()
            .WithChild(BuildHeader())
            .WithChild(BuildDemoGrid().Fill())
            .WithChild(BuildStatusBar());
    }

    private static ILayoutNode BuildHeader()
    {
        return new PanelNode()
            .WithTitle("SelectionListNode Gallery")
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(Color.Magenta)
            .WithContent(
                new TextNode("Showcasing SelectionListNode: numbering beyond 9, fixed 'Other' UX, and rich content")
                    .WithForeground(Color.Gray))
            .Height(4);
    }

    private GridNode BuildDemoGrid()
    {
        var grid = new GridNode()
            .WithColumns(
                SizeConstraint.Percentage(33),
                SizeConstraint.Percentage(34),
                SizeConstraint.Percentage(33))
            .WithRows(SizeConstraint.FillRemaining())
            .WithGridLines(BorderStyle.Single)
            .WithGridLineColor(Color.BrightBlack);

        // Column 1: Single-select with Other
        grid.SetCell(0, 0,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Single Select + Other")
                        .WithForeground(Color.BrightGreen)
                        .Bold()
                        .Height(1))
                .WithChild(
                    new TextNode("Press Enter/Space to edit 'Other'")
                        .WithForeground(Color.DarkGray)
                        .Height(2))
                .WithChild(_singleSelectList));

        // Column 2: Multi-select with rich content
        grid.SetCell(0, 1,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Multi-Select + Rich Content")
                        .WithForeground(Color.BrightYellow)
                        .Bold()
                        .Height(1))
                .WithChild(
                    new TextNode("Space to toggle, Enter to confirm")
                        .WithForeground(Color.DarkGray)
                        .Height(2))
                .WithChild(_multiSelectRichList));

        // Column 3: Scrolling numbered list (12 items)
        grid.SetCell(0, 2,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Scrolling List")
                        .WithForeground(Color.BrightCyan)
                        .Bold()
                        .Height(1))
                .WithChild(
                    new TextNode("12 items in 6 visible rows")
                        .WithForeground(Color.DarkGray)
                        .Height(2))
                .WithChild(_numberedList));

        return grid;
    }

    private ILayoutNode BuildStatusBar()
    {
        return Layouts.Horizontal()
            .WithChild(
                ViewModel.StatusMessageChanged
                    .Select(msg => new TextNode(msg).WithForeground(Color.White))
                    .AsLayout()
                    .Fill())
            .WithChild(
                new TextNode("[Tab] Switch List  [Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .NoWrap()
                    .WidthAuto())
            .Height(1);
    }

    private static Color GetStatusColor(string status) => status switch
    {
        "Online" => Color.Green,
        "Degraded" => Color.Yellow,
        "Offline" => Color.Red,
        _ => Color.Gray
    };
}

/// <summary>
/// Represents a server for the multi-select demo.
/// </summary>
public record ServerInfo(string Name, string Region, string Status, int Load);
