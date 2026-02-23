using System.Text;
using System.Threading.Channels;

namespace Termina.Input;

/// <summary>
/// Real console input source that reads from Console.ReadKey and detects terminal resize.
/// Runs on a background thread since ReadKey is blocking.
/// Also handles bracketed paste mode sequences and SGR mouse scroll events.
/// </summary>
public sealed class ConsoleInputSource : IInputSource
{
    private int _lastWidth;
    private int _lastHeight;

    // Paste / escape-sequence detection state machine
    private enum PasteDetectionState { Normal, AfterEscape, InBracketSequence, PasteBuffering }
    private PasteDetectionState _pasteState = PasteDetectionState.Normal;
    private readonly StringBuilder _seqBuffer = new();
    private readonly StringBuilder _pasteBuffer = new();
    private long _escapeReceivedAt;   // Environment.TickCount64 when ESC was seen
    private int _pendingEndSeqPos;    // how many chars of _pasteEndSeq we've matched so far

    // The bracketed-paste end sentinel: ESC [ 2 0 1 ~
    private const string PasteEndSeq = "\x1b[201~";

    /// <inheritdoc />
    public async Task RunAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
        // Initialize dimensions for resize detection
        InitializeDimensions();

        // Run blocking Console.ReadKey on thread pool
        await Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Flush a lone ESC if 50 ms have passed with no follow-up character
                if (_pasteState == PasteDetectionState.AfterEscape &&
                    Environment.TickCount64 - _escapeReceivedAt > 50)
                {
                    await writer.WriteAsync(
                        new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)),
                        cancellationToken);
                    _pasteState = PasteDetectionState.Normal;
                }

                // Check if key available to avoid blocking forever on cancellation
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    await ProcessKeyAsync(key, writer, cancellationToken);
                }
                else
                {
                    // Brief yield to check cancellation
                    await Task.Delay(10, cancellationToken);
                }

                // Check for terminal resize (polling)
                await CheckForResizeAsync(writer, cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task ProcessKeyAsync(ConsoleKeyInfo key, ChannelWriter<object> writer, CancellationToken ct)
    {
        switch (_pasteState)
        {
            case PasteDetectionState.Normal:
                if (key.Key == ConsoleKey.Escape)
                {
                    _escapeReceivedAt = Environment.TickCount64;
                    _pasteState = PasteDetectionState.AfterEscape;
                }
                else
                {
                    await writer.WriteAsync(new KeyPressed(key), ct);
                }
                break;

            case PasteDetectionState.AfterEscape:
                if (key.KeyChar == '[')
                {
                    _seqBuffer.Clear();
                    _seqBuffer.Append('[');
                    _pasteState = PasteDetectionState.InBracketSequence;
                }
                else
                {
                    // Not a CSI sequence — flush ESC and re-process the current key
                    await writer.WriteAsync(
                        new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)),
                        ct);
                    _pasteState = PasteDetectionState.Normal;
                    await ProcessKeyAsync(key, writer, ct);
                }
                break;

            case PasteDetectionState.InBracketSequence:
                _seqBuffer.Append(key.KeyChar);
                var seq = _seqBuffer.ToString();

                // Detected bracketed paste start: ESC[200~
                if (seq == "[200~")
                {
                    _pasteBuffer.Clear();
                    _pendingEndSeqPos = 0;
                    _pasteState = PasteDetectionState.PasteBuffering;
                    break;
                }

                // Detected SGR mouse event: ESC[<button;x;yM or ESC[<button;x;ym
                if (seq.Length >= 2 && seq[1] == '<' && (key.KeyChar == 'M' || key.KeyChar == 'm'))
                {
                    EmitMouseSgrEvent(seq, writer);
                    _seqBuffer.Clear();
                    _pasteState = PasteDetectionState.Normal;
                    break;
                }

                // Check whether the accumulated sequence could still match a known pattern
                if (!CouldLeadToRecognizedSequence(seq))
                {
                    // Unrecognized — flush ESC and the accumulated sequence characters as key presses
                    await writer.WriteAsync(
                        new KeyPressed(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)),
                        ct);
                    foreach (var c in seq)
                    {
                        await writer.WriteAsync(
                            new KeyPressed(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false)),
                            ct);
                    }
                    _seqBuffer.Clear();
                    _pasteState = PasteDetectionState.Normal;
                }
                break;

            case PasteDetectionState.PasteBuffering:
                // Detect the end sentinel ESC[201~ one character at a time
                var isEscChar = key.Key == ConsoleKey.Escape || key.KeyChar == '\x1b';
                var expectedChar = _pendingEndSeqPos == 0 ? '\x1b' : PasteEndSeq[_pendingEndSeqPos];
                var charMatches = _pendingEndSeqPos == 0 ? isEscChar : key.KeyChar == expectedChar;

                if (charMatches)
                {
                    _pendingEndSeqPos++;
                    if (_pendingEndSeqPos == PasteEndSeq.Length)
                    {
                        // Paste complete
                        await writer.WriteAsync(new PasteEvent(_pasteBuffer.ToString()), ct);
                        _pasteBuffer.Clear();
                        _pendingEndSeqPos = 0;
                        _pasteState = PasteDetectionState.Normal;
                    }
                }
                else if (_pendingEndSeqPos > 0)
                {
                    // Partial end-sequence match failed — those chars belong to the paste content
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
    /// Returns true if the accumulated bracket sequence could still lead to a recognized escape sequence.
    /// </summary>
    private static bool CouldLeadToRecognizedSequence(string seq)
    {
        // Could be the paste start "[200~"
        const string pasteStart = "[200~";
        if (seq.Length <= pasteStart.Length && pasteStart.StartsWith(seq, StringComparison.Ordinal))
            return true;

        // Could be an SGR mouse event starting with "[<"
        if (seq.Length >= 2 && seq[1] == '<' && seq.Length <= 20)
            return true;

        return false;
    }

    /// <summary>
    /// Parses an SGR mouse scroll sequence and emits a <see cref="MouseScrollEvent"/> if applicable.
    /// </summary>
    private static void EmitMouseSgrEvent(string seq, ChannelWriter<object> writer)
    {
        // seq is "[<button;x;yM" or "[<button;x;ym"
        // strip the leading "[<" and trailing 'M'/'m'
        if (seq.Length < 3) return;
        var inner = seq[2..^1]; // "button;x;y"
        var semicolon = inner.IndexOf(';');
        if (semicolon < 0) return;
        if (!int.TryParse(inner[..semicolon], out var button)) return;

        // SGR button 64 = scroll up, 65 = scroll down
        if (button == 64)
            writer.TryWrite(new MouseScrollEvent(+1));
        else if (button == 65)
            writer.TryWrite(new MouseScrollEvent(-1));
        // All other mouse events are silently consumed
    }

    private void InitializeDimensions()
    {
        try
        {
            _lastWidth = Console.WindowWidth;
            _lastHeight = Console.WindowHeight;
        }
        catch (IOException)
        {
            // No TTY available - use defaults
            _lastWidth = 80;
            _lastHeight = 24;
        }
    }

    private async Task CheckForResizeAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
        try
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            if (width != _lastWidth || height != _lastHeight)
            {
                _lastWidth = width;
                _lastHeight = height;
                await writer.WriteAsync(new ResizeEvent(width, height), cancellationToken);
            }
        }
        catch (IOException)
        {
            // No TTY available - ignore resize checks
        }
    }
}
