// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class KittySecondFormKeyboardDecoderTests
{
    [Theory]
    [InlineData("[1;2A", ConsoleKey.UpArrow, true, false, false)]
    [InlineData("[1;5B", ConsoleKey.DownArrow, false, false, true)]
    [InlineData("[1;8A", ConsoleKey.UpArrow, true, true, true)]
    [InlineData("[1;3C", ConsoleKey.RightArrow, false, true, false)]
    public void TryDecode_PressSequence_ReturnsKeyStrokeWithModifiers(
        string sequence,
        ConsoleKey expectedKey,
        bool shift,
        bool alt,
        bool ctrl)
    {
        var decoded = KittySecondFormKeyboardDecoder.TryDecode(sequence, out var keyStroke);

        Assert.True(decoded);
        Assert.NotNull(keyStroke);
        Assert.Equal(ToTerminaKey(expectedKey), keyStroke!.Key);
        Assert.Equal(KeyEventPhase.Press, keyStroke.Phase);
        Assert.Equal(shift, keyStroke.Modifiers.HasFlag(KeyModifiers.Shift));
        Assert.Equal(alt, keyStroke.Modifiers.HasFlag(KeyModifiers.Alt));
        Assert.Equal(ctrl, keyStroke.Modifiers.HasFlag(KeyModifiers.Control));
        Assert.Null(keyStroke.Text);
    }

    [Fact]
    public void TryDecode_ExplicitPressEvent_ReturnsKeyStroke()
    {
        var decoded = KittySecondFormKeyboardDecoder.TryDecode("[1;1:1A", out var keyStroke);

        Assert.True(decoded);
        Assert.NotNull(keyStroke);
        Assert.Equal(TerminaKey.UpArrow, keyStroke!.Key);
        Assert.Equal(KeyEventPhase.Press, keyStroke.Phase);
    }

    [Theory]
    [InlineData("[1;1:2A", 2)]
    [InlineData("[1;1:3A", 3)]
    public void TryDecode_RepeatOrRelease_ReturnsKeyStrokeWithPhase(string sequence, int eventType)
    {
        var decoded = KittySecondFormKeyboardDecoder.TryDecode(sequence, out var keyStroke);

        Assert.True(decoded);
        Assert.NotNull(keyStroke);
        Assert.Equal(TerminaKey.UpArrow, keyStroke!.Key);
        Assert.Equal(ToPhase(eventType), keyStroke.Phase);
    }

    [Theory]
    [InlineData("[2;1A")]
    [InlineData("[1A")]
    [InlineData("[1;0A")]
    [InlineData("[1;badA")]
    [InlineData("[1;1:badA")]
    [InlineData("[1;1E")]
    [InlineData("1;1A")]
    public void TryDecode_UnknownOrMalformedSequence_ReturnsFalse(string sequence)
    {
        var decoded = KittySecondFormKeyboardDecoder.TryDecode(sequence, out var keyStroke);

        Assert.False(decoded);
        Assert.Null(keyStroke);
    }

    private static TerminaKey ToTerminaKey(ConsoleKey key) => key switch
    {
        ConsoleKey.UpArrow => TerminaKey.UpArrow,
        ConsoleKey.DownArrow => TerminaKey.DownArrow,
        ConsoleKey.RightArrow => TerminaKey.RightArrow,
        ConsoleKey.LeftArrow => TerminaKey.LeftArrow,
        ConsoleKey.Home => TerminaKey.Home,
        ConsoleKey.End => TerminaKey.End,
        >= ConsoleKey.F1 and <= ConsoleKey.F4 => TerminaKey.F1 + (key - ConsoleKey.F1),
        _ => TerminaKey.None,
    };

    private static KeyEventPhase ToPhase(int eventType) => eventType switch
    {
        2 => KeyEventPhase.Repeat,
        3 => KeyEventPhase.Release,
        _ => KeyEventPhase.Press,
    };
}
