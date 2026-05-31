// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Internal facade for terminal input processing.
/// </summary>
/// <remarks>
/// This currently delegates to <see cref="EscapeSequenceParser"/> to preserve behavior while
/// introducing the seam where protocol-specific decoders can be extracted incrementally.
/// </remarks>
internal sealed class TerminalInputPipeline
{
    private readonly EscapeSequenceParser _parser;

    public TerminalInputPipeline(bool kittyReportAllKeysVisible = false, Func<long>? getTick = null)
    {
        _parser = new EscapeSequenceParser(getTick)
        {
            KittyReportAllKeysVisible = kittyReportAllKeysVisible,
        };
    }

    /// <summary>
    /// Whether the pipeline is currently buffering an incomplete escape sequence.
    /// </summary>
    public bool IsBufferingEscape => _parser.IsBufferingEscape;

    /// <summary>
    /// Process one key-shaped transport event into zero or more current public input events.
    /// </summary>
    public IReadOnlyList<IInputEvent> Process(ConsoleKeyInfo key) => _parser.Process(key);

    /// <summary>
    /// Flush a standalone Escape key if the escape timeout has elapsed.
    /// </summary>
    public KeyPressed? CheckEscapeTimeout() => _parser.CheckEscapeTimeout();
}
