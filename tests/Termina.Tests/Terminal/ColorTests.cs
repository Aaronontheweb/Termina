// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Tests.Terminal;

/// <summary>
/// Tests for the Color struct.
/// </summary>
public class ColorTests
{
    [Fact]
    public void Default_HasDefaultMode()
    {
        var color = Color.Default;

        Assert.Equal(ColorMode.Default, color.Mode);
    }

    [Fact]
    public void FromIndex_CreatesIndexedColor()
    {
        var color = Color.FromIndex(42);

        Assert.Equal(ColorMode.Indexed, color.Mode);
        Assert.Equal(42, color.Index);
    }

    [Fact]
    public void FromRgb_CreatesRgbColor()
    {
        var color = Color.FromRgb(255, 128, 64);

        Assert.Equal(ColorMode.Rgb, color.Mode);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
    }

    [Fact]
    public void FromHex_ParsesValidHex()
    {
        var color = Color.FromHex("#FF8040");

        Assert.Equal(ColorMode.Rgb, color.Mode);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
    }

    [Fact]
    public void FromHex_ParsesWithoutHash()
    {
        var color = Color.FromHex("FF8040");

        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
    }

    [Fact]
    public void FromHex_InvalidLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => Color.FromHex("FFF"));
        Assert.Throws<ArgumentException>(() => Color.FromHex("FFFFFFF"));
    }

    [Theory]
    [InlineData(0, "Black")]
    [InlineData(1, "Red")]
    [InlineData(2, "Green")]
    [InlineData(3, "Yellow")]
    [InlineData(4, "Blue")]
    [InlineData(5, "Magenta")]
    [InlineData(6, "Cyan")]
    [InlineData(7, "White")]
    public void StandardColors_HaveCorrectIndices(byte expectedIndex, string colorName)
    {
        var color = colorName switch
        {
            "Black" => Color.Black,
            "Red" => Color.Red,
            "Green" => Color.Green,
            "Yellow" => Color.Yellow,
            "Blue" => Color.Blue,
            "Magenta" => Color.Magenta,
            "Cyan" => Color.Cyan,
            "White" => Color.White,
            _ => throw new ArgumentException($"Unknown color: {colorName}")
        };

        Assert.Equal(ColorMode.Indexed, color.Mode);
        Assert.Equal(expectedIndex, color.Index);
    }

    [Theory]
    [InlineData(8, "BrightBlack")]
    [InlineData(9, "BrightRed")]
    [InlineData(10, "BrightGreen")]
    [InlineData(11, "BrightYellow")]
    [InlineData(12, "BrightBlue")]
    [InlineData(13, "BrightMagenta")]
    [InlineData(14, "BrightCyan")]
    [InlineData(15, "BrightWhite")]
    public void BrightColors_HaveCorrectIndices(byte expectedIndex, string colorName)
    {
        var color = colorName switch
        {
            "BrightBlack" => Color.BrightBlack,
            "BrightRed" => Color.BrightRed,
            "BrightGreen" => Color.BrightGreen,
            "BrightYellow" => Color.BrightYellow,
            "BrightBlue" => Color.BrightBlue,
            "BrightMagenta" => Color.BrightMagenta,
            "BrightCyan" => Color.BrightCyan,
            "BrightWhite" => Color.BrightWhite,
            _ => throw new ArgumentException($"Unknown color: {colorName}")
        };

        Assert.Equal(ColorMode.Indexed, color.Mode);
        Assert.Equal(expectedIndex, color.Index);
    }

    [Fact]
    public void ToForegroundAnsi_Default_ReturnsDefaultCode()
    {
        var ansi = Color.Default.ToForegroundAnsi();

        Assert.Equal("\x1b[39m", ansi);
    }

    [Fact]
    public void ToForegroundAnsi_Indexed_Returns256Code()
    {
        var ansi = Color.Red.ToForegroundAnsi();

        Assert.Equal("\x1b[38;5;1m", ansi);
    }

    [Fact]
    public void ToForegroundAnsi_Rgb_ReturnsRgbCode()
    {
        var color = Color.FromRgb(255, 128, 64);
        var ansi = color.ToForegroundAnsi();

        Assert.Equal("\x1b[38;2;255;128;64m", ansi);
    }

    [Fact]
    public void ToBackgroundAnsi_Default_ReturnsDefaultCode()
    {
        var ansi = Color.Default.ToBackgroundAnsi();

        Assert.Equal("\x1b[49m", ansi);
    }

    [Fact]
    public void ToBackgroundAnsi_Indexed_Returns256Code()
    {
        var ansi = Color.Blue.ToBackgroundAnsi();

        Assert.Equal("\x1b[48;5;4m", ansi);
    }

    [Fact]
    public void ToBackgroundAnsi_Rgb_ReturnsRgbCode()
    {
        var color = Color.FromRgb(128, 64, 32);
        var ansi = color.ToBackgroundAnsi();

        Assert.Equal("\x1b[48;2;128;64;32m", ansi);
    }

    [Fact]
    public void Equals_SameColor_ReturnsTrue()
    {
        var color1 = Color.FromRgb(255, 128, 64);
        var color2 = Color.FromRgb(255, 128, 64);

        Assert.True(color1.Equals(color2));
        Assert.True(color1 == color2);
        Assert.False(color1 != color2);
    }

    [Fact]
    public void Equals_DifferentColor_ReturnsFalse()
    {
        var color1 = Color.FromRgb(255, 128, 64);
        var color2 = Color.FromRgb(255, 128, 65);

        Assert.False(color1.Equals(color2));
        Assert.False(color1 == color2);
        Assert.True(color1 != color2);
    }

    [Fact]
    public void Equals_DifferentMode_ReturnsFalse()
    {
        var indexed = Color.FromIndex(1);
        var rgb = Color.FromRgb(255, 0, 0);

        Assert.False(indexed.Equals(rgb));
    }

    [Fact]
    public void GetHashCode_SameColor_SameHash()
    {
        var color1 = Color.FromRgb(255, 128, 64);
        var color2 = Color.FromRgb(255, 128, 64);

        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
    }

    [Fact]
    public void ToString_Default_ReturnsDefault()
    {
        Assert.Equal("Default", Color.Default.ToString());
    }

    [Fact]
    public void ToString_Indexed_ReturnsIndexFormat()
    {
        Assert.Equal("Index(42)", Color.FromIndex(42).ToString());
    }

    [Fact]
    public void ToString_Rgb_ReturnsRgbFormat()
    {
        Assert.Equal("RGB(255,128,64)", Color.FromRgb(255, 128, 64).ToString());
    }

    [Fact]
    public void Lerp_BothRgb_InterpolatesMidpoint()
    {
        var a = Color.FromRgb(0, 0, 0);
        var b = Color.FromRgb(200, 100, 50);

        var result = Color.Lerp(a, b, 0.5f);

        Assert.Equal(ColorMode.Rgb, result.Mode);
        Assert.Equal(100, result.R);
        Assert.Equal(50, result.G);
        Assert.Equal(25, result.B);
    }

    [Fact]
    public void Lerp_AtZero_ReturnsFirstColor()
    {
        var a = Color.FromRgb(10, 20, 30);
        var b = Color.FromRgb(200, 100, 50);

        var result = Color.Lerp(a, b, 0f);

        Assert.Equal(a, result);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsSecondColor()
    {
        var a = Color.FromRgb(10, 20, 30);
        var b = Color.FromRgb(200, 100, 50);

        var result = Color.Lerp(a, b, 1f);

        Assert.Equal(b, result);
    }

    [Fact]
    public void Lerp_ClampsBelowZero()
    {
        var a = Color.FromRgb(0, 0, 0);
        var b = Color.FromRgb(200, 100, 50);

        var result = Color.Lerp(a, b, -0.5f);

        Assert.Equal(a, result);
    }

    [Fact]
    public void Lerp_ClampsAboveOne()
    {
        var a = Color.FromRgb(0, 0, 0);
        var b = Color.FromRgb(200, 100, 50);

        var result = Color.Lerp(a, b, 1.5f);

        Assert.Equal(b, result);
    }

    [Fact]
    public void Lerp_NonRgbColor_ReturnsFirst()
    {
        var a = Color.FromIndex(5);
        var b = Color.FromRgb(200, 100, 50);

        var result = Color.Lerp(a, b, 0.5f);

        Assert.Equal(a, result);
    }

    [Fact]
    public void Lerp_DefaultColor_ReturnsFirst()
    {
        var a = Color.Default;
        var b = Color.FromRgb(200, 100, 50);

        var result = Color.Lerp(a, b, 0.5f);

        Assert.Equal(a, result);
    }
}
