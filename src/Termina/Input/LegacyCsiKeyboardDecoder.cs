// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Decodes legacy xterm/VT CSI tilde keyboard sequences.
/// </summary>
internal static class LegacyCsiKeyboardDecoder
{
    /// <summary>
    /// Attempts to decode <c>[num~</c> or <c>[num;modifiers~</c> into a keyboard event.
    /// </summary>
    public static bool TryDecode(InputSequence sequence, out KeyStroke? result)
    {
        result = null;
        var text = sequence.Text;
        if (text.Length < 3 || text[0] != '[' || text[^1] != '~')
            return false;

        var inner = text[1..^1];
        var semicolon = inner.IndexOf(';');
        var keyPart = semicolon < 0 ? inner : inner[..semicolon];
        if (!int.TryParse(keyPart, out var keyCode))
            return false;

        var key = LegacyTildeCodeToKey(keyCode);
        if (key == TerminaKey.None)
            return false;

        var modValue = 1;
        if (semicolon >= 0)
        {
            var modPart = inner[(semicolon + 1)..];
            if (!int.TryParse(modPart, out modValue) || modValue < 1)
                return false;
        }

        var modBits = modValue - 1;
        var modifiers = KeyModifiers.None;
        if ((modBits & 1) != 0) modifiers |= KeyModifiers.Shift;
        if ((modBits & 2) != 0) modifiers |= KeyModifiers.Alt;
        if ((modBits & 4) != 0) modifiers |= KeyModifiers.Control;

        result = new KeyStroke(key, modifiers);
        return true;
    }

    private static TerminaKey LegacyTildeCodeToKey(int code) => code switch
    {
        1 or 7 => TerminaKey.Home,
        2 => TerminaKey.Insert,
        3 => TerminaKey.Delete,
        4 or 8 => TerminaKey.End,
        5 => TerminaKey.PageUp,
        6 => TerminaKey.PageDown,
        11 => TerminaKey.F1,
        12 => TerminaKey.F2,
        13 => TerminaKey.F3,
        14 => TerminaKey.F4,
        15 => TerminaKey.F5,
        17 => TerminaKey.F6,
        18 => TerminaKey.F7,
        19 => TerminaKey.F8,
        20 => TerminaKey.F9,
        21 => TerminaKey.F10,
        23 => TerminaKey.F11,
        24 => TerminaKey.F12,
        _ => TerminaKey.None,
    };
}
