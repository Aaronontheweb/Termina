// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using R3;

namespace Termina.Platform;

/// <summary>
/// Fallback console implementation using polling-based input.
/// Used on platforms where native event-driven input is not available.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="Console.KeyAvailable"/> and <see cref="Console.ReadKey(bool)"/>
/// with a 1ms polling delay. While not as efficient as native implementations, it works on any
/// .NET-supported platform.
/// </para>
/// <para>
/// Based on the original <see cref="Input.ConsoleInputSource"/> implementation.
/// </para>
/// </remarks>
public sealed class FallbackConsole : IPlatformConsole
{
    private readonly Subject<ConsoleResizeEvent> _resized = new();
    private int _lastWidth;
    private int _lastHeight;
    private bool _disposed;

    /// <inheritdoc />
    public bool SupportsEventDrivenInput => false;

    /// <inheritdoc />
    public Observable<ConsoleResizeEvent> Resized => _resized;

    /// <inheritdoc />
    public void Initialize()
    {
        // Set UTF-8 encoding for proper Unicode support
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Capture initial dimensions
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

    /// <inheritdoc />
    public void Restore()
    {
        // No special cleanup needed for fallback implementation
    }

    /// <inheritdoc />
    public async ValueTask<IConsoleInputEvent?> ReadInputAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Check for keyboard input
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                return new ConsoleKeyEvent(key);
            }

            // Check for resize
            var resizeEvent = CheckForResize();
            if (resizeEvent.HasValue)
            {
                return resizeEvent.Value;
            }

            // Yield to allow cancellation - reduced from 10ms to 1ms
            try
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public (int Width, int Height) GetSize()
    {
        try
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }
        catch (IOException)
        {
            return (_lastWidth, _lastHeight);
        }
    }

    private ConsoleResizeEvent? CheckForResize()
    {
        try
        {
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            if (width != _lastWidth || height != _lastHeight)
            {
                _lastWidth = width;
                _lastHeight = height;

                var evt = new ConsoleResizeEvent(width, height);
                _resized.OnNext(evt);
                return evt;
            }
        }
        catch (IOException)
        {
            // No TTY available - ignore resize checks
        }

        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Restore();
        _resized.OnCompleted();
        _resized.Dispose();
    }
}
