// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class KittyCsiUKeyboardDecoderTests
{
    [Theory]
    [InlineData("[13;5u", ConsoleKey.Enter, '\r', false, false, true)]
    [InlineData("[13;2u", ConsoleKey.Enter, '\r', true, false, false)]
    [InlineData("[13;6u", ConsoleKey.Enter, '\r', true, false, true)]
    [InlineData("[13u", ConsoleKey.Enter, '\r', false, false, false)]
    [InlineData("[97;2;65u", ConsoleKey.A, 'a', true, false, false)]
    public void TryDecode_StandardSequence_ReturnsKeyPressed(
        string sequence,
        ConsoleKey expectedKey,
        char expectedChar,
        bool shift,
        bool alt,
        bool ctrl)
    {
        var decoded = KittyCsiUKeyboardDecoder.TryDecode(sequence, out var keyEvent);

        Assert.True(decoded);
        Assert.NotNull(keyEvent);
        Assert.Equal(expectedKey, keyEvent!.KeyInfo.Key);
        Assert.Equal(expectedChar, keyEvent.KeyInfo.KeyChar);
        Assert.Equal(shift, keyEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift));
        Assert.Equal(alt, keyEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt));
        Assert.Equal(ctrl, keyEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control));
    }

    [Theory]
    [InlineData("[57352u", ConsoleKey.UpArrow, false)]
    [InlineData("[57352;2u", ConsoleKey.UpArrow, true)]
    [InlineData("[57368u", ConsoleKey.F5, false)]
    [InlineData("[57348u", ConsoleKey.Insert, false)]
    public void TryDecode_PuaFunctionalKey_ReturnsKeyPressed(
        string sequence,
        ConsoleKey expectedKey,
        bool shift)
    {
        var decoded = KittyCsiUKeyboardDecoder.TryDecode(sequence, out var keyEvent);

        Assert.True(decoded);
        Assert.NotNull(keyEvent);
        Assert.Equal(expectedKey, keyEvent!.KeyInfo.Key);
        Assert.Equal('\0', keyEvent.KeyInfo.KeyChar);
        Assert.Equal(shift, keyEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift));
    }

    [Theory]
    [InlineData("[57441u")]
    [InlineData("[57448u")]
    [InlineData("[57441;1:3u")]
    [InlineData("[97;1:3u")]
    [InlineData("[20000u")]
    public void TryDecode_SwallowedSequence_ReturnsTrueWithoutEvent(string sequence)
    {
        var decoded = KittyCsiUKeyboardDecoder.TryDecode(sequence, out var keyEvent);

        Assert.True(decoded);
        Assert.Null(keyEvent);
    }

    [Theory]
    [InlineData("[bad;1u")]
    [InlineData("[13;0u")]
    [InlineData("[13;badu")]
    [InlineData("[13;1:badu")]
    [InlineData("[13;1x")]
    [InlineData("13;1u")]
    public void TryDecode_MalformedSequence_ReturnsFalse(string sequence)
    {
        var decoded = KittyCsiUKeyboardDecoder.TryDecode(sequence, out var keyEvent);

        Assert.False(decoded);
        Assert.Null(keyEvent);
    }
}
