// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Rendering;

/// <summary>
/// Tests for the Panel component (v2).
/// </summary>
public class PanelTests
{
    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_DefaultValues()
    {
        var panel = new Panel();

        Assert.Null(panel.Content);
        Assert.Equal(string.Empty, panel.Title);
        Assert.Equal(BorderStyle.Single, panel.Border);
        Assert.Equal(Color.Default, panel.BorderColor);
    }

    [Fact]
    public void Content_SetAndGet()
    {
        var text = new Text("Hello");
        var panel = new Panel { Content = text };

        Assert.Same(text, panel.Content);
    }

    [Fact]
    public void Title_SetAndGet()
    {
        var panel = new Panel { Title = "My Panel" };

        Assert.Equal("My Panel", panel.Title);
    }

    [Fact]
    public void Border_SetAndGet()
    {
        var panel = new Panel { Border = BorderStyle.Double };

        Assert.Equal(BorderStyle.Double, panel.Border);
    }

    [Fact]
    public void BorderColor_SetAndGet()
    {
        var panel = new Panel { BorderColor = Color.Blue };

        Assert.Equal(Color.Blue, panel.BorderColor);
    }

    #endregion

    #region Render Tests

    [Fact]
    public void Render_EmptyPanel_DrawsBorder()
    {
        var terminal = new VirtualTerminal(20, 10);
        var context = new RegionRenderContext(terminal, 0, 0, 20, 10);
        var panel = new Panel();

        panel.Render(context);

        // Top-left corner should be drawn
        var topLeft = terminal.GetChar(0, 0);
        Assert.True(topLeft == '┌' || topLeft == '╔', $"Expected corner but got: {topLeft}");
    }

    [Fact]
    public void Render_SingleBorder_UsesCorrectCharacters()
    {
        var terminal = new VirtualTerminal(10, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 5);
        var panel = new Panel { Border = BorderStyle.Single };

        panel.Render(context);

        Assert.Equal('┌', terminal.GetChar(0, 0));  // Top-left
        Assert.Equal('┐', terminal.GetChar(9, 0));  // Top-right
        Assert.Equal('└', terminal.GetChar(0, 4));  // Bottom-left
        Assert.Equal('┘', terminal.GetChar(9, 4));  // Bottom-right
        Assert.Equal('─', terminal.GetChar(1, 0));  // Top edge
        Assert.Equal('│', terminal.GetChar(0, 1));  // Left edge
    }

    [Fact]
    public void Render_DoubleBorder_UsesCorrectCharacters()
    {
        var terminal = new VirtualTerminal(10, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 5);
        var panel = new Panel { Border = BorderStyle.Double };

        panel.Render(context);

        Assert.Equal('╔', terminal.GetChar(0, 0));  // Top-left
        Assert.Equal('╗', terminal.GetChar(9, 0));  // Top-right
        Assert.Equal('╚', terminal.GetChar(0, 4));  // Bottom-left
        Assert.Equal('╝', terminal.GetChar(9, 4));  // Bottom-right
        Assert.Equal('═', terminal.GetChar(1, 0));  // Top edge
        Assert.Equal('║', terminal.GetChar(0, 1));  // Left edge
    }

    [Fact]
    public void Render_RoundedBorder_UsesCorrectCharacters()
    {
        var terminal = new VirtualTerminal(10, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 5);
        var panel = new Panel { Border = BorderStyle.Rounded };

        panel.Render(context);

        Assert.Equal('╭', terminal.GetChar(0, 0));  // Top-left
        Assert.Equal('╮', terminal.GetChar(9, 0));  // Top-right
        Assert.Equal('╰', terminal.GetChar(0, 4));  // Bottom-left
        Assert.Equal('╯', terminal.GetChar(9, 4));  // Bottom-right
    }

