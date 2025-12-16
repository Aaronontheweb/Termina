// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Tests.Terminal;

/// <summary>
/// Tests for the TextStyle struct.
/// </summary>
public class TextStyleTests
{
    [Fact]
    public void Default_HasAllDefaults()
    {
        var style = TextStyle.Default;

        Assert.Equal(Color.Default, style.Foreground);
        Assert.Equal(Color.Default, style.Background);
        Assert.Equal(TextDecoration.None, style.Decoration);
        Assert.True(style.IsDefault);
    }

    [Fact]
    public void Constructor_WithForeground_SetsForeground()
    {
        var style = new TextStyle(Color.Red);

        Assert.Equal(Color.Red, style.Foreground);
        Assert.Equal(Color.Default, style.Background);
        Assert.Equal(TextDecoration.None, style.Decoration);
    }

    [Fact]
    public void Constructor_WithForegroundAndBackground_SetsBoth()
    {
        var style = new TextStyle(Color.Red, Color.Blue);

        Assert.Equal(Color.Red, style.Foreground);
        Assert.Equal(Color.Blue, style.Background);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAll()
    {
        var style = new TextStyle(Color.Green, Color.Yellow, TextDecoration.Bold);

        Assert.Equal(Color.Green, style.Foreground);
        Assert.Equal(Color.Yellow, style.Background);
        Assert.Equal(TextDecoration.Bold, style.Decoration);
    }

    [Fact]
    public void HasForeground_TrueWhenNotDefault()
    {
        var style = new TextStyle(Color.Red);

        Assert.True(style.HasForeground);
        Assert.False(style.HasBackground);
    }

    [Fact]
    public void HasBackground_TrueWhenNotDefault()
    {
        var style = new TextStyle(Color.Default, Color.Blue);

        Assert.False(style.HasForeground);
        Assert.True(style.HasBackground);
    }

    [Fact]
    public void HasDecoration_TrueWhenNotNone()
    {
        var style = new TextStyle(Color.Default, Color.Default, TextDecoration.Italic);

        Assert.True(style.HasDecoration);
        Assert.False(style.HasForeground);
        Assert.False(style.HasBackground);
    }

    [Fact]
    public void IsDefault_FalseWhenForegroundSet()
    {
        var style = new TextStyle(Color.Red);

        Assert.False(style.IsDefault);
    }

    [Fact]
    public void IsDefault_FalseWhenBackgroundSet()
    {
        var style = new TextStyle(Color.Default, Color.Blue);

        Assert.False(style.IsDefault);
    }

    [Fact]
    public void IsDefault_FalseWhenDecorationSet()
    {
        var style = new TextStyle(Color.Default, Color.Default, TextDecoration.Underline);

        Assert.False(style.IsDefault);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var style1 = new TextStyle(Color.Red, Color.Blue, TextDecoration.Bold);
        var style2 = new TextStyle(Color.Red, Color.Blue, TextDecoration.Bold);

        Assert.Equal(style1, style2);
        Assert.True(style1 == style2);
    }

    [Fact]
    public void Equality_DifferentForeground_AreNotEqual()
    {
        var style1 = new TextStyle(Color.Red);
        var style2 = new TextStyle(Color.Blue);

        Assert.NotEqual(style1, style2);
        Assert.True(style1 != style2);
    }

    [Fact]
    public void Equality_DifferentBackground_AreNotEqual()
    {
        var style1 = new TextStyle(Color.Default, Color.Red);
        var style2 = new TextStyle(Color.Default, Color.Blue);

        Assert.NotEqual(style1, style2);
    }

    [Fact]
    public void Equality_DifferentDecoration_AreNotEqual()
    {
        var style1 = new TextStyle(Color.Default, Color.Default, TextDecoration.Bold);
        var style2 = new TextStyle(Color.Default, Color.Default, TextDecoration.Italic);

        Assert.NotEqual(style1, style2);
    }
}
