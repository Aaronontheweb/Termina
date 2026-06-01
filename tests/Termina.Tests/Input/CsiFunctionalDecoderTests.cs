// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class CsiFunctionalDecoderTests
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
    public void TryDecodeBareFinal_KittyVisible_ReturnsKeyPressed(char final, ConsoleKey expectedKey)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            kittyReportAllKeysVisible: true,
            out var inputEvent);

        Assert.True(decoded);
        var pressed = Assert.IsType<KeyPressed>(inputEvent);
        Assert.Equal(expectedKey, pressed.KeyInfo.Key);
        Assert.Equal('\0', pressed.KeyInfo.KeyChar);
    }

    [Fact]
    public void TryDecodeBareFinal_KittyVisible_KpBeginConsumesWithoutEvent()
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            'E',
            kittyReportAllKeysVisible: true,
            out var inputEvent);

        Assert.True(decoded);
        Assert.Null(inputEvent);
    }

    [Theory]
    [InlineData('A', +1)]
    [InlineData('B', -1)]
    public void TryDecodeBareFinal_KittyInactive_VerticalArrowReturnsMouseScroll(char final, int expectedDelta)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            kittyReportAllKeysVisible: false,
            out var inputEvent);

        Assert.True(decoded);
        var scroll = Assert.IsType<MouseScrollEvent>(inputEvent);
        Assert.Equal(expectedDelta, scroll.Delta);
    }

    [Theory]
    [InlineData('C')]
    [InlineData('D')]
    [InlineData('E')]
    [InlineData('H')]
    [InlineData('F')]
    [InlineData('P')]
    [InlineData('Q')]
    [InlineData('R')]
    [InlineData('S')]
    public void TryDecodeBareFinal_KittyInactive_OtherFunctionalFinalConsumesWithoutEvent(char final)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            kittyReportAllKeysVisible: false,
            out var inputEvent);

        Assert.True(decoded);
        Assert.Null(inputEvent);
    }

    [Theory]
    [InlineData('~')]
    [InlineData('u')]
    [InlineData('x')]
    public void TryDecodeBareFinal_NonFunctionalFinal_ReturnsFalse(char final)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            kittyReportAllKeysVisible: true,
            out var inputEvent);

        Assert.False(decoded);
        Assert.Null(inputEvent);
    }
}
