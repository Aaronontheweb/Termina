// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Tests.Terminal;

/// <summary>
/// Tests for ANSI escape code generation.
/// </summary>
public class AnsiCodesTests
{
    private const string Csi = "\x1b[";

    [Fact]
    public void Escape_IsCorrect()
    {
        Assert.Equal('\x1b', AnsiCodes.Escape);
    }

    [Fact]
    public void Csi_IsCorrect()
    {
        Assert.Equal("\x1b[", AnsiCodes.Csi);
    }

    // Cursor positioning
    [Theory]
    [InlineData(0, 0, "\x1b[1;1H")]
    [InlineData(5, 10, "\x1b[6;11H")]
    [InlineData(23, 79, "\x1b[24;80H")]
    public void MoveTo_GeneratesCorrectCode(int row, int col, string expected)
    {
        Assert.Equal(expected, AnsiCodes.MoveTo(row, col));
    }

    [Theory]
    [InlineData(1, "\x1b[1A")]
    [InlineData(5, "\x1b[5A")]
    public void MoveUp_GeneratesCorrectCode(int n, string expected)
    {
        Assert.Equal(expected, AnsiCodes.MoveUp(n));
    }

    [Theory]
    [InlineData(1, "\x1b[1B")]
    [InlineData(5, "\x1b[5B")]
    public void MoveDown_GeneratesCorrectCode(int n, string expected)
    {
        Assert.Equal(expected, AnsiCodes.MoveDown(n));
    }

    [Theory]
    [InlineData(1, "\x1b[1C")]
    [InlineData(5, "\x1b[5C")]
    public void MoveRight_GeneratesCorrectCode(int n, string expected)
    {
        Assert.Equal(expected, AnsiCodes.MoveRight(n));
    }

    [Theory]
    [InlineData(1, "\x1b[1D")]
    [InlineData(5, "\x1b[5D")]
    public void MoveLeft_GeneratesCorrectCode(int n, string expected)
    {
        Assert.Equal(expected, AnsiCodes.MoveLeft(n));
    }

    [Fact]
    public void SaveCursor_IsCorrect()
    {
        Assert.Equal($"{Csi}s", AnsiCodes.SaveCursor);
    }

    [Fact]
    public void RestoreCursor_IsCorrect()
    {
        Assert.Equal($"{Csi}u", AnsiCodes.RestoreCursor);
    }

    [Fact]
    public void ShowCursor_IsCorrect()
    {
        Assert.Equal($"{Csi}?25h", AnsiCodes.ShowCursor);
    }

    [Fact]
    public void HideCursor_IsCorrect()
    {
        Assert.Equal($"{Csi}?25l", AnsiCodes.HideCursor);
    }

    // Screen clearing
    [Fact]
    public void ClearScreen_IsCorrect()
    {
        Assert.Equal($"{Csi}2J", AnsiCodes.ClearScreen);
    }

    [Fact]
    public void ClearToEnd_IsCorrect()
    {
        Assert.Equal($"{Csi}0J", AnsiCodes.ClearToEnd);
    }

    [Fact]
    public void ClearToBeginning_IsCorrect()
    {
        Assert.Equal($"{Csi}1J", AnsiCodes.ClearToBeginning);
    }

    [Fact]
    public void ClearLine_IsCorrect()
    {
        Assert.Equal($"{Csi}2K", AnsiCodes.ClearLine);
    }

    [Fact]
    public void ClearLineToEnd_IsCorrect()
    {
        Assert.Equal($"{Csi}0K", AnsiCodes.ClearLineToEnd);
    }

    [Fact]
    public void ClearLineToBeginning_IsCorrect()
    {
        Assert.Equal($"{Csi}1K", AnsiCodes.ClearLineToBeginning);
    }

    // Colors - 256 mode
    [Theory]
    [InlineData(0, "\x1b[38;5;0m")]
    [InlineData(15, "\x1b[38;5;15m")]
    [InlineData(255, "\x1b[38;5;255m")]
    public void Foreground256_GeneratesCorrectCode(byte color, string expected)
    {
        Assert.Equal(expected, AnsiCodes.Foreground256(color));
    }

    [Theory]
    [InlineData(0, "\x1b[48;5;0m")]
    [InlineData(15, "\x1b[48;5;15m")]
    [InlineData(255, "\x1b[48;5;255m")]
    public void Background256_GeneratesCorrectCode(byte color, string expected)
    {
        Assert.Equal(expected, AnsiCodes.Background256(color));
    }

    // Colors - RGB mode
    [Theory]
    [InlineData(255, 0, 0, "\x1b[38;2;255;0;0m")]
    [InlineData(0, 255, 0, "\x1b[38;2;0;255;0m")]
    [InlineData(0, 0, 255, "\x1b[38;2;0;0;255m")]
    [InlineData(128, 64, 32, "\x1b[38;2;128;64;32m")]
    public void ForegroundRgb_GeneratesCorrectCode(byte r, byte g, byte b, string expected)
    {
        Assert.Equal(expected, AnsiCodes.ForegroundRgb(r, g, b));
    }

    [Theory]
    [InlineData(255, 0, 0, "\x1b[48;2;255;0;0m")]
    [InlineData(0, 255, 0, "\x1b[48;2;0;255;0m")]
    [InlineData(0, 0, 255, "\x1b[48;2;0;0;255m")]
    [InlineData(128, 64, 32, "\x1b[48;2;128;64;32m")]
    public void BackgroundRgb_GeneratesCorrectCode(byte r, byte g, byte b, string expected)
    {
        Assert.Equal(expected, AnsiCodes.BackgroundRgb(r, g, b));
    }

    // Default colors
    [Fact]
    public void ForegroundDefault_IsCorrect()
    {
        Assert.Equal($"{Csi}39m", AnsiCodes.ForegroundDefault);
    }

