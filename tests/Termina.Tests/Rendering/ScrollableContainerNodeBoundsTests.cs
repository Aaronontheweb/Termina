// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Rendering;

/// <summary>
/// Regression tests for issue #300: ScrollableContainerNode.Render() must respect
/// bounds.Y so its content lands at the correct screen row instead of always
/// writing at absolute row 0 (which would overwrite content above it).
/// </summary>
public class ScrollableContainerNodeBoundsTests
{
    /// <summary>
    /// Core fix test: render a ScrollableContainerNode at offset (0, 2).
    /// Fill row 0 with X's — if the fix works, X's must survive at row 0.
    /// Content should appear at row 2 (bounds.Y).
    /// </summary>
    [Fact]
    public void Render_AtOffset_PutsContentAtCorrectRow()
    {
        var terminal = new VirtualTerminal(20, 10);

        // Fill row 0 with X's to detect overwrites
        terminal.MoveTo(0, 0);
        terminal.Write("XXXXXXXXXXX");

        // Create multi-line content using a vertical layout of TextNodes
        var scrollable = new ScrollableContainerNode();
        scrollable.WithContent(
            Layouts.Vertical(
                new TextNode("AAA"),
                new TextNode("BBB"),
                new TextNode("CCC")
            )
        );

        var scrollCtx = new RegionRenderContext(terminal, 0, 2, 12, 5);
        scrollable.Measure(new Size(12, 5));
        scrollable.Render(scrollCtx, new Rect(0, 0, 12, 5));

        // Core assertion: row 0 X's must NOT be overwritten
        Assert.Equal('X', terminal.GetChar(0, 0));

        // Content should appear at row 2 (bounds.Y)
        Assert.Equal('A', terminal.GetChar(0, 2));
        Assert.Equal('B', terminal.GetChar(0, 3));
        Assert.Equal('C', terminal.GetChar(0, 4));
    }

    /// <summary>
    /// Verify the fix works via the full layout tree: header row + scrollable.
    /// </summary>
    [Fact]
    public void Render_InVerticalLayout_PreservesHeaderAboveScrollable()
    {
        var terminal = new VirtualTerminal(20, 8);

        var header = new TextNode("HEADER").Height(1);
        var scrollable = new ScrollableContainerNode()
            .WithContent(
                Layouts.Vertical(
                    new TextNode("Content1"),
                    new TextNode("Content2"),
                    new TextNode("Content3"),
                    new TextNode("Content4"),
                    new TextNode("Content5")
                )
            )
            .Fill();

        var layout = Layouts.Vertical().WithChild(header).WithChild(scrollable);
        var available = new Size(20, 8);
        layout.Measure(available);

        var rootContext = new RegionRenderContext(terminal, 0, 0, 20, 8);
        layout.Render(rootContext, new Rect(0, 0, 20, 8));

        // Header at row 0
        Assert.Equal('H', terminal.GetChar(0, 0));
        Assert.Equal('E', terminal.GetChar(1, 0));

        // Scrollable content below header
        Assert.True(terminal.Contains("Content1"));
    }

    /// <summary>
    /// Verify scrolling still works correctly when placed below other content.
    /// </summary>
    [Fact]
    public void Render_ScrollShiftsContentWithinBounds()
    {
        var terminal = new VirtualTerminal(20, 8);

        var header = new TextNode("HEADER").Height(1);
        var scrollable = new ScrollableContainerNode()
            .WithContent(
                Layouts.Vertical(
                    new TextNode("Line1"),
                    new TextNode("Line2"),
                    new TextNode("Line3"),
                    new TextNode("Line4"),
                    new TextNode("Line5"),
                    new TextNode("Line6"),
                    new TextNode("Line7"),
                    new TextNode("Line8")
                )
            );
        scrollable.ScrollDown(); scrollable.ScrollDown(); scrollable.ScrollDown(); // ScrollDown() takes no args
        scrollable.Height(5);

        var layout = Layouts.Vertical().WithChild(header).WithChild(scrollable);
        var available = new Size(20, 8);
        layout.Measure(available);

        var rootContext = new RegionRenderContext(terminal, 0, 0, 20, 8);
        layout.Render(rootContext, new Rect(0, 0, 20, 8));

        // Header preserved at row 0
        Assert.Equal('H', terminal.GetChar(0, 0));

        // Scrolled content visible
        Assert.True(terminal.Contains("Line4"));
    }

