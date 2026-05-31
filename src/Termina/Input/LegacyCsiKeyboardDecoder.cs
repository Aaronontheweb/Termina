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
    public static bool TryDecode(string sequence, out KeyPressed? result)
    {
        result = null;
        if (sequence.Length < 3 || sequence[0] != '[' || sequence[^1] != '~')
            return false;

        var inner = sequence[1..^1];
        var semicolon = inner.IndexOf(';');
        var keyPart = semicolon < 0 ? inner : inner[..semicolon];
        if (!int.TryParse(keyPart, out var keyCode))
            return false;

        var consoleKey = LegacyTildeCodeToKey(keyCode);
        if (consoleKey == ConsoleKey.None)
            return false;

        var modValue = 1;
        if (semicolon >= 0)
        {
            var modPart = inner[(semicolon + 1)..];
            if (!int.TryParse(modPart, out modValue) || modValue < 1)
                return false;
        }

        var modBits = modValue - 1;
        var shift = (modBits & 1) != 0;
        var alt = (modBits & 2) != 0;
        var ctrl = (modBits & 4) != 0;

        result = new KeyPressed(new ConsoleKeyInfo('\0', consoleKey, shift, alt, ctrl));
        return true;
    }

    private static ConsoleKey LegacyTildeCodeToKey(int code) => code switch
    {
        1 or 7 => ConsoleKey.Home,
        2 => ConsoleKey.Insert,
        3 => ConsoleKey.Delete,
        4 or 8 => ConsoleKey.End,
        5 => ConsoleKey.PageUp,
        6 => ConsoleKey.PageDown,
        11 => ConsoleKey.F1,
        12 => ConsoleKey.F2,
        13 => ConsoleKey.F3,
        14 => ConsoleKey.F4,
        15 => ConsoleKey.F5,
        17 => ConsoleKey.F6,
        18 => ConsoleKey.F7,
        19 => ConsoleKey.F8,
        20 => ConsoleKey.F9,
        21 => ConsoleKey.F10,
        23 => ConsoleKey.F11,
        24 => ConsoleKey.F12,
        _ => ConsoleKey.None,
    };
}
