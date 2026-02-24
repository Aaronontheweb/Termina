// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests;

/// <summary>
/// Tests for mouse wheel scroll support via <see cref="IScrollable"/> and <see cref="MouseScrollEvent"/>.
/// </summary>
public class MouseScrollRoutingTests
{
    [Fact]
    public void StreamingTextNode_ImplementsIScrollable()
    {
        var node = StreamingTextNode.Create();
        Assert.IsAssignableFrom<IScrollable>(node);
    }

    [Fact]
    public void CanScrollUp_False_BeforeFirstRender_WithNoContent()
    {
        var node = StreamingTextNode.Create();
        Assert.False(node.CanScrollUp);
    }

    [Fact]
    public void CanScrollDown_False_WhenAtBottom()
    {
        var node = StreamingTextNode.Create();
        for (var i = 0; i < 30; i++)
            node.AppendLine($"Line {i}");

        var terminal = new VirtualTerminal(40, 10);
        var context = new RegionRenderContext(terminal, 0, 0, 40, 10);
        node.Render(context, new Rect(0, 0, 40, 10));

        // At the bottom (scrollOffset=0) → cannot scroll further down
        Assert.False(node.CanScrollDown);
    }

    [Fact]
    public void CanScrollUp_True_WhenContentExceedsViewport()
    {
        var node = StreamingTextNode.Create();
        for (var i = 0; i < 30; i++)
            node.AppendLine($"Line {i}");

        var terminal = new VirtualTerminal(40, 10);
        var context = new RegionRenderContext(terminal, 0, 0, 40, 10);
        node.Render(context, new Rect(0, 0, 40, 10));

        // 30 lines in a 10-row viewport → can scroll up to older content
        Assert.True(node.CanScrollUp);
    }

    [Fact]
    public void CanScrollDown_True_AfterScrollingUp()
    {
        var node = StreamingTextNode.Create();
        for (var i = 0; i < 30; i++)
            node.AppendLine($"Line {i}");

        var terminal = new VirtualTerminal(40, 10);
        var context = new RegionRenderContext(terminal, 0, 0, 40, 10);
        node.Render(context, new Rect(0, 0, 40, 10));

        // Scroll up first
        node.ScrollUp(5, 40);
        node.Render(context, new Rect(0, 0, 40, 10));

        // After scrolling up, can scroll back down
        Assert.True(node.CanScrollDown);
    }

    [Fact]
    public void IScrollable_ScrollUp_UsesLastViewportWidth()
    {
        var node = StreamingTextNode.Create();
        for (var i = 0; i < 30; i++)
            node.AppendLine($"Line {i}");

        var terminal = new VirtualTerminal(40, 10);
        var context = new RegionRenderContext(terminal, 0, 0, 40, 10);
        node.Render(context, new Rect(0, 0, 40, 10));

        // ScrollUp via IScrollable interface
        IScrollable scrollable = node;
        scrollable.ScrollUp(3);

        // Verify we scrolled (CanScrollDown should now be true)
        node.Render(context, new Rect(0, 0, 40, 10));
        Assert.True(node.CanScrollDown);
    }

    [Fact]
    public void IScrollable_ScrollDown_DecreasesScrollOffset()
    {
        var node = StreamingTextNode.Create();
        for (var i = 0; i < 30; i++)
            node.AppendLine($"Line {i}");

        var terminal = new VirtualTerminal(40, 10);
        var context = new RegionRenderContext(terminal, 0, 0, 40, 10);
        node.Render(context, new Rect(0, 0, 40, 10));

        // Scroll up then back down via interface
        IScrollable scrollable = node;
        scrollable.ScrollUp(5);
        node.Render(context, new Rect(0, 0, 40, 10));
        Assert.True(node.CanScrollDown);

        scrollable.ScrollDown(5);
        node.Render(context, new Rect(0, 0, 40, 10));
        Assert.False(node.CanScrollDown);
    }

    [Fact]
    public void MouseScrollEvent_PositiveDelta_MeansScrollUp()
    {
        // Verify that positive Delta represents scroll up (toward older content)
        var evt = new MouseScrollEvent(+1);
        Assert.True(evt.Delta > 0);
    }

    [Fact]
    public void MouseScrollEvent_NegativeDelta_MeansScrollDown()
    {
        var evt = new MouseScrollEvent(-1);
        Assert.True(evt.Delta < 0);
    }
}
