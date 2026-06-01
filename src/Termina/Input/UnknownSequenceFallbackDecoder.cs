// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Handles incomplete and unrecognized CSI sequences without leaving the parser stuck.
/// </summary>
internal static class UnknownSequenceFallbackDecoder
{
    /// <summary>
    /// Returns <c>true</c> if the accumulated bracket sequence could still lead to a recognized
    /// escape sequence.
    /// </summary>
    public static bool CouldLeadToRecognizedSequence(InputSequence sequence)
    {
        var text = sequence.Text;

        if (BracketedPasteDecoder.CouldBeStartSequence(sequence))
            return true;

        // Complete but still-unrecognized CSI tilde-terminated sequences flush as raw
        // KeyPressed events rather than keeping the parser stuck in InBracketSequence.
        if (text.Length >= 3 && text[^1] == '~')
            return false;

        // Could be a CSI u sequence "[keycode;modifiersu".
        if (text.Length >= 2
            && text.Length <= 32
            && (char.IsDigit(text[1]) || text[1] == ';' || text[1] == ':'))
            return true;

        // Could be an SGR mouse event "[<button;x;yM".
        if (text.Length >= 2 && text[1] == '<' && text.Length <= 30)
            return true;

        // Could still become a bare CSI functional final: "[A" / "[B" etc.
        if (text == "[")
            return true;

        return false;
    }

    public static IReadOnlyList<IInputEvent> ToRawKeyEvents(string sequence)
    {
        var results = new List<IInputEvent>(sequence.Length + 1);
        AppendRawKeyEvents(sequence, results);
        return results;
    }

    public static void AppendRawKeyEvents(string sequence, List<IInputEvent> results)
    {
        results.Add(new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)));
        foreach (var c in sequence)
            results.Add(new KeyPressed(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false)));
    }

    public static void AppendRawSs3KeyEvents(char final, List<IInputEvent> results)
    {
        results.Add(new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)));
        results.Add(new KeyPressed(new ConsoleKeyInfo('O', ConsoleKey.O, false, false, false)));
        results.Add(new KeyPressed(new ConsoleKeyInfo(final, ConsoleKey.None, false, false, false)));
    }
}
