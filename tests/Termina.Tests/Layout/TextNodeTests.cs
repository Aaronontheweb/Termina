// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for the TextNode layout component.
/// </summary>
public class TextNodeTests
{
    [Fact]
    public void Constructor_SetsContent()
    {
        var node = new TextNode("Hello");

        Assert.Equal("Hello", node.Content);
    }

    [Fact]
    public void WordWrap_DefaultsToTrue()
    {
        var node = new TextNode("Hello");

        Assert.True(node.WordWrap);
    }

    [Fact]
    public void NoWrap_DisablesWordWrap()
    {
        var node = new TextNode("Hello").NoWrap();

        Assert.False(node.WordWrap);
    }

    [Fact]
    public void Measure_WithWordWrap_CalculatesWrappedHeight()
    {
        // "Hello World" is 11 chars, with width of 6, it wraps to 2 lines
        var node = new TextNode("Hello World");

        var size = node.Measure(new Size(6, 100));

        Assert.Equal(6, size.Width);
        Assert.Equal(2, size.Height); // "Hello" and "World"
    }

    [Fact]
    public void Measure_WithoutWordWrap_CalculatesOriginalHeight()
    {
        var node = new TextNode("Hello World").NoWrap();

        var size = node.Measure(new Size(6, 100));

        Assert.Equal(6, size.Width);
        Assert.Equal(1, size.Height); // Single line (no wrapping)
    }

    [Fact]
    public void Measure_MultilineWithWordWrap_CalculatesTotalWrappedHeight()
    {
        // Two lines: "Hello World" (wraps to 2) + "Test" (stays 1) = 3 lines at width 6
        var node = new TextNode("Hello World\nTest");

        var size = node.Measure(new Size(6, 100));

        Assert.Equal(6, size.Width);
        Assert.Equal(3, size.Height);
    }

    [Fact]
    public void Render_WithWordWrap_WrapsTextToMultipleLines()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var node = new TextNode("Hello World");

        // Render with width of 6, should wrap to "Hello" and "World"
        node.Render(context, new Rect(0, 0, 6, 10));

        Assert.Equal("Hello", terminal.GetLine(0).TrimEnd());
        Assert.Equal("World", terminal.GetLine(1).TrimEnd());
    }

    [Fact]
    public void Render_WithoutWordWrap_TruncatesText()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var node = new TextNode("Hello World").NoWrap();

        // Render with width of 5, should truncate to "Hello" (no space for the space)
        node.Render(context, new Rect(0, 0, 5, 10));

        // With NoWrap, text is truncated to bounds width
        Assert.Equal("Hello", terminal.GetLine(0).TrimEnd());
        // Line 1 should be empty (no wrapping)
        Assert.Equal("", terminal.GetLine(1).TrimEnd());
    }

    [Fact]
    public void Render_LongWordThatExceedsWidth_BreaksWord()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var node = new TextNode("Supercalifragilisticexpialidocious");

        // Width of 10 should break the long word
        node.Render(context, new Rect(0, 0, 10, 10));

        Assert.Equal("Supercalif", terminal.GetLine(0).Substring(0, 10));
        Assert.Equal("ragilistic", terminal.GetLine(1).Substring(0, 10));
    }

    [Fact]
    public void Render_RespectsHeightBounds()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        // This will wrap to many lines at width 5
        var node = new TextNode("Hello World Test");

        // Only allow 2 lines of height
        node.Render(context, new Rect(0, 0, 5, 2));

        Assert.Equal("Hello", terminal.GetLine(0).TrimEnd());
        Assert.Equal("World", terminal.GetLine(1).TrimEnd());
        // Line 2 should be empty because height is constrained
        Assert.Equal("", terminal.GetLine(2).TrimEnd());
    }

    [Fact]
    public void FluentMethods_ReturnSameInstance()
    {
        var node = new TextNode("Hello");

        var result = node
            .WithForeground(Color.Red)
            .WithBackground(Color.Blue)
            .Bold()
            .Italic()
            .Underline()
            .NoWrap();

        Assert.Same(node, result);
    }
}
