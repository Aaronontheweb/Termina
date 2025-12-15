// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Rendering;

/// <summary>
/// Tests for the ScrollableContent component.
/// </summary>
public class ScrollableContentTests
{
    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_DefaultValues()
    {
        var scroll = new ScrollableContent();

        Assert.Equal(0, scroll.ScrollOffset);
        Assert.Equal(0, scroll.ContentHeight);
        Assert.False(scroll.ShowScrollbar);
    }

    [Fact]
    public void ContentLines_SetAndGet()
    {
        var scroll = new ScrollableContent();
        var lines = new[] { "Line 1", "Line 2", "Line 3" };

        scroll.SetContent(lines);

        Assert.Equal(3, scroll.ContentHeight);
    }

    [Fact]
    public void Content_SetRenderable()
    {
        var scroll = new ScrollableContent();
        var text = new Text("Hello\nWorld\nTest");

        scroll.Content = text;

        Assert.Same(text, scroll.Content);
    }

    #endregion

    #region Scrolling Tests

    [Fact]
    public void ScrollDown_IncreasesOffset()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);

        scroll.ScrollDown();

        Assert.Equal(1, scroll.ScrollOffset);
    }

    [Fact]
    public void ScrollDown_StopsAtMaxScroll()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" }); // 5 lines
        scroll.SetViewportHeight(3); // Can see 3, so max scroll = 2

        scroll.ScrollDown();
        scroll.ScrollDown();
        scroll.ScrollDown();
        scroll.ScrollDown();

        Assert.Equal(2, scroll.ScrollOffset); // Clamped to max
    }

    [Fact]
    public void ScrollUp_DecreasesOffset()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);
        scroll.ScrollOffset = 2;

        scroll.ScrollUp();

        Assert.Equal(1, scroll.ScrollOffset);
    }

    [Fact]
    public void ScrollUp_StopsAtZero()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3" });
        scroll.ScrollOffset = 1;

        scroll.ScrollUp();
        scroll.ScrollUp();
        scroll.ScrollUp();

        Assert.Equal(0, scroll.ScrollOffset);
    }

    [Fact]
    public void ScrollTo_SetsOffsetDirectly()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);

        scroll.ScrollTo(2);

        Assert.Equal(2, scroll.ScrollOffset);
    }

    [Fact]
    public void ScrollTo_ClampsToValidRange()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);

        scroll.ScrollTo(100);

        Assert.Equal(2, scroll.ScrollOffset); // Max scroll
    }

    [Fact]
    public void ScrollTo_ClampsNegative()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3" });

        scroll.ScrollTo(-5);

        Assert.Equal(0, scroll.ScrollOffset);
    }

    [Fact]
    public void PageDown_ScrollsFullPage()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(Enumerable.Range(1, 20).Select(i => $"Line {i}").ToArray());
        scroll.SetViewportHeight(5);

        scroll.PageDown();

        Assert.Equal(5, scroll.ScrollOffset);
    }

    [Fact]
    public void PageUp_ScrollsFullPage()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(Enumerable.Range(1, 20).Select(i => $"Line {i}").ToArray());
        scroll.SetViewportHeight(5);
        scroll.ScrollOffset = 10;

        scroll.PageUp();

        Assert.Equal(5, scroll.ScrollOffset);
    }

    [Fact]
    public void ScrollToTop_SetsOffsetToZero()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.ScrollOffset = 3;

        scroll.ScrollToTop();

        Assert.Equal(0, scroll.ScrollOffset);
    }

    [Fact]
    public void ScrollToBottom_SetsOffsetToMax()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);

        scroll.ScrollToBottom();

        Assert.Equal(2, scroll.ScrollOffset);
    }

    #endregion

    #region Render Tests

    [Fact]
    public void Render_ShowsVisibleLines()
    {
        var terminal = new VirtualTerminal(20, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 20, 3);
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "Line 1", "Line 2", "Line 3", "Line 4", "Line 5" });
        scroll.SetViewportHeight(3);

        scroll.Render(context);

        Assert.True(terminal.Contains("Line 1"));
        Assert.True(terminal.Contains("Line 2"));
        Assert.True(terminal.Contains("Line 3"));
        Assert.False(terminal.Contains("Line 4"));
    }

    [Fact]
    public void Render_AfterScroll_ShowsCorrectLines()
    {
        var terminal = new VirtualTerminal(20, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 20, 3);
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "Line 1", "Line 2", "Line 3", "Line 4", "Line 5" });
        scroll.SetViewportHeight(3);
        scroll.ScrollOffset = 2;

        scroll.Render(context);

        Assert.False(terminal.Contains("Line 1"));
        Assert.False(terminal.Contains("Line 2"));
        Assert.True(terminal.Contains("Line 3"));
        Assert.True(terminal.Contains("Line 4"));
        Assert.True(terminal.Contains("Line 5"));
    }

    [Fact]
    public void Render_WithScrollbar_DrawsScrollbar()
    {
        var terminal = new VirtualTerminal(22, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 22, 5);
        var scroll = new ScrollableContent { ShowScrollbar = true };
        scroll.SetContent(Enumerable.Range(1, 20).Select(i => $"Line {i}").ToArray());
        scroll.SetViewportHeight(5);

        scroll.Render(context);

        // Scrollbar should be in the rightmost column
        var hasScrollChars = false;
        for (var y = 0; y < 5; y++)
        {
            var c = terminal.GetChar(21, y);
            if (c == '█' || c == '░' || c == '▓' || c == '│')
            {
                hasScrollChars = true;
                break;
            }
        }
        Assert.True(hasScrollChars);
    }

    [Fact]
    public void Render_EmptyContent_DoesNotCrash()
    {
        var terminal = new VirtualTerminal(20, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 20, 5);
        var scroll = new ScrollableContent();

        // Should not throw
        scroll.Render(context);
    }

    [Fact]
    public void Render_WithRenderable_RendersContent()
    {
        var terminal = new VirtualTerminal(20, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 20, 3);
        var scroll = new ScrollableContent { Content = new Text("Hello\nWorld") };
        scroll.SetViewportHeight(3);

        scroll.Render(context);

        Assert.True(terminal.Contains("Hello"));
        Assert.True(terminal.Contains("World"));
    }

    #endregion

    #region Measure Tests

    [Fact]
    public void Measure_ReturnsContentSize()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "Hello", "World" });

        var (width, height) = scroll.Measure(100, 100);

        Assert.Equal(5, width); // "Hello" is longest
        Assert.Equal(2, height);
    }

    [Fact]
    public void Measure_ClampsToAvailable()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "Hello World", "Test" });

        var (width, height) = scroll.Measure(5, 1);

        Assert.Equal(5, width);
        Assert.Equal(1, height);
    }

    [Fact]
    public void Measure_WithScrollbar_AddsWidth()
    {
        var scroll = new ScrollableContent { ShowScrollbar = true };
        scroll.SetContent(new[] { "Hello" });

        var (width, height) = scroll.Measure(100, 100);

        Assert.Equal(6, width); // 5 + 1 for scrollbar
    }

    #endregion

    #region MaxScroll Tests

    [Fact]
    public void MaxScroll_WhenContentFits_IsZero()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2" });
        scroll.SetViewportHeight(5);

        Assert.Equal(0, scroll.MaxScroll);
    }

    [Fact]
    public void MaxScroll_WhenContentOverflows_IsCorrect()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);

        Assert.Equal(2, scroll.MaxScroll); // 5 - 3 = 2
    }

    [Fact]
    public void CanScrollDown_WhenNotAtBottom_ReturnsTrue()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);
        scroll.ScrollOffset = 0;

        Assert.True(scroll.CanScrollDown);
    }

    [Fact]
    public void CanScrollDown_WhenAtBottom_ReturnsFalse()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3", "4", "5" });
        scroll.SetViewportHeight(3);
        scroll.ScrollOffset = 2;

        Assert.False(scroll.CanScrollDown);
    }

    [Fact]
    public void CanScrollUp_WhenNotAtTop_ReturnsTrue()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3" });
        scroll.ScrollOffset = 1;

        Assert.True(scroll.CanScrollUp);
    }

    [Fact]
    public void CanScrollUp_WhenAtTop_ReturnsFalse()
    {
        var scroll = new ScrollableContent();
        scroll.SetContent(new[] { "1", "2", "3" });
        scroll.ScrollOffset = 0;

        Assert.False(scroll.CanScrollUp);
    }

    #endregion
}
