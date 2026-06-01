// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Termina.Diagnostics;
using Termina.Platform;
using R3;

namespace Termina.Input;

/// <summary>
/// Input source that uses the platform-specific console implementation.
/// </summary>
/// <remarks>
/// <para>
/// This input source wraps <see cref="IPlatformConsole"/> to provide event-driven input
/// instead of polling. On Windows, this uses native console APIs for true event-driven
/// input with no polling delay.
/// </para>
/// <para>
/// Escape sequences (mouse events, bracketed paste) are transparently decoded by
/// <see cref="TerminalInputPipeline"/> before being emitted as application events.
/// This prevents terminal mouse click sequences from appearing as spurious ESC keypresses.
/// </para>
/// </remarks>
public sealed class PlatformInputSource : IInputSource
{
    private readonly IPlatformConsole _console;
    private readonly TerminalInputPipeline _pipeline;
    private IDisposable? _resizeSubscription;

    /// <summary>
    /// Create a new platform input source.
    /// </summary>
    /// <param name="console">The platform console to use for input.</param>
    /// <param name="configuration">Transport-level parser configuration.</param>
    public PlatformInputSource(IPlatformConsole console, PlatformInputConfiguration configuration)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _pipeline = new TerminalInputPipeline(
            new TerminalModeContext(configuration.KittyReportAllKeysVisible));
    }

    /// <inheritdoc />
    public async Task RunAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
        TerminaTrace.Input.Debug(this, "PlatformInputSource.RunAsync starting");

        // Subscribe to resize events from the platform console
        _resizeSubscription = _console.Resized.Subscribe(evt =>
        {
            writer.TryWrite(new ResizeEvent(evt.Width, evt.Height));
        });

        try
        {
            TerminaTrace.Input.Debug(this, "PlatformInputSource entering input loop");

            while (!cancellationToken.IsCancellationRequested)
            {
                IConsoleInputEvent? inputEvent;

                // When buffering an incomplete escape sequence, use a short timeout so a
                // standalone ESC key (e.g., user pressing Esc) can be flushed after 50 ms.
                if (_pipeline.IsBufferingEscape)
                {
                    inputEvent = await ReadInputWithTimeoutAsync(50, cancellationToken);
                    if (inputEvent is null)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        // Timed out — check if the buffered ESC should be flushed
                        var escEvent = _pipeline.CheckEscapeTimeout();
                        if (escEvent is not null)
                            await writer.WriteAsync(escEvent, cancellationToken);
                        continue;
                    }
                }
                else
                {
                    inputEvent = await _console.ReadInputAsync(cancellationToken);
                    if (inputEvent is null)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        continue;
                    }
                }

                TerminaTrace.Input.Debug(this, "Received input event: {0}", inputEvent.GetType().Name);

                switch (inputEvent)
                {
                    case ConsoleKeyEvent keyEvent:
                        TerminaTrace.Input.Trace(this, "KeyEvent: {0}", keyEvent.KeyInfo.Key);
                        var events = _pipeline.Process(keyEvent.KeyInfo);
                        foreach (var e in events)
                        {
                            if (e is PasteEvent pe)
                                TerminaTrace.Input.Debug(this, "PasteEvent emitted: {0} chars", pe.Content.Length);
                            await writer.WriteAsync(e, cancellationToken);
                        }
                        break;

                    case ConsoleResizeEvent resizeEvent:
                        // Resize events are also returned from ReadInputAsync on Windows
                        TerminaTrace.Input.Debug(this, "ResizeEvent: {0}x{1}", resizeEvent.Width, resizeEvent.Height);
                        await writer.WriteAsync(new ResizeEvent(resizeEvent.Width, resizeEvent.Height), cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConsoleMouseEvent:
                        // Silently consume raw platform mouse events (Windows).
                        // On Unix, mouse events arrive as SGR escape sequences handled by the input pipeline.
                        break;
                }
            }

            TerminaTrace.Input.Debug(this, "PlatformInputSource loop exited (cancellation requested)");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TerminaTrace.Input.Error(this, "PlatformInputSource exception: {0}", ex.Message);
            throw;
        }
        finally
        {
            TerminaTrace.Input.Debug(this, "PlatformInputSource cleaning up");
            _resizeSubscription?.Dispose();
            _resizeSubscription = null;
        }
    }

    /// <summary>
    /// Reads the next input event, returning <c>null</c> if <paramref name="timeoutMs"/> elapses
    /// before any input arrives. Used to detect standalone ESC keys that are not followed
    /// by a CSI sequence.
    /// </summary>
    private async ValueTask<IConsoleInputEvent?> ReadInputWithTimeoutAsync(int timeoutMs, CancellationToken outerCt)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
        timeoutCts.CancelAfter(timeoutMs);
        try
        {
            return await _console.ReadInputAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!outerCt.IsCancellationRequested)
        {
            // Timed out (not cancelled by the outer token)
            return null;
        }
    }
}
