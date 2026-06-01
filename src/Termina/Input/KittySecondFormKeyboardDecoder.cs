// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Decodes kitty keyboard protocol second-form CSI keyboard sequences.
/// </summary>
internal static class KittySecondFormKeyboardDecoder
{
    /// <summary>
    /// Attempts to parse <c>[1;modifiers[:event] final</c>.
    /// </summary>
    /// <param name="sequence">The buffered sequence, e.g. <c>[1;5A</c> or <c>[1;2:1A</c>.</param>
    /// <param name="result">
    /// On <c>true</c> return: the resulting semantic key event, or <c>null</c> when a recognized
    /// sequence does not map to a supported key or event phase.
    /// </param>
    /// <returns><c>true</c> if the sequence was recognized, whether or not an event is produced.</returns>
    public static bool TryDecode(InputSequence sequence, out KeyStroke? result)
    {
        result = null;
        var text = sequence.Text;
        if (text.Length < 3 || text[0] != '[')
            return false;

        var final = text[^1];
        var key = ToTerminaKey(CsiFunctionalDecoder.FinalToKey(final));
        if (key == TerminaKey.None)
            return false;

        var inner = text[1..^1];
        var semicolon = inner.IndexOf(';');
        if (semicolon < 0)
            return false;

        if (inner[..semicolon] != "1")
            return false;

        var rest = inner[(semicolon + 1)..];
        var colon = rest.IndexOf(':');
        var modPart = colon < 0 ? rest : rest[..colon];
        if (!int.TryParse(modPart, out var modValue) || modValue < 1)
            return false;

        var phase = KeyEventPhase.Press;
        if (colon >= 0)
        {
            if (!int.TryParse(rest[(colon + 1)..], out var eventType))
                return false;

            phase = eventType switch
            {
                1 => KeyEventPhase.Press,
                2 => KeyEventPhase.Repeat,
                3 => KeyEventPhase.Release,
                _ => phase,
            };

            if (eventType is not (1 or 2 or 3))
                return true;
        }

        var modBits = modValue - 1;
        var modifiers = KeyModifiers.None;
        if ((modBits & 1) != 0) modifiers |= KeyModifiers.Shift;
        if ((modBits & 2) != 0) modifiers |= KeyModifiers.Alt;
        if ((modBits & 4) != 0) modifiers |= KeyModifiers.Control;

        result = new KeyStroke(key, modifiers, phase);
        return true;
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
}
