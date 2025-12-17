// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Runtime.Versioning;

namespace Termina.Platform;

/// <summary>
/// Unix/macOS console implementation using termios and POSIX signals.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides:
/// </para>
/// <list type="bullet">
/// <item><description>Raw mode via termios (no line buffering or echo)</description></item>
/// <item><description>Event-driven input via poll() on stdin</description></item>
/// <item><description>Window resize events via SIGWINCH signal</description></item>
/// </list>
/// <para>
/// ANSI escape sequences are parsed to extract special keys (arrows, function keys, etc.).
/// </para>
/// </remarks>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class UnixConsole : IPlatformConsole
{
    private readonly Subject<ConsoleResizeEvent> _resized = new();
    private bool _disposed;

    // TODO: P/Invoke declarations will be added in issue #80
    // - termios struct and tcgetattr/tcsetattr
    // - poll/pollfd for event-driven input
    // - sigaction for SIGWINCH handling

    /// <inheritdoc />
    public bool SupportsEventDrivenInput => true;

    /// <inheritdoc />
    public IObservable<ConsoleResizeEvent> Resized => _resized;

    /// <inheritdoc />
    public void Initialize()
    {
        // TODO: Implement in issue #80
        // - Save current termios state
        // - Enter raw mode (disable ICANON, ECHO, ISIG)
        // - Set VMIN=0, VTIME=0 for non-blocking reads
        // - Register SIGWINCH handler
        throw new NotImplementedException("Unix console implementation pending - see issue #80");
    }

    /// <inheritdoc />
    public void Restore()
    {
        // TODO: Implement in issue #80
        // - Restore saved termios state
        // - Unregister SIGWINCH handler
    }

    /// <inheritdoc />
    public ValueTask<IConsoleInputEvent?> ReadInputAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement in issue #80
        // - poll() on stdin with timeout for cancellation support
        // - read() stdin bytes
        // - Parse ANSI escape sequences for special keys
        throw new NotImplementedException("Unix console implementation pending - see issue #80");
    }

    /// <inheritdoc />
    public (int Width, int Height) GetSize()
    {
        // TODO: Implement in issue #80
        // - ioctl with TIOCGWINSZ
        return (Console.WindowWidth, Console.WindowHeight);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            Restore();
        }
        catch
        {
            // Ignore errors during cleanup
        }

        _resized.OnCompleted();
        _resized.Dispose();
    }
}
