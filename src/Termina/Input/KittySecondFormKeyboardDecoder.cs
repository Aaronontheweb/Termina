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
    /// On <c>true</c> return: the resulting <see cref="KeyPressed"/> when this was a press event,
    /// or <c>null</c> when a repeat/release event was parsed and intentionally swallowed.
    /// </param>
    /// <returns><c>true</c> if the sequence was recognized, whether or not an event is produced.</returns>
    public static bool TryDecode(InputSequence sequence, out KeyPressed? result)
    {
        result = null;
        var text = sequence.Text;
        if (text.Length < 3 || text[0] != '[')
            return false;

        var final = text[^1];
        var key = CsiFunctionalDecoder.FinalToKey(final);
        if (key == ConsoleKey.None)
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

        if (colon >= 0)
        {
            if (!int.TryParse(rest[(colon + 1)..], out var eventType))
                return false;

            if (eventType != 1)
                return true;
        }

        var modBits = modValue - 1;
        var shift = (modBits & 1) != 0;
        var alt = (modBits & 2) != 0;
        var ctrl = (modBits & 4) != 0;

        result = new KeyPressed(new ConsoleKeyInfo('\0', key, shift, alt, ctrl));
        return true;
    }
}