    [Fact]
    public void Render_AsciiBorder_UsesCorrectCharacters()
    {
        var terminal = new VirtualTerminal(10, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 5);
        var panel = new Panel { Border = BorderStyle.Ascii };

        panel.Render(context);

        Assert.Equal('+', terminal.GetChar(0, 0));  // Top-left
        Assert.Equal('+', terminal.GetChar(9, 0));  // Top-right
        Assert.Equal('+', terminal.GetChar(0, 4));  // Bottom-left
        Assert.Equal('+', terminal.GetChar(9, 4));  // Bottom-right
        Assert.Equal('-', terminal.GetChar(1, 0));  // Top edge
        Assert.Equal('|', terminal.GetChar(0, 1));  // Left edge
    }

    [Fact]
    public void Render_NoBorder_DoesNotDrawBorder()
    {
        var terminal = new VirtualTerminal(10, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 5);
        var panel = new Panel { Border = BorderStyle.None, Content = new Text("Test") };

        panel.Render(context);

        // Content should start at 0,0 without border
        Assert.True(terminal.Contains("Test"));
    }

    [Fact]
    public void Render_WithTitle_DisplaysTitle()
    {
        var terminal = new VirtualTerminal(20, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 20, 5);
        var panel = new Panel { Title = "Test" };

        panel.Render(context);

        Assert.True(terminal.Contains("Test"));
    }

    [Fact]
    public void Render_WithContent_DisplaysContent()
    {
        var terminal = new VirtualTerminal(20, 10);
        var context = new RegionRenderContext(terminal, 0, 0, 20, 10);
        var panel = new Panel { Content = new Text("Hello World") };

        panel.Render(context);

        Assert.True(terminal.Contains("Hello World"));
    }

    [Fact]
    public void Render_ContentClippedToInterior()
    {
        var terminal = new VirtualTerminal(10, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 5);
        var panel = new Panel { Content = new Text("This is a very long text") };

        panel.Render(context);

        // Content should be clipped to interior (width - 2 for borders)
        var line1 = terminal.GetLine(1);
        Assert.True(line1.Length <= 10);
    }

    [Fact]
    public void Render_BorderColor_Applied()
    {
        var terminal = new VirtualTerminal(10, 5);
        var context = new RegionRenderContext(terminal, 0, 0, 10, 5);
        var panel = new Panel { BorderColor = Color.Red };

        panel.Render(context);

        // Top-left corner should have red foreground
        Assert.Equal(Color.Red, terminal.GetForeground(0, 0));
    }

    [Fact]
    public void Render_MinimalSize_StillRenders()
    {
        var terminal = new VirtualTerminal(3, 3);
        var context = new RegionRenderContext(terminal, 0, 0, 3, 3);
        var panel = new Panel();

        // Should not throw
        panel.Render(context);
    }

    #endregion

    #region Measure Tests

    [Fact]
    public void Measure_EmptyPanel_ReturnsMinimalSize()
    {
        var panel = new Panel();

        var (width, height) = panel.Measure(100, 100);

        // Minimum is 2 for borders (left + right, top + bottom)
        Assert.True(width >= 2);
        Assert.True(height >= 2);
    }

    [Fact]
    public void Measure_WithContent_IncludesBorder()
    {
        var panel = new Panel { Content = new Text("Hi") };

        var (width, height) = panel.Measure(100, 100);

        // Content "Hi" (2) + borders (2) = 4 minimum width
        Assert.True(width >= 4);
        Assert.True(height >= 3); // 1 line content + 2 for borders
    }

    [Fact]
    public void Measure_NoBorder_DoesNotAddBorderSpace()
    {
        var panel = new Panel { Border = BorderStyle.None, Content = new Text("Hi") };

        var (width, height) = panel.Measure(100, 100);

        Assert.True(width >= 2); // Just content width
        Assert.True(height >= 1); // Just content height
    }

    [Fact]
    public void Measure_ClampsToAvailable()
    {
        var panel = new Panel { Content = new Text("Hello World Long Text") };

        var (width, height) = panel.Measure(10, 5);

        Assert.Equal(10, width);
        Assert.True(height <= 5);
    }

    #endregion
}
