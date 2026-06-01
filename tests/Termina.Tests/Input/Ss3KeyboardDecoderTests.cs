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
    public void TryDecode_KnownFinal_ReturnsKeyStroke(char final, ConsoleKey expectedKey)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            Context(kittyReportAllKeysVisible: false),
            out var inputEvent);

        Assert.True(decoded);
        var keyStroke = Assert.IsType<KeyStroke>(inputEvent);
        Assert.Equal(ToTerminaKey(expectedKey), keyStroke.Key);
        Assert.Equal(KeyEventPhase.Press, keyStroke.Phase);
        Assert.Equal(KeyModifiers.None, keyStroke.Modifiers);
        Assert.Null(keyStroke.Text);
    }

    [Theory]
    [InlineData('A', +1)]
    [InlineData('B', -1)]
    public void TryDecode_KittyVisibleVerticalArrow_ReturnsPointerWheel(char final, int expectedDelta)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            Context(kittyReportAllKeysVisible: true),
            out var inputEvent);

        Assert.True(decoded);
        var pointerInput = Assert.IsType<PointerInput>(inputEvent);
        Assert.Equal(PointerAction.Wheel, pointerInput.Action);
        Assert.Equal(expectedDelta > 0 ? MouseButton.WheelUp : MouseButton.WheelDown, pointerInput.Button);
    }

    [Theory]
    [InlineData('C', ConsoleKey.RightArrow)]
    [InlineData('D', ConsoleKey.LeftArrow)]
    public void TryDecode_KittyVisibleHorizontalArrow_ReturnsKeyStroke(char final, ConsoleKey expectedKey)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            Context(kittyReportAllKeysVisible: true),
            out var inputEvent);

        Assert.True(decoded);
        var keyStroke = Assert.IsType<KeyStroke>(inputEvent);
        Assert.Equal(ToTerminaKey(expectedKey), keyStroke.Key);
    }

    [Theory]
    [InlineData('E')]
    [InlineData('x')]
    public void TryDecode_UnknownFinal_ReturnsFalse(char final)
    {
        var decoded = Ss3KeyboardDecoder.TryDecode(
            final,
            Context(kittyReportAllKeysVisible: false),
            out var inputEvent);

        Assert.False(decoded);
        Assert.Null(inputEvent);
    }

    private static TerminalModeContext Context(bool kittyReportAllKeysVisible) =>
        new(kittyReportAllKeysVisible);

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
