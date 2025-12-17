// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Platform;

/// <summary>
/// Platform abstraction for console I/O operations.
/// Provides event-driven input and proper terminal initialization for each platform.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts the differences between console implementations on different platforms:
/// </para>
/// <list type="bullet">
/// <item><description>Windows: P/Invoke for SetConsoleMode, ReadConsoleInputW</description></item>
/// <item><description>Unix/macOS: termios for raw mode, poll() for input, SIGWINCH for resize</description></item>
/// <item><description>Fallback: Polling-based implementation using Console.ReadKey</description></item>
/// </list>
/// <para>
/// The key improvement over direct Console usage is event-driven input without polling delays.
/// </para>
/// </remarks>
public interface IPlatformConsole : IDisposable
{
    /// <summary>
    /// Initialize the console for TUI mode.
    /// </summary>
    /// <remarks>
    /// Platform-specific initialization includes:
    /// <list type="bullet">
    /// <item><description>Windows: Enable VIRTUAL_TERMINAL_PROCESSING and WINDOW_INPUT</description></item>
    /// <item><description>Unix: Enter raw mode via termios, register SIGWINCH handler</description></item>
    /// </list>
    /// </remarks>
    void Initialize();

    /// <summary>
    /// Restore the console to its original state.
    /// </summary>
    /// <remarks>
    /// Should be called before application exit to restore terminal settings.
    /// Called automatically by <see cref="IDisposable.Dispose"/>.
    /// </remarks>
    void Restore();

    /// <summary>
    /// Wait for and return the next input event.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>
    /// The next input event, or null if cancelled or no event available.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method blocks until input is available but supports cancellation.
    /// Unlike polling-based approaches, this uses native OS mechanisms for efficient waiting:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Windows: WaitForSingleObjectEx on console input handle</description></item>
    /// <item><description>Unix: poll() on stdin file descriptor</description></item>
    /// </list>
    /// </remarks>
    ValueTask<IConsoleInputEvent?> ReadInputAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get the current terminal dimensions.
    /// </summary>
    /// <returns>A tuple of (Width, Height) in character cells.</returns>
    (int Width, int Height) GetSize();

    /// <summary>
    /// Observable that emits when the terminal is resized.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On Unix, this is triggered by the SIGWINCH signal for true event-driven resize detection.
    /// On Windows, resize events come through the console input buffer (WINDOW_BUFFER_SIZE_EVENT).
    /// The fallback implementation polls Console.WindowWidth/Height.
    /// </para>
    /// </remarks>
    IObservable<ConsoleResizeEvent> Resized { get; }

    /// <summary>
    /// Gets whether this platform console supports true event-driven input.
    /// </summary>
    /// <remarks>
    /// Returns false for the fallback polling-based implementation.
    /// </remarks>
    bool SupportsEventDrivenInput { get; }
}
