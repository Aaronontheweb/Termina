// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Components.Streaming;
using Termina.Terminal;

namespace Termina.Tests.Components.Streaming;

/// <summary>
/// Tests for the StyledSegment struct.
/// </summary>
public class StyledSegmentTests
{
    [Fact]
    public void Constructor_SetsTextAndDefaultStyle()
    {
        var segment = new StyledSegment("Hello");

        Assert.Equal("Hello", segment.Text);
        Assert.True(segment.Style.IsDefault);
    }

    [Fact]
    public void Constructor_SetsTextAndStyle()
    {
        var style = new TextStyle(Color.Red, Color.Blue, TextDecoration.Bold);
        var segment = new StyledSegment("Hello", style);

        Assert.Equal("Hello", segment.Text);
        Assert.Equal(Color.Red, segment.Style.Foreground);
        Assert.Equal(Color.Blue, segment.Style.Background);
        Assert.Equal(TextDecoration.Bold, segment.Style.Decoration);
    }

    [Fact]
    public void Length_ReturnsTextLength()
    {
        var segment = new StyledSegment("Hello World");

        Assert.Equal(11, segment.Length);
    }

    [Fact]
    public void Length_EmptyString_ReturnsZero()
    {
        var segment = new StyledSegment("");

        Assert.Equal(0, segment.Length);
    }

    [Fact]
    public void Substring_ReturnsNewSegmentWithSameStyle()
    {
        var style = new TextStyle(Color.Green, Color.Default, TextDecoration.Italic);
        var segment = new StyledSegment("Hello World", style);

        var sub = segment.Substring(6, 5);

        Assert.Equal("World", sub.Text);
        Assert.Equal(Color.Green, sub.Style.Foreground);
        Assert.Equal(TextDecoration.Italic, sub.Style.Decoration);
    }

    [Fact]
    public void Substring_FromStart_ReturnsCorrectPortion()
    {
        var segment = new StyledSegment("Hello World");

        var sub = segment.Substring(0, 5);

        Assert.Equal("Hello", sub.Text);
    }

    [Fact]
    public void Equality_SameTextAndStyle_AreEqual()
    {
        var style = new TextStyle(Color.Red);
        var segment1 = new StyledSegment("Hello", style);
        var segment2 = new StyledSegment("Hello", style);

        Assert.Equal(segment1, segment2);
    }

    [Fact]
    public void Equality_DifferentText_AreNotEqual()
    {
        var style = new TextStyle(Color.Red);
        var segment1 = new StyledSegment("Hello", style);
        var segment2 = new StyledSegment("World", style);

        Assert.NotEqual(segment1, segment2);
    }

    [Fact]
    public void Equality_DifferentStyle_AreNotEqual()
    {
        var segment1 = new StyledSegment("Hello", new TextStyle(Color.Red));
        var segment2 = new StyledSegment("Hello", new TextStyle(Color.Blue));

        Assert.NotEqual(segment1, segment2);
    }
}
