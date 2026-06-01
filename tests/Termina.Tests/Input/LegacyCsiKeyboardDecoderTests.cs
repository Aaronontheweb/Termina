// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class LegacyCsiKeyboardDecoderTests
{
    [Theory]
    [InlineData("[1~", ConsoleKey.Home)]
    [InlineData("[2~", ConsoleKey.Insert)]
    [InlineData("[3~", ConsoleKey.Delete)]
    [InlineData("[4~", ConsoleKey.End)]
    [InlineData("[5~", ConsoleKey.PageUp)]
    [InlineData("[6~", ConsoleKey.PageDown)]
    [InlineData("[7~", ConsoleKey.Home)]
    [InlineData("[8~", ConsoleKey.End)]
    [InlineData("[11~", ConsoleKey.F1)]
    [InlineData("[12~", ConsoleKey.F2)]
    [InlineData("[13~", ConsoleKey.F3)]
    [InlineData("[14~", ConsoleKey.F4)]
    [InlineData("[15~", ConsoleKey.F5)]
    [InlineData("[17~", ConsoleKey.F6)]
    [InlineData("[18~", ConsoleKey.F7)]
    [InlineData("[19~", ConsoleKey.F8)]
    [InlineData("[20~", ConsoleKey.F9)]
    [InlineData("[21~", ConsoleKey.F10)]
    [InlineData("[23~", ConsoleKey.F11)]
    [InlineData("[24~", ConsoleKey.F12)]
    public void TryDecode_KnownTildeSequence_ReturnsKeyStroke(string sequence, ConsoleKey expectedKey)
    {
        var decoded = LegacyCsiKeyboardDecoder.TryDecode(sequence, out var keyStroke);

        Assert.True(decoded);
        Assert.NotNull(keyStroke);
        Assert.Equal(ToTerminaKey(expectedKey), keyStroke!.Key);
        Assert.Equal(KeyModifiers.None, keyStroke.Modifiers);
        Assert.Equal(KeyEventPhase.Press, keyStroke.Phase);
        Assert.Null(keyStroke.Text);
    }

    [Theory]
    [InlineData("[5;2~", true, false, false)]
    [InlineData("[5;3~", false, true, false)]
    [InlineData("[5;5~", false, false, true)]
    [InlineData("[5;8~", true, true, true)]
    public void TryDecode_ModifiedTildeSequence_ReturnsKeyStrokeWithModifiers(
        string sequence,
        bool shift,
        bool alt,
        bool ctrl)
    {
        var decoded = LegacyCsiKeyboardDecoder.TryDecode(sequence, out var keyStroke);

        Assert.True(decoded);
        Assert.NotNull(keyStroke);
        Assert.Equal(TerminaKey.PageUp, keyStroke!.Key);
        Assert.Equal(shift, keyStroke.Modifiers.HasFlag(KeyModifiers.Shift));
        Assert.Equal(alt, keyStroke.Modifiers.HasFlag(KeyModifiers.Alt));
        Assert.Equal(ctrl, keyStroke.Modifiers.HasFlag(KeyModifiers.Control));
    }

    [Theory]
    [InlineData("[9~")]
    [InlineData("[25;5~")]
    [InlineData("[200~")]
    [InlineData("[5;0~")]
    [InlineData("[5;bad~")]
    [InlineData("5~")]
    public void TryDecode_UnknownOrMalformedSequence_ReturnsFalse(string sequence)
    {
        var decoded = LegacyCsiKeyboardDecoder.TryDecode(sequence, out var keyStroke);

        Assert.False(decoded);
        Assert.Null(keyStroke);
    }

    private static TerminaKey ToTerminaKey(ConsoleKey key) => key switch
    {
        ConsoleKey.Insert => TerminaKey.Insert,
        ConsoleKey.Delete => TerminaKey.Delete,
        ConsoleKey.Home => TerminaKey.Home,
        ConsoleKey.End => TerminaKey.End,
        ConsoleKey.PageUp => TerminaKey.PageUp,
        ConsoleKey.PageDown => TerminaKey.PageDown,
        >= ConsoleKey.F1 and <= ConsoleKey.F12 => TerminaKey.F1 + (key - ConsoleKey.F1),
        _ => TerminaKey.None,
    };
}
