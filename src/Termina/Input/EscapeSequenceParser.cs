// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Text;
using Termina.Diagnostics;

namespace Termina.Input;

/// <summary>
/// Parses escape sequences from individual <see cref="ConsoleKeyInfo"/> values returned by
/// <see cref="Console.ReadKey(bool)"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>Console.ReadKey</c> does not parse terminal mouse or paste escape sequences — it returns
/// each byte of the sequence as a separate <see cref="ConsoleKeyInfo"/>. This parser buffers
/// those individual key events and detects:
/// </para>
/// <list type="bullet">
///   <item><description>Bracketed paste: <c>ESC[200~</c>…content…<c>ESC[201~</c> → <see cref="PasteEvent"/></description></item>
///   <item><description>SGR mouse scroll up: <c>ESC[&lt;64;x;yM</c> → <see cref="MouseScrollEvent"/>(<c>+1</c>)</description></item>
///   <item><description>SGR mouse scroll down: <c>ESC[&lt;65;x;yM</c> → <see cref="MouseScrollEvent"/>(<c>-1</c>)</description></item>
///   <item><description>Other SGR mouse events (clicks, releases): silently consumed — prevents spurious <c>ESC</c> keypresses.</description></item>
///   <item><description>Standalone <c>ESC</c>: emitted as <see cref="KeyPressed"/> after a 50 ms timeout with no follow-up character.</description></item>
/// </list>
/// <para>
/// Call <see cref="Process"/> for each key received. When <see cref="IsBufferingEscape"/> is
/// <c>true</c>, the caller should use a short read timeout (≈50 ms) and call
/// <see cref="CheckEscapeTimeout"/> when no key arrives in time.
/// </para>
/// </remarks>
internal sealed class EscapeSequenceParser
{
    private enum State { Normal, AfterEscape, InBracketSequence, PasteBuffering }

    private State _state = State.Normal;
    private readonly StringBuilder _seqBuffer = new();
    private readonly StringBuilder _pasteBuffer = new();
    private long _escapeReceivedAt;
    private int _pendingEndSeqPos;

    private const string PasteEndSeq = "\x1b[201~";

    // Injected clock for testing; defaults to Environment.TickCount64
    private readonly Func<long> _getTick;

    /// <summary>
    /// Creates a parser using the system clock.
    /// </summary>
    public EscapeSequenceParser() : this(null) { }

    /// <summary>
    /// Creates a parser with an injectable clock function.
    /// Pass a custom <paramref name="getTick"/> for deterministic unit tests.
    /// </summary>
    /// <param name="getTick">Returns the current tick in milliseconds. If null, uses <see cref="Environment.TickCount64"/>.</param>
    internal EscapeSequenceParser(Func<long>? getTick)
    {
        _getTick = getTick ?? (() => Environment.TickCount64);
    }

    /// <summary>
    /// Whether the parser is currently buffering an incomplete escape sequence.
    /// When <c>true</c>, the caller should use a short read timeout so that a standalone
    /// <c>ESC</c> key (with no follow-up) can be flushed via <see cref="CheckEscapeTimeout"/>.
    /// </summary>
    public bool IsBufferingEscape =>
        _state == State.AfterEscape || _state == State.InBracketSequence;

    /// <summary>
    /// If 50 ms have elapsed since an <c>ESC</c> was buffered with no follow-up character,
    /// resets state and returns a <see cref="KeyPressed"/> for the standalone <c>ESC</c>.
    /// Returns <c>null</c> if no timeout has occurred.
    /// </summary>
    public KeyPressed? CheckEscapeTimeout()
    {
        if (_state == State.AfterEscape && _getTick() - _escapeReceivedAt > 50)
        {
            _state = State.Normal;
            return new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
        }
        return null;
    }

    /// <summary>
    /// Processes one key from <see cref="Console.ReadKey(bool)"/> and returns the application
    /// events that should be emitted as a result. The list may be empty (e.g., while buffering
    /// a multi-character escape sequence or silently consuming a mouse click event).
    /// </summary>
    /// <param name="key">The key to process.</param>
    /// <returns>Zero or more <see cref="IInputEvent"/> instances to emit.</returns>
    public IReadOnlyList<IInputEvent> Process(ConsoleKeyInfo key)
    {
        var results = new List<IInputEvent>(2);
        ProcessCore(key, results);
        return results;
    }

    private void ProcessCore(ConsoleKeyInfo key, List<IInputEvent> results)
    {
        TerminaTrace.Input.Trace(this, "ESP: state={0} KeyChar=0x{1:X4} Key={2}",
            _state, (int)key.KeyChar, key.Key);

        switch (_state)
        {
            case State.Normal:
                if (key.Key == ConsoleKey.Escape)
                {
                    _escapeReceivedAt = _getTick();
                    _state = State.AfterEscape;
                    TerminaTrace.Input.Debug(this, "ESP: ESC received → AfterEscape");
                }
                else
                {
                    results.Add(new KeyPressed(key));
                }
                break;

            case State.AfterEscape:
                if (key.KeyChar == '[')
                {
                    _seqBuffer.Clear();
                    _seqBuffer.Append('[');
                    _state = State.InBracketSequence;
                }
                else
                {
                    // Not a CSI sequence — flush ESC, then re-process this key as Normal
                    results.Add(new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)));
                    _state = State.Normal;
                    ProcessCore(key, results);
                }
                break;

