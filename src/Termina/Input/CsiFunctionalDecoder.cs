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
        TerminalModeContext context,
        out TerminaInputEvent? result)
    {
        result = null;

        if (!IsFunctionalFinal(final))
            return false;

        if (context.KittyReportAllKeysVisible)
        {
            var key = FinalToTerminaKey(final);
            if (key != TerminaKey.None)
                result = new KeyStroke(key);

            return true;
        }

        if (final is 'A' or 'B' && context.DeckmConfirmed)
        {
            // DECCKM is confirmed (we've seen SS3 arrow keys), so CSI A/B can only be
            // alternate-scroll wheel events — real arrows arrive as SS3.
            result = new PointerInput(
                PointerAction.Wheel,
                X: 0,
                Y: 0,
                final == 'A' ? MouseButton.WheelUp : MouseButton.WheelDown);
        }
        else
        {
            // DECCKM not yet confirmed — treat CSI A/B/C/D as keyboard arrows.
            // Terminals that ignore DECCKM (VHS, some PTY wrappers) send arrows
            // as CSI form; misrouting them as wheel breaks all navigation.
            var key = FinalToTerminaKey(final);
            if (key != TerminaKey.None)
                result = new KeyStroke(key);
        }

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

    private static TerminaKey FinalToTerminaKey(char c) => c switch
    {
        'A' => TerminaKey.UpArrow,
        'B' => TerminaKey.DownArrow,
        'C' => TerminaKey.RightArrow,
        'D' => TerminaKey.LeftArrow,
        'H' => TerminaKey.Home,
        'F' => TerminaKey.End,
        'P' => TerminaKey.F1,
        'Q' => TerminaKey.F2,
        'R' => TerminaKey.F3,
        'S' => TerminaKey.F4,
        _ => TerminaKey.None,
    };
}
