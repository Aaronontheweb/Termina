// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Termina.Input;

/// <summary>
/// Decodes content between bracketed paste start and end sentinels.
/// </summary>
internal sealed class BracketedPasteDecoder
{
    private const string StartSequence = "[200~";
    private const string EndSequence = "\x1b[201~";

    private readonly StringBuilder _buffer = new();
    private int _pendingEndSequencePosition;

    public static bool IsStartSequence(InputSequence sequence) => sequence.Text == StartSequence;

    public static bool CouldBeStartSequence(InputSequence sequence) =>
        sequence.Length <= StartSequence.Length && StartSequence.StartsWith(sequence.Text, StringComparison.Ordinal);

    public void Begin()
    {
        _buffer.Clear();
        _pendingEndSequencePosition = 0;
    }

    public bool TryConsume(ConsoleKeyInfo key, out PasteEvent? pasteEvent)
    {
        pasteEvent = null;

        var expectedChar = _pendingEndSequencePosition == 0
            ? '\x1b'
            : EndSequence[_pendingEndSequencePosition];
        var isEscChar = key.Key == ConsoleKey.Escape || key.KeyChar == '\x1b';
        var charMatches = _pendingEndSequencePosition == 0
            ? isEscChar
            : key.KeyChar == expectedChar;

        if (charMatches)
        {
            _pendingEndSequencePosition++;
            if (_pendingEndSequencePosition == EndSequence.Length)
            {
                pasteEvent = new PasteEvent(_buffer.ToString());
                Begin();
                return true;
            }
        }
        else if (_pendingEndSequencePosition > 0)
        {
            _buffer.Append(EndSequence[.._pendingEndSequencePosition]);
            _pendingEndSequencePosition = 0;
            _buffer.Append(key.KeyChar);
        }
        else
        {
            _buffer.Append(key.KeyChar);
        }

        return false;
    }
}
