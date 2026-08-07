// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Components.Streaming;
using Termina.Terminal;

namespace Termina.Tests.Components.Streaming;

/// <summary>
/// Tests for the StyledWordWrapper class.
/// </summary>
public class StyledWordWrapperTests
{
    [Fact]
    public void WrapLine_ShortLine_NoWrap()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("Hello", new TextStyle(Color.Red)));

        var wrapped = StyledWordWrapper.WrapLine(line, 20);

        Assert.Single(wrapped);
        Assert.Equal("Hello", wrapped[0].ToPlainText());
    }

    [Fact]
    public void WrapLine_LongLine_WrapsAtWordBoundary()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("Hello World", new TextStyle(Color.Red)));

        var wrapped = StyledWordWrapper.WrapLine(line, 6);

        Assert.Equal(2, wrapped.Count);
        Assert.Equal("Hello", wrapped[0].ToPlainText());
        Assert.Equal("World", wrapped[1].ToPlainText());
    }

    [Fact]
    public void WrapLine_PreservesStyleAcrossWrap()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("Hello World", new TextStyle(Color.Red)));

        var wrapped = StyledWordWrapper.WrapLine(line, 6);

        Assert.Equal(Color.Red, wrapped[0].Segments[0].Style.Foreground);
        Assert.Equal(Color.Red, wrapped[1].Segments[0].Style.Foreground);
    }

    [Fact]
    public void WrapLine_MixedStyles_PreservesStylesPerWord()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("Red ", new TextStyle(Color.Red)));
        line.Append(new StyledSegment("Blue", new TextStyle(Color.Blue)));

        var wrapped = StyledWordWrapper.WrapLine(line, 5);

        Assert.Equal(2, wrapped.Count);
        Assert.Equal("Red", wrapped[0].ToPlainText());
        Assert.Equal("Blue", wrapped[1].ToPlainText());
        Assert.Equal(Color.Red, wrapped[0].Segments[0].Style.Foreground);
        Assert.Equal(Color.Blue, wrapped[1].Segments[0].Style.Foreground);
    }

    [Fact]
    public void WrapLine_LongWord_BreaksWord()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("Supercalifragilistic", new TextStyle(Color.Green)));

        var wrapped = StyledWordWrapper.WrapLine(line, 10);

        Assert.Equal(2, wrapped.Count);
        Assert.Equal("Supercalif", wrapped[0].ToPlainText());
        Assert.Equal("ragilistic", wrapped[1].ToPlainText());
    }

    [Fact]
    public void WrapLine_LongWord_PreservesStyleAcrossBreak()
    {
        var line = new StyledLine();
        var style = new TextStyle(Color.Yellow, Color.Default, TextDecoration.Bold);
        line.Append(new StyledSegment("Supercalifragilistic", style));

        var wrapped = StyledWordWrapper.WrapLine(line, 10);

        Assert.Equal(Color.Yellow, wrapped[0].Segments[0].Style.Foreground);
        Assert.Equal(TextDecoration.Bold, wrapped[0].Segments[0].Style.Decoration);
        Assert.Equal(Color.Yellow, wrapped[1].Segments[0].Style.Foreground);
        Assert.Equal(TextDecoration.Bold, wrapped[1].Segments[0].Style.Decoration);
    }

    [Fact]
    public void WrapLine_EmptyLine_ReturnsSingleEmptyLine()
    {
        var line = new StyledLine();

        var wrapped = StyledWordWrapper.WrapLine(line, 10);

        Assert.Single(wrapped);
        Assert.Equal(0, wrapped[0].Length);
    }

    [Fact]
    public void WrapLine_MultipleSpaces_Handled()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("Hello    World", new TextStyle(Color.Red)));

        var wrapped = StyledWordWrapper.WrapLine(line, 8);

        // Should handle multiple spaces between words
        Assert.True(wrapped.Count >= 2);
    }

    [Fact]
    public void WrapLines_MultipleLines_WrapsEach()
    {
        var lines = new List<StyledLine>
        {
            CreateLine("Hello World", Color.Red),
            CreateLine("Foo Bar Baz", Color.Blue)
        };

        var wrapped = StyledWordWrapper.WrapLines(lines, 6);

        // "Hello World" wraps to 2 lines, "Foo Bar Baz" wraps to 3 lines
        Assert.True(wrapped.Count >= 4);
    }

    [Fact]
    public void WrapLine_MidWordStyleChange_PreservesStyles()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("Hel", new TextStyle(Color.Red)));
        line.Append(new StyledSegment("lo World", new TextStyle(Color.Blue)));

        var wrapped = StyledWordWrapper.WrapLine(line, 6);

        // First line should have mixed styles within "Hello"
        Assert.Equal("Hello", wrapped[0].ToPlainText());
        Assert.True(wrapped[0].Segments.Count >= 2); // "Hel" (red) + "lo" (blue)
    }

    [Fact]
    public void WrapLine_WidthOne_CharacterPerLine()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("ABC", new TextStyle(Color.Red)));

        var wrapped = StyledWordWrapper.WrapLine(line, 1);

        Assert.Equal(3, wrapped.Count);
        Assert.Equal("A", wrapped[0].ToPlainText());
        Assert.Equal("B", wrapped[1].ToPlainText());
        Assert.Equal("C", wrapped[2].ToPlainText());
    }

    [Fact]
    public void WrapLine_MultiWordSegmentWithBackground_PreservesBackgroundColorOnSeparatorSpace()
    {
        var line = new StyledLine();
        var style = new TextStyle(Color.Black, Color.BrightMagenta, TextDecoration.Bold);
        line.Append(new StyledSegment("[Highlighted Message]", style));

        var wrapped = StyledWordWrapper.WrapLine(line, 30);

        Assert.Single(wrapped);
        Assert.Equal("[Highlighted Message]", wrapped[0].ToPlainText());

        // All segments in the wrapped line (including the space separator segment between words) must inherit background color
        foreach (var segment in wrapped[0].Segments)
        {
            Assert.Equal(Color.BrightMagenta, segment.Style.Background);
        }
    }

    [Fact]
    public void WrapLine_BackgroundWordAfterUnstyledWord_SpaceSeparatorDoesNotInheritBackground()
    {
        var line = new StyledLine();
        line.Append(new StyledSegment("[YT]", new TextStyle(Color.Red))); // Unstyled background
        line.Append(new StyledSegment(" ", TextStyle.Default));
        line.Append(new StyledSegment("[GIFT SUB]", new TextStyle(Color.Black, Color.BrightGreen, TextDecoration.Bold)));

        var wrapped = StyledWordWrapper.WrapLine(line, 30);

        Assert.Single(wrapped);

        // Find the space segment between [YT] and [GIFT SUB]
        var spaceSegment = wrapped[0].Segments.First(s => s.Text == " ");
        Assert.NotEqual(Color.BrightGreen, spaceSegment.Style.Background);
    }

    private static StyledLine CreateLine(string text, Color color)
    {
        var line = new StyledLine();
        line.Append(new StyledSegment(text, new TextStyle(color)));
        return line;
    }
}
