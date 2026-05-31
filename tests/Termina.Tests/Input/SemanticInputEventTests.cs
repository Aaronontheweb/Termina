// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

public class SemanticInputEventTests
{
    [Fact]
    public void KeyStroke_DefaultsToPressWithoutModifiers()
    {
        var stroke = new KeyStroke(TerminaKey.Enter);

        Assert.Equal(TerminaKey.Enter, stroke.Key);
        Assert.Equal(KeyModifiers.None, stroke.Modifiers);
        Assert.Equal(KeyEventPhase.Press, stroke.Phase);
        Assert.Null(stroke.Text);
    }

    [Fact]
    public void KeyStroke_CanRepresentModifiedReleaseWithAssociatedText()
    {
        var stroke = new KeyStroke(
            TerminaKey.F5,
            KeyModifiers.Control | KeyModifiers.Shift,
            KeyEventPhase.Release,
            Text: "associated text");

        Assert.Equal(TerminaKey.F5, stroke.Key);
        Assert.True(stroke.Modifiers.HasFlag(KeyModifiers.Control));
        Assert.True(stroke.Modifiers.HasFlag(KeyModifiers.Shift));
        Assert.Equal(KeyEventPhase.Release, stroke.Phase);
        Assert.Equal("associated text", stroke.Text);
    }

    [Fact]
    public void TextEntered_CanCarryMultipleCharacters()
    {
        var entered = new TextEntered("hello");

        Assert.Equal("hello", entered.Text);
    }

    [Fact]
    public void PointerInput_CanRepresentWheelWithCoordinatesAndModifiers()
    {
        var pointer = new PointerInput(
            PointerAction.Wheel,
            X: 12,
            Y: 5,
            MouseButton.WheelDown,
            KeyModifiers.Alt);

        Assert.Equal(PointerAction.Wheel, pointer.Action);
        Assert.Equal(12, pointer.X);
        Assert.Equal(5, pointer.Y);
        Assert.Equal(MouseButton.WheelDown, pointer.Button);
        Assert.Equal(KeyModifiers.Alt, pointer.Modifiers);
    }

    [Fact]
    public void PasteInput_CarriesPasteTextAsSingleSemanticEvent()
    {
        var paste = new PasteInput("line 1\nline 2");

        Assert.Equal("line 1\nline 2", paste.Text);
    }

    [Fact]
    public void TerminalResizeInput_CarriesViewportDimensions()
    {
        var resize = new TerminalResizeInput(120, 40);

        Assert.Equal(120, resize.Width);
        Assert.Equal(40, resize.Height);
    }

    [Fact]
    public void TerminalReplyInput_CarriesRawReplySequence()
    {
        var reply = new TerminalReplyInput("[12;40R");

        Assert.Equal("[12;40R", reply.Sequence);
    }

    [Fact]
    public void ConsoleModifiers_RoundTripThroughTerminaSubset()
    {
        const ConsoleModifiers console = ConsoleModifiers.Control | ConsoleModifiers.Shift;

        var termina = console.ToTerminaKeyModifiers();

        Assert.True(termina.HasFlag(KeyModifiers.Control));
        Assert.True(termina.HasFlag(KeyModifiers.Shift));
        Assert.False(termina.HasFlag(KeyModifiers.Alt));
        Assert.Equal(console, termina.ToConsoleModifiers());
    }

    [Fact]
    public void TerminaOnlyModifiers_AreIgnoredWhenConvertingToConsoleModifiers()
    {
        const KeyModifiers modifiers = KeyModifiers.Super | KeyModifiers.Meta | KeyModifiers.Control;

        var console = modifiers.ToConsoleModifiers();

        Assert.Equal(ConsoleModifiers.Control, console);
    }
}