            case State.InBracketSequence:
                _seqBuffer.Append(key.KeyChar);
                var seq = _seqBuffer.ToString();

                // Bracketed paste start: ESC[200~
                if (seq == "[200~")
                {
                    _pasteBuffer.Clear();
                    _pendingEndSeqPos = 0;
                    _state = State.PasteBuffering;
                    TerminaTrace.Input.Debug(this, "ESP: Paste start detected → PasteBuffering");
                    break;
                }

                // SGR mouse event: ESC[<button;x;yM (press) or ESC[<button;x;ym (release)
                if (seq.Length >= 2 && seq[1] == '<' && (key.KeyChar == 'M' || key.KeyChar == 'm'))
                {
                    EmitMouseSgrEvent(seq, results);
                    _seqBuffer.Clear();
                    _state = State.Normal;
                    break;
                }

                // If this sequence can no longer match any recognized pattern, flush as raw keys
                if (!CouldLeadToRecognizedSequence(seq))
                {
                    results.Add(new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)));
                    foreach (var c in seq)
                    {
                        results.Add(new KeyPressed(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false)));
                    }
                    _seqBuffer.Clear();
                    _state = State.Normal;
                }
                break;

            case State.PasteBuffering:
                // Detect ESC[201~ end sentinel one character at a time
                var expectedChar = _pendingEndSeqPos == 0 ? '\x1b' : PasteEndSeq[_pendingEndSeqPos];
                var isEscChar = key.Key == ConsoleKey.Escape || key.KeyChar == '\x1b';
                var charMatches = _pendingEndSeqPos == 0 ? isEscChar : key.KeyChar == expectedChar;

                if (charMatches)
                {
                    _pendingEndSeqPos++;
                    if (_pendingEndSeqPos == PasteEndSeq.Length)
                    {
                        TerminaTrace.Input.Debug(this, "ESP: Paste end detected, {0} chars", _pasteBuffer.Length);
                        results.Add(new PasteEvent(_pasteBuffer.ToString()));
                        _pasteBuffer.Clear();
                        _pendingEndSeqPos = 0;
                        _state = State.Normal;
                    }
                }
                else if (_pendingEndSeqPos > 0)
                {
                    // Partial end-sequence match failed — the buffered chars belong to paste content
                    _pasteBuffer.Append(PasteEndSeq[.._pendingEndSeqPos]);
                    _pendingEndSeqPos = 0;
                    _pasteBuffer.Append(key.KeyChar);
                }
                else
                {
                    _pasteBuffer.Append(key.KeyChar);
                }
                break;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the accumulated bracket sequence could still lead to a recognized
    /// escape sequence. Used to decide whether to keep buffering or flush the sequence as raw keys.
    /// </summary>
    private static bool CouldLeadToRecognizedSequence(string seq)
    {
        // Could still be paste start "[200~"
        const string pasteStart = "[200~";
        if (seq.Length <= pasteStart.Length && pasteStart.StartsWith(seq, StringComparison.Ordinal))
            return true;

        // Could be an SGR mouse event "[<button;x;yM" — open-ended length up to ~30 chars
        if (seq.Length >= 2 && seq[1] == '<' && seq.Length <= 30)
            return true;

        return false;
    }

    /// <summary>
    /// Parses an SGR mouse sequence and appends a <see cref="MouseScrollEvent"/> to <paramref name="results"/>
    /// when the button code indicates a scroll event (64 = up, 65 = down).
    /// Other mouse events (clicks, releases) are silently consumed — no event is appended.
    /// </summary>
    /// <param name="seq">The buffered sequence string, e.g. <c>[&lt;64;5;10M</c>.</param>
    /// <param name="results">List to append events into.</param>
    private static void EmitMouseSgrEvent(string seq, List<IInputEvent> results)
    {
        // seq = "[<button;x;yM" or "[<button;x;ym"
        // Strip leading "[<" and trailing terminator char
        if (seq.Length < 3) return;
        var inner = seq[2..^1]; // "button;x;y"
        var semicolon = inner.IndexOf(';');
        if (semicolon < 0) return;
        if (!int.TryParse(inner[..semicolon], out var button)) return;

        // SGR button 64 = wheel up, 65 = wheel down
        if (button == 64)
            results.Add(new MouseScrollEvent(+1));
        else if (button == 65)
            results.Add(new MouseScrollEvent(-1));
        // All other buttons (clicks, releases, drags) are silently consumed
    }
}
