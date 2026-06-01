// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Decodes SGR mouse sequences of the form <c>[&lt;button;x;yM</c> or <c>[&lt;button;x;ym</c>.
/// </summary>
internal static class SgrMouseDecoder
{
    /// <summary>
    /// Attempts to decode an SGR mouse sequence.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the sequence is a recognized SGR mouse sequence. Scroll sequences produce
    /// a <see cref="PointerInput"/>; click, release, and drag sequences are consumed with no event.
    /// </returns>
    public static bool TryDecode(InputSequence sequence, out PointerInput? result)
    {
        result = null;
        var text = sequence.Text;

        if (text.Length < 4 || text[0] != '[' || text[1] != '<')
            return false;

        var terminator = text[^1];
        if (terminator is not ('M' or 'm'))
            return false;

        var inner = text[2..^1];
        var semicolon = inner.IndexOf(';');
        if (semicolon < 0)
            return false;

        if (!int.TryParse(inner[..semicolon], out var button))
            return false;

        var (x, y) = ParseCoordinates(inner[(semicolon + 1)..]);

        result = button switch
        {
            64 => new PointerInput(PointerAction.Wheel, x, y, MouseButton.WheelUp),
            65 => new PointerInput(PointerAction.Wheel, x, y, MouseButton.WheelDown),
            _ => null,
        };

        return true;
    }

    private static (int X, int Y) ParseCoordinates(string coordinateText)
    {
        var semicolon = coordinateText.IndexOf(';');
        if (semicolon < 0)
            return (0, 0);

        return int.TryParse(coordinateText[..semicolon], out var x)
            && int.TryParse(coordinateText[(semicolon + 1)..], out var y)
            ? (x, y)
            : (0, 0);
    }
}