    [Fact]
    public void BackgroundDefault_IsCorrect()
    {
        Assert.Equal($"{Csi}49m", AnsiCodes.BackgroundDefault);
    }

    [Fact]
    public void Reset_IsCorrect()
    {
        Assert.Equal($"{Csi}0m", AnsiCodes.Reset);
    }

    // Text attributes
    [Fact]
    public void Bold_IsCorrect()
    {
        Assert.Equal($"{Csi}1m", AnsiCodes.Bold);
    }

    [Fact]
    public void Dim_IsCorrect()
    {
        Assert.Equal($"{Csi}2m", AnsiCodes.Dim);
    }

    [Fact]
    public void Italic_IsCorrect()
    {
        Assert.Equal($"{Csi}3m", AnsiCodes.Italic);
    }

    [Fact]
    public void Underline_IsCorrect()
    {
        Assert.Equal($"{Csi}4m", AnsiCodes.Underline);
    }

    [Fact]
    public void Blink_IsCorrect()
    {
        Assert.Equal($"{Csi}5m", AnsiCodes.Blink);
    }

    [Fact]
    public void Reverse_IsCorrect()
    {
        Assert.Equal($"{Csi}7m", AnsiCodes.Reverse);
    }

    [Fact]
    public void Strikethrough_IsCorrect()
    {
        Assert.Equal($"{Csi}9m", AnsiCodes.Strikethrough);
    }

    // Reset attributes
    [Fact]
    public void ResetBold_IsCorrect()
    {
        Assert.Equal($"{Csi}22m", AnsiCodes.ResetBold);
    }

    [Fact]
    public void ResetItalic_IsCorrect()
    {
        Assert.Equal($"{Csi}23m", AnsiCodes.ResetItalic);
    }

    [Fact]
    public void ResetUnderline_IsCorrect()
    {
        Assert.Equal($"{Csi}24m", AnsiCodes.ResetUnderline);
    }

    [Fact]
    public void ResetBlink_IsCorrect()
    {
        Assert.Equal($"{Csi}25m", AnsiCodes.ResetBlink);
    }

    [Fact]
    public void ResetReverse_IsCorrect()
    {
        Assert.Equal($"{Csi}27m", AnsiCodes.ResetReverse);
    }

    [Fact]
    public void ResetStrikethrough_IsCorrect()
    {
        Assert.Equal($"{Csi}29m", AnsiCodes.ResetStrikethrough);
    }

    // Mouse tracking
    [Fact]
    public void EnableMouseX10_IsCorrect()
    {
        Assert.Equal($"{Csi}?9h", AnsiCodes.EnableMouseX10);
    }

    [Fact]
    public void DisableMouseX10_IsCorrect()
    {
        Assert.Equal($"{Csi}?9l", AnsiCodes.DisableMouseX10);
    }

    [Fact]
    public void EnableMouseNormal_IsCorrect()
    {
        Assert.Equal($"{Csi}?1000h", AnsiCodes.EnableMouseNormal);
    }

    [Fact]
    public void DisableMouseNormal_IsCorrect()
    {
        Assert.Equal($"{Csi}?1000l", AnsiCodes.DisableMouseNormal);
    }

    [Fact]
    public void EnableMouseSgr_IsCorrect()
    {
        Assert.Equal($"{Csi}?1006h", AnsiCodes.EnableMouseSgr);
    }

    [Fact]
    public void DisableMouseSgr_IsCorrect()
    {
        Assert.Equal($"{Csi}?1006l", AnsiCodes.DisableMouseSgr);
    }

    [Fact]
    public void EnableAlternateScroll_IsCorrect()
    {
        Assert.Equal($"{Csi}?1007h", AnsiCodes.EnableAlternateScroll);
    }

    [Fact]
    public void DisableAlternateScroll_IsCorrect()
    {
        Assert.Equal($"{Csi}?1007l", AnsiCodes.DisableAlternateScroll);
    }

    // Alternate screen
    [Fact]
    public void EnterAlternateScreen_IsCorrect()
    {
        Assert.Equal($"{Csi}?1049h", AnsiCodes.EnterAlternateScreen);
    }

    [Fact]
    public void ExitAlternateScreen_IsCorrect()
    {
        Assert.Equal($"{Csi}?1049l", AnsiCodes.ExitAlternateScreen);
    }

    // Terminal queries
    [Fact]
    public void QueryCursorPosition_IsCorrect()
    {
        Assert.Equal($"{Csi}6n", AnsiCodes.QueryCursorPosition);
    }

    [Fact]
    public void QueryTerminalSize_IsCorrect()
    {
        Assert.Equal($"{Csi}18t", AnsiCodes.QueryTerminalSize);
    }

    [Fact]
    public void Osc52Clipboard_EncodesUtf8Text()
    {
        Assert.Equal("\x1b]52;c;SGVsbG8=\x07", AnsiCodes.Osc52Clipboard("Hello"));
    }

    [Fact]
    public void Osc52Clipboard_WithStringTerminator_EncodesUtf8Text()
    {
        Assert.Equal("\x1b]52;c;SGVsbG8=\x1b\\", AnsiCodes.Osc52Clipboard("Hello", useStringTerminator: true));
    }

    [Fact]
    public void TmuxPassthrough_WrapsOsc52Sequence()
    {
        var wrapped = AnsiCodes.TmuxPassthrough(AnsiCodes.Osc52Clipboard("Hello"));
        Assert.StartsWith("\x1bPtmux;\x1b", wrapped);
        Assert.EndsWith("\x1b\\", wrapped);
        Assert.Contains("]52;c;SGVsbG8=\x07", wrapped.Replace("\x1b\x1b", "\x1b"));
    }
}
