// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Decodes kitty keyboard protocol CSI-u sequences.
/// </summary>
internal static class KittyCsiUKeyboardDecoder
{
    /// <summary>
    /// Attempts to parse <c>[keycode[;modifiers[:event][;text...]]u</c>.
    /// </summary>
    /// <param name="sequence">The buffered sequence, e.g. <c>[13;5u</c>.</param>
    /// <param name="result">The resulting <see cref="KeyPressed"/> event, if one should be emitted.</param>
    /// <returns><c>true</c> if the sequence was parsed, whether or not an event is produced.</returns>
    public static bool TryDecode(InputSequence sequence, out KeyPressed? result)
    {
        result = null;
        var text = sequence.Text;

        if (text.Length < 3 || text[0] != '[' || text[^1] != 'u')
            return false;

        var inner = text[1..^1];
        var semicolon = inner.IndexOf(';');

        int keycode;
        var modValue = 1;

        if (semicolon < 0)
        {
            var keyPart = inner;
            var sub = keyPart.IndexOf(':');
            if (sub >= 0)
                keyPart = keyPart[..sub];

            if (!int.TryParse(keyPart, out keycode))
                return false;
        }
        else
        {
            var keyPart = inner[..semicolon];
            var subKey = keyPart.IndexOf(':');
            if (subKey >= 0)
                keyPart = keyPart[..subKey];

            if (!int.TryParse(keyPart, out keycode))
                return false;

            var rest = inner[(semicolon + 1)..];
            var nextSemi = rest.IndexOf(';');
            if (nextSemi >= 0)
                rest = rest[..nextSemi];

            var modPart = rest;
            var colon = rest.IndexOf(':');
            if (colon >= 0)
            {
                modPart = rest[..colon];
                if (!int.TryParse(rest[(colon + 1)..], out var eventType))
                    return false;

                if (eventType != 1)
                    return true;
            }

            if (!int.TryParse(modPart, out modValue) || modValue < 1)
                return false;
        }

        // Modifier-key-alone events add noise without actionable ConsoleKey equivalents.
        if (keycode is >= 57441 and <= 57454)
            return true;

        var modBits = modValue - 1;
        var shift = (modBits & 1) != 0;
        var alt = (modBits & 2) != 0;
        var ctrl = (modBits & 4) != 0;

        var (consoleKey, keyChar) = MapKeycodeToConsoleKey(keycode);
        if (consoleKey == ConsoleKey.None && keyChar == '\0')
            return true;

        result = new KeyPressed(new ConsoleKeyInfo(keyChar, consoleKey, shift, alt, ctrl));
        return true;
    }

    private static (ConsoleKey Key, char Char) MapKeycodeToConsoleKey(int keycode) => keycode switch
    {
        13 => (ConsoleKey.Enter, '\r'),
        9 => (ConsoleKey.Tab, '\t'),
        127 => (ConsoleKey.Backspace, '\b'),
        27 => (ConsoleKey.Escape, '\x1b'),
        >= 32 and <= 126 => (CharToConsoleKey((char)keycode), (char)keycode),
        57344 => (ConsoleKey.Escape, '\x1b'),
        57345 => (ConsoleKey.Enter, '\r'),
        57346 => (ConsoleKey.Tab, '\t'),
        57347 => (ConsoleKey.Backspace, '\b'),
        57348 => (ConsoleKey.Insert, '\0'),
        57349 => (ConsoleKey.Delete, '\0'),
        57350 => (ConsoleKey.LeftArrow, '\0'),
        57351 => (ConsoleKey.RightArrow, '\0'),
        57352 => (ConsoleKey.UpArrow, '\0'),
        57353 => (ConsoleKey.DownArrow, '\0'),
        57354 => (ConsoleKey.PageUp, '\0'),
        57355 => (ConsoleKey.PageDown, '\0'),
        57356 => (ConsoleKey.Home, '\0'),
        57357 => (ConsoleKey.End, '\0'),
        57358 => (ConsoleKey.None, '\0'),
        >= 57364 and <= 57375 => (ConsoleKey.F1 + (keycode - 57364), '\0'),
        _ => (ConsoleKey.None, keycode < 128 ? (char)keycode : '\0')
    };

    private static ConsoleKey CharToConsoleKey(char c) => c switch
    {
        >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
        >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
        >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
        ' ' => ConsoleKey.Spacebar,
        _ => ConsoleKey.None
    };
}
