// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Tests.Terminal;

/// <summary>
/// Tests for the TextDecoration flags enum.
/// </summary>
public class TextDecorationTests
{
    [Fact]
    public void None_HasNoFlags()
    {
        var decoration = TextDecoration.None;

        Assert.False(decoration.HasFlag(TextDecoration.Bold));
        Assert.False(decoration.HasFlag(TextDecoration.Dim));
        Assert.False(decoration.HasFlag(TextDecoration.Italic));
        Assert.False(decoration.HasFlag(TextDecoration.Underline));
        Assert.False(decoration.HasFlag(TextDecoration.Strikethrough));
    }

    [Fact]
    public void Bold_OnlyHasBold()
    {
        var decoration = TextDecoration.Bold;

        Assert.True(decoration.HasFlag(TextDecoration.Bold));
        Assert.False(decoration.HasFlag(TextDecoration.Dim));
        Assert.False(decoration.HasFlag(TextDecoration.Italic));
    }

    [Fact]
    public void Combined_BoldAndItalic_HasBothFlags()
    {
        var decoration = TextDecoration.Bold | TextDecoration.Italic;

        Assert.True(decoration.HasFlag(TextDecoration.Bold));
        Assert.True(decoration.HasFlag(TextDecoration.Italic));
        Assert.False(decoration.HasFlag(TextDecoration.Underline));
    }

    [Fact]
    public void Combined_AllDecorations_HasAllFlags()
    {
        var decoration = TextDecoration.Bold | TextDecoration.Dim |
                        TextDecoration.Italic | TextDecoration.Underline |
                        TextDecoration.Strikethrough;

        Assert.True(decoration.HasFlag(TextDecoration.Bold));
        Assert.True(decoration.HasFlag(TextDecoration.Dim));
        Assert.True(decoration.HasFlag(TextDecoration.Italic));
        Assert.True(decoration.HasFlag(TextDecoration.Underline));
        Assert.True(decoration.HasFlag(TextDecoration.Strikethrough));
    }

    [Fact]
    public void RemoveFlag_WorksCorrectly()
    {
        var decoration = TextDecoration.Bold | TextDecoration.Italic;
        decoration &= ~TextDecoration.Bold;

        Assert.False(decoration.HasFlag(TextDecoration.Bold));
        Assert.True(decoration.HasFlag(TextDecoration.Italic));
    }

    [Fact]
    public void Values_ArePowersOfTwo()
    {
        Assert.Equal(0, (int)TextDecoration.None);
        Assert.Equal(1, (int)TextDecoration.Bold);
        Assert.Equal(2, (int)TextDecoration.Dim);
        Assert.Equal(4, (int)TextDecoration.Italic);
        Assert.Equal(8, (int)TextDecoration.Underline);
        Assert.Equal(16, (int)TextDecoration.Strikethrough);
    }
}
