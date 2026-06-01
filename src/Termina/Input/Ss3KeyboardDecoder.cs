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
    public static bool TryDecode(char final, TerminalModeContext context, out TerminaInputEvent? result)
    {
        result = null;

        if (context.KittyReportAllKeysVisible && final is 'A' or 'B')
        {
            result = new PointerInput(
                PointerAction.Wheel,
                X: 0,
                Y: 0,
                final == 'A' ? MouseButton.WheelUp : MouseButton.WheelDown);
            return true;
        }

        var key = final switch
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

        if (key == TerminaKey.None)
            return false;

        result = new KeyStroke(key);
        return true;
    }
}
