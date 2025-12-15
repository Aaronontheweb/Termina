// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Rendering;

/// <summary>
/// Tests for the Text component.
/// </summary>
public class TextTests
{
    [Fact]
    public void Constructor_SetsContent()
    {
        var text = new Text("Hello");

        Assert.Equal("Hello", text.Content);
    }

    [Fact]
    public void Render_WritesTextToContext()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var text = new Text("Hello World");

        text.Render(context);

        Assert.True(terminal.Contains("Hello World"));
    }

    [Fact]
    public void Render_AppliesForegroundColor()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var text = new Text("X") { Foreground = Color.Red };

        text.Render(context);

        Assert.Equal(Color.Red, terminal.GetForeground(0, 0));
    }

    [Fact]
    public void Render_AppliesBackgroundColor()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var text = new Text("X") { Background = Color.Blue };

        text.Render(context);

        Assert.Equal(Color.Blue, terminal.GetBackground(0, 0));
    }

    [Fact]
    public void Render_HandlesMultipleLines()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var text = new Text("Line1\nLine2\nLine3");

        text.Render(context);

        Assert.True(terminal.Contains("Line1"));
        Assert.True(terminal.Contains("Line2"));
        Assert.True(terminal.Contains("Line3"));
    }

    [Fact]
    public void Render_TruncatesLinesToContextWidth()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 5, 1);
        var text = new Text("Hello World");

        text.Render(context);

        Assert.Equal("Hello", terminal.GetLine(0));
    }

    [Fact]
    public void Render_StopsAtContextHeight()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 2);
        var text = new Text("Line1\nLine2\nLine3\nLine4");

        text.Render(context);

        Assert.True(terminal.Contains("Line1"));
        Assert.True(terminal.Contains("Line2"));
        Assert.False(terminal.Contains("Line3"));
    }

    [Fact]
    public void Render_EmptyContent_DoesNothing()
    {
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        var text = new Text("");

        text.Render(context);

        Assert.Equal("", terminal.GetLine(0));
    }

    [Fact]
    public void Measure_ReturnsCorrectDimensions()
    {
        var text = new Text("Hello\nWorld!");

        var (width, height) = text.Measure(100, 100);

        Assert.Equal(6, width);  // "World!" is longer
        Assert.Equal(2, height);
    }

    [Fact]
    public void Measure_ClampsToAvailableSpace()
    {
        var text = new Text("Hello World");

        var (width, height) = text.Measure(5, 1);

        Assert.Equal(5, width);
        Assert.Equal(1, height);
    }

    [Fact]
    public void Measure_EmptyContent_ReturnsZero()
    {
        var text = new Text("");

        var (width, height) = text.Measure(100, 100);

        Assert.Equal(0, width);
        Assert.Equal(0, height);
    }

    [Fact]
    public void Colored_CreatesForegroundColoredText()
    {
        var text = Text.Colored("Hello", Color.Green);

        Assert.Equal("Hello", text.Content);
        Assert.Equal(Color.Green, text.Foreground);
        Assert.Equal(Color.Default, text.Background);
    }

    [Fact]
    public void Colored_CreatesForegroundAndBackgroundColoredText()
    {
        var text = Text.Colored("Hello", Color.Yellow, Color.Blue);

        Assert.Equal("Hello", text.Content);
        Assert.Equal(Color.Yellow, text.Foreground);
        Assert.Equal(Color.Blue, text.Background);
    }

    [Fact]
    public void Content_CanBeUpdated()
    {
        var text = new Text("Initial");

        text.Content = "Updated";

        Assert.Equal("Updated", text.Content);
    }
}