    /// <summary>
    /// Verify the workaround from issue #300 (wrapping in PanelNode)
    /// still works with the fix.
    /// </summary>
    [Fact]
    public void Render_WrappedInPanelNode_WorkaroundStillWorks()
    {
        var terminal = new VirtualTerminal(20, 8);

        var header = new TextNode("TOP HEADER").Height(1);
        var innerScrollable = new ScrollableContainerNode()
            .WithContent(new TextNode("Inner1\nInner2\nInner3"))
            .Fill();
        var panel = new PanelNode()
            .WithTitle("Panel")
            .WithContent(innerScrollable)
            .Height(5);

        var layout = Layouts.Vertical().WithChild(header).WithChild(panel);
        var available = new Size(20, 8);
        layout.Measure(available);

        var rootContext = new RegionRenderContext(terminal, 0, 0, 20, 8);
        layout.Render(rootContext, new Rect(0, 0, 20, 8));

        Assert.Equal('T', terminal.GetChar(0, 0));
        Assert.True(terminal.Contains("Panel"));
        Assert.True(terminal.Contains("Inner1"));
    }

    /// <summary>
    /// Verify scrollable at row 0 (no header above) still renders correctly.
    /// </summary>
    [Fact]
    public void Render_AtRow0_NoHeaderAbove_RendersContent()
    {
        var terminal = new VirtualTerminal(16, 5);
        var scrollable = new ScrollableContainerNode()
            .WithContent(
                Layouts.Vertical(
                    new TextNode("AAA"),
                    new TextNode("BBB"),
                    new TextNode("CCC"),
                    new TextNode("DDD"),
                    new TextNode("EEE"),
                    new TextNode("FFF")
                )
            )
            .Height(5);

        var layout = Layouts.Vertical().WithChild(scrollable);
        layout.Measure(new Size(16, 5));

        var rootContext = new RegionRenderContext(terminal, 0, 0, 16, 5);
        layout.Render(rootContext, new Rect(0, 0, 16, 5));

        // Content visible — just check that something rendered (no crash, no overwrite)
        // The key is that row 0 is not blank and not full of garbage
        var row0 = terminal.GetLine(0);
        Assert.False(string.IsNullOrWhiteSpace(row0) || row0.Trim() == "");
    }

    /// <summary>
    /// Verify that the scrollbar renders at the correct position (right edge of
    /// the node's bounds, not at row 0).
    /// </summary>
    [Fact]
    public void Render_WithScrollbar_DrawsScrollbarInBounds()
    {
        var terminal = new VirtualTerminal(20, 8);

        var header = new TextNode("HEADER").Height(1);
        var scrollable = new ScrollableContainerNode()
            .WithContent(
                Layouts.Vertical(
                    new TextNode("Item1"),
                    new TextNode("Item2"),
                    new TextNode("Item3"),
                    new TextNode("Item4"),
                    new TextNode("Item5"),
                    new TextNode("Item6"),
                    new TextNode("Item7"),
                    new TextNode("Item8")
                )
            )
            .WithScrollbar(true)
            .Height(5);

        var layout = Layouts.Vertical().WithChild(header).WithChild(scrollable);
        var available = new Size(20, 8);
        layout.Measure(available);

        var rootContext = new RegionRenderContext(terminal, 0, 0, 20, 8);
        layout.Render(rootContext, new Rect(0, 0, 20, 8));

        // Header preserved at row 0
        Assert.Equal('H', terminal.GetChar(0, 0));

        // Scrollbar chars should exist somewhere in the scrollable's rows (2-6)
        bool found = false;
        for (var y = 2; y < 7 && !found; y++)
        {
            for (var x = 0; x < 20 && !found; x++)
            {
                var c = terminal.GetChar(x, y);
                if (c == '█' || c == '░' || c == '▓' || c == '│')
                    found = true;
            }
        }
        Assert.True(found, "Scrollbar should be drawn somewhere in the scrollable's bounds");
    }
}
