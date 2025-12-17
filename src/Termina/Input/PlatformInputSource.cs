// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Termina.Diagnostics;
using Termina.Platform;

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
/// This replaces <see cref="ConsoleInputSource"/> which used a 10ms polling loop.
/// </para>
/// </remarks>
public sealed class PlatformInputSource : IInputSource
{
    private readonly IPlatformConsole _console;
    private IDisposable? _resizeSubscription;

    /// <summary>
    /// Create a new platform input source.
    /// </summary>
    /// <param name="console">The platform console to use for input.</param>
    public PlatformInputSource(IPlatformConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    public async Task RunAsync(ChannelWriter<object> writer, CancellationToken cancellationToken)
    {
        TerminaTrace.Input.Debug(this, "PlatformInputSource.RunAsync starting");

        // Subscribe to resize events from the platform console
        // These come through the observable for signal-based notifications (Unix)
        // or through ReadInputAsync for Windows (WINDOW_BUFFER_SIZE_EVENT)
        _resizeSubscription = _console.Resized.Subscribe(evt =>
        {
            // Convert platform resize event to application resize event
            writer.TryWrite(new ResizeEvent(evt.Width, evt.Height));
        });

        try
        {
            TerminaTrace.Input.Debug(this, "PlatformInputSource entering input loop");

            while (!cancellationToken.IsCancellationRequested)
            {
                var inputEvent = await _console.ReadInputAsync(cancellationToken).ConfigureAwait(false);

                if (inputEvent is null)
                {
                    // Cancelled or no event
                    continue;
                }

                TerminaTrace.Input.Debug(this, "Received input event: {0}", inputEvent.GetType().Name);

                // Convert platform events to application events
                switch (inputEvent)
                {
                    case ConsoleKeyEvent keyEvent:
                        TerminaTrace.Input.Trace(this, "KeyEvent: {0}", keyEvent.KeyInfo.Key);
                        await writer.WriteAsync(new KeyPressed(keyEvent.KeyInfo), cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConsoleResizeEvent resizeEvent:
                        // Resize events are also returned from ReadInputAsync on Windows
                        // We emit them here too in case the observable subscription missed them
                        TerminaTrace.Input.Debug(this, "ResizeEvent: {0}x{1}", resizeEvent.Width, resizeEvent.Height);
                        await writer.WriteAsync(new ResizeEvent(resizeEvent.Width, resizeEvent.Height), cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConsoleMouseEvent mouseEvent:
                        // TODO: Convert to MouseEvent when mouse support is added
                        break;
                }
            }

            TerminaTrace.Input.Debug(this, "PlatformInputSource loop exited (cancellation requested)");
        }
        catch (Exception ex)
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
}
