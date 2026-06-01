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
    public void TryDecode_PressSequence_ReturnsKeyPressedWithModifiers(
        string sequence,
        ConsoleKey expectedKey,
        bool shift,
        bool alt,
        bool ctrl)
    {
        var decoded = KittySecondFormKeyboardDecoder.TryDecode(sequence, out var keyEvent);

        Assert.True(decoded);
        Assert.NotNull(keyEvent);
        Assert.Equal(expectedKey, keyEvent!.KeyInfo.Key);
        Assert.Equal('\0', keyEvent.KeyInfo.KeyChar);
        Assert.Equal(shift, keyEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift));
        Assert.Equal(alt, keyEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt));
        Assert.Equal(ctrl, keyEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control));
    }

    [Fact]
    public void TryDecode_ExplicitPressEvent_ReturnsKeyPressed()
    {
        var decoded = KittySecondFormKeyboardDecoder.TryDecode("[1;1:1A", out var keyEvent);

        Assert.True(decoded);
        Assert.NotNull(keyEvent);
        Assert.Equal(ConsoleKey.UpArrow, keyEvent!.KeyInfo.Key);
    }

    [Theory]
    [InlineData("[1;1:2A")]
    [InlineData("[1;1:3A")]
    public void TryDecode_RepeatOrRelease_ReturnsTrueWithoutEvent(string sequence)
    {
        var decoded = KittySecondFormKeyboardDecoder.TryDecode(sequence, out var keyEvent);

        Assert.True(decoded);
        Assert.Null(keyEvent);
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
        var decoded = KittySecondFormKeyboardDecoder.TryDecode(sequence, out var keyEvent);

        Assert.False(decoded);
        Assert.Null(keyEvent);
    }
}
