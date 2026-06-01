// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Decodes CSI functional final bytes used by arrows, Home/End, and F1-F4.
/// </summary>
internal static class CsiFunctionalDecoder
{
    /// <summary>
    /// Attempts to decode a bare CSI functional sequence final such as <c>ESC[A</c>.
    /// </summary>
    public static bool TryDecodeBareFinal(
        char final,
        bool kittyReportAllKeysVisible,
        out IInputEvent? result)
    {
        result = null;

        if (!IsFunctionalFinal(final))
            return false;

        if (kittyReportAllKeysVisible)
        {
            var key = FinalToKey(final);
            if (key != ConsoleKey.None)
                result = new KeyPressed(new ConsoleKeyInfo('\0', key, false, false, false));

            return true;
        }

        if (final is 'A' or 'B')
            result = new MouseScrollEvent(final == 'A' ? +1 : -1);

        return true;
    }

    /// <summary>True for CSI finals that represent an arrow / Home/End / F1-F4 / KP_Begin.</summary>
    /// <remarks>
    /// <c>'E'</c> is KP_Begin (numpad-5 / "center" with NumLock off). It is recognized so
    /// <c>ESC[E</c> / <c>ESC[1;&lt;mods&gt;E</c> are consumed rather than flushed as raw keys, but
    /// <see cref="FinalToKey"/> returns <see cref="ConsoleKey.None"/> because there is no
    /// equivalent <see cref="ConsoleKey"/>.
    /// </remarks>
    public static bool IsFunctionalFinal(char c) =>
        c is 'A' or 'B' or 'C' or 'D' or 'E' or 'F' or 'H' or 'P' or 'Q' or 'R' or 'S';

    /// <summary>Maps a CSI functional final char to a <see cref="ConsoleKey"/>.</summary>
    public static ConsoleKey FinalToKey(char c) => c switch
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
}
