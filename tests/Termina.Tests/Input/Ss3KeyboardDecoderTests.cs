// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class Ss3KeyboardDecoderTests
{
    [Theory]
    [InlineData('A', ConsoleKey.UpArrow)]
    [InlineData('B', ConsoleKey.DownArrow)]
    [InlineData('C', ConsoleKey.RightArrow)]
    [InlineData('D', ConsoleKey.LeftArrow)]
    [InlineData('H', ConsoleKey.Home)]
    [InlineData('F', ConsoleKey.End)]
    [InlineData('P', ConsoleKey.F1)]
    [InlineData('Q', ConsoleKey.F2)]
    [InlineData('R', ConsoleKey.F3)]
    [InlineData('S', ConsoleKey.F4)]
    public void TryDecode_KnownFinal_ReturnsKeyPressed(char final, ConsoleKey expectedKey)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            kittyReportAllKeysVisible: false,
            out var inputEvent);

        Assert.True(decoded);
        var pressed = Assert.IsType<KeyPressed>(inputEvent);
        Assert.Equal(expectedKey, pressed.KeyInfo.Key);
        Assert.Equal('\0', pressed.KeyInfo.KeyChar);
    }

    [Theory]
    [InlineData('A', +1)]
    [InlineData('B', -1)]
    public void TryDecode_KittyVisibleVerticalArrow_ReturnsMouseScroll(char final, int expectedDelta)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            kittyReportAllKeysVisible: true,
            out var inputEvent);

        Assert.True(decoded);
        var scroll = Assert.IsType<MouseScrollEvent>(inputEvent);
        Assert.Equal(expectedDelta, scroll.Delta);
    }

    [Theory]
    [InlineData('C', ConsoleKey.RightArrow)]
    [InlineData('D', ConsoleKey.LeftArrow)]
    public void TryDecode_KittyVisibleHorizontalArrow_ReturnsKeyPressed(char final, ConsoleKey expectedKey)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            kittyReportAllKeysVisible: true,
            out var inputEvent);

        Assert.True(decoded);
        var pressed = Assert.IsType<KeyPressed>(inputEvent);
        Assert.Equal(expectedKey, pressed.KeyInfo.Key);
    }

    [Theory]
    [InlineData('E')]
    [InlineData('x')]
    public void TryDecode_UnknownFinal_ReturnsFalse(char final)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            kittyReportAllKeysVisible: false,
            out var inputEvent);

        Assert.False(decoded);
        Assert.Null(inputEvent);
    }
}
