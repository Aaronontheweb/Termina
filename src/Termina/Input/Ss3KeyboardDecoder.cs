// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Decodes SS3 keyboard sequences of the form <c>ESC O final</c>.
/// </summary>
internal static class Ss3KeyboardDecoder
{
    /// <summary>
    /// Attempts to decode the final byte from an SS3 sequence.
    /// </summary>
    public static bool TryDecode(char final, bool kittyReportAllKeysVisible, out IInputEvent? result)
    {
        result = null;

        if (kittyReportAllKeysVisible && final is 'A' or 'B')
        {
            result = new MouseScrollEvent(final == 'A' ? +1 : -1);
            return true;
        }

        var key = final switch
        {
            'A' => ConsoleKey.UpArrow,
            'B' => ConsoleKey.DownArrow,
            'C' => ConsoleKey.RightArrow,
            'D' => ConsoleKey.LeftArrow,
            'H' => ConsoleKey.Home,
            'F' => ConsoleKey.End,
            'P' => ConsoleKey.F1,
            'Q' => ConsoleKey.F2,
            'R' => ConsoleKey.F3,
            'S' => ConsoleKey.F4,
            _ => ConsoleKey.None,
        };

        if (key == ConsoleKey.None)
            return false;

        result = new KeyPressed(new ConsoleKeyInfo('\0', key, false, false, false));
        return true;
    }
}
