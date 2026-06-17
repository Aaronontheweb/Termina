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
    public void TryDecodeBareFinal_KittyVisible_ReturnsKeyStroke(char final, ConsoleKey expectedKey)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            Context(kittyReportAllKeysVisible: true),
            out var inputEvent);

        Assert.True(decoded);
        var keyStroke = Assert.IsType<KeyStroke>(inputEvent);
        Assert.Equal(ToTerminaKey(expectedKey), keyStroke.Key);
        Assert.Equal(KeyEventPhase.Press, keyStroke.Phase);
        Assert.Equal(KeyModifiers.None, keyStroke.Modifiers);
        Assert.Null(keyStroke.Text);
    }

    [Fact]
    public void TryDecodeBareFinal_KittyVisible_KpBeginConsumesWithoutEvent()
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            'E',
            Context(kittyReportAllKeysVisible: true),
            out var inputEvent);

        Assert.True(decoded);
        Assert.Null(inputEvent);
    }

    [Theory]
    [InlineData('A', +1)]
    [InlineData('B', -1)]
    public void TryDecodeBareFinal_DeckmConfirmed_VerticalArrowReturnsPointerWheel(char final, int expectedDelta)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            Context(kittyReportAllKeysVisible: false, deckmConfirmed: true),
            out var inputEvent);

        Assert.True(decoded);
        var pointerInput = Assert.IsType<PointerInput>(inputEvent);
        Assert.Equal(PointerAction.Wheel, pointerInput.Action);
        Assert.Equal(expectedDelta > 0 ? MouseButton.WheelUp : MouseButton.WheelDown, pointerInput.Button);
    }

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
    public void TryDecodeBareFinal_DeckmNotConfirmed_ReturnsKeyStroke(char final, ConsoleKey expectedKey)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            Context(kittyReportAllKeysVisible: false, deckmConfirmed: false),
            out var inputEvent);

        Assert.True(decoded);
        var keyStroke = Assert.IsType<KeyStroke>(inputEvent);
        Assert.Equal(ToTerminaKey(expectedKey), keyStroke.Key);
    }

    [Fact]
    public void TryDecodeBareFinal_DeckmNotConfirmed_KpBeginConsumesWithoutEvent()
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            'E',
            Context(kittyReportAllKeysVisible: false, deckmConfirmed: false),
            out var inputEvent);

        Assert.True(decoded);
        Assert.Null(inputEvent);
    }

    [Theory]
    [InlineData('C')]
    [InlineData('D')]
    [InlineData('H')]
    [InlineData('F')]
    [InlineData('P')]
    [InlineData('Q')]
    [InlineData('R')]
    [InlineData('S')]
    public void TryDecodeBareFinal_DeckmConfirmed_NonWheelFunctionalReturnsKeyStroke(char final)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            Context(kittyReportAllKeysVisible: false, deckmConfirmed: true),
            out var inputEvent);

        Assert.True(decoded);
        var keyStroke = Assert.IsType<KeyStroke>(inputEvent);
        Assert.NotEqual(TerminaKey.None, keyStroke.Key);
    }

    [Fact]
    public void TryDecodeBareFinal_BacktabZ_ReturnsTabWithShift()
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            'Z',
            Context(kittyReportAllKeysVisible: false, deckmConfirmed: false),
            out var inputEvent);

        Assert.True(decoded);
        var keyStroke = Assert.IsType<KeyStroke>(inputEvent);
        Assert.Equal(TerminaKey.Tab, keyStroke.Key);
        Assert.Equal(KeyModifiers.Shift, keyStroke.Modifiers);
    }

    [Fact]
    public void TryDecodeBareFinal_BacktabZ_KittyVisible_ReturnsTabWithShift()
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            'Z',
            Context(kittyReportAllKeysVisible: true),
            out var inputEvent);

        Assert.True(decoded);
        var keyStroke = Assert.IsType<KeyStroke>(inputEvent);
        Assert.Equal(TerminaKey.Tab, keyStroke.Key);
        Assert.Equal(KeyModifiers.Shift, keyStroke.Modifiers);
    }

    [Fact]
    public void IsFunctionalFinal_Z_ReturnsTrue()
    {
        Assert.True(CsiFunctionalDecoder.IsFunctionalFinal('Z'));
    }

    [Theory]
    [InlineData('~')]
    [InlineData('u')]
    [InlineData('x')]
    public void TryDecodeBareFinal_NonFunctionalFinal_ReturnsFalse(char final)
    {
        var decoded = CsiFunctionalDecoder.TryDecodeBareFinal(
            final,
            Context(kittyReportAllKeysVisible: true),
            out var inputEvent);

        Assert.False(decoded);
        Assert.Null(inputEvent);
    }

    private static TerminalModeContext Context(
        bool kittyReportAllKeysVisible,
        bool deckmConfirmed = false) =>
        new(kittyReportAllKeysVisible, deckmConfirmed);

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
}
