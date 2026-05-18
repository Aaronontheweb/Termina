// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Terminal;

/// <summary>
/// Abstraction over terminal output for ANSI escape sequence rendering.
/// Implementations can target real consoles or virtual buffers for testing.
/// </summary>
public interface IAnsiTerminal
{
    /// <summary>
    /// Terminal width in columns.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Terminal height in rows.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Move cursor to the specified position (0-indexed).
    /// </summary>
    void MoveTo(int x, int y);

    /// <summary>
    /// Write text at the current cursor position.
    /// </summary>
    void Write(string text);

    /// <summary>
    /// Write a single character at the current cursor position.
    /// </summary>
    void Write(char c);

    /// <summary>
    /// Set the foreground color for subsequent writes.
    /// </summary>
    void SetForeground(Color color);

    /// <summary>
    /// Set the background color for subsequent writes.
    /// </summary>
    void SetBackground(Color color);

    /// <summary>
    /// Reset colors to terminal defaults.
    /// </summary>
    void ResetColors();

    /// <summary>
    /// Save the current cursor position.
    /// </summary>
    void SaveCursor();

    /// <summary>
    /// Restore the previously saved cursor position.
    /// </summary>
    void RestoreCursor();

    /// <summary>
    /// Set cursor visibility.
    /// </summary>
    void SetCursorVisible(bool visible);

    /// <summary>
    /// Clear a rectangular region of the screen, filling with spaces.
    /// </summary>
    void ClearRegion(int x, int y, int width, int height);

    /// <summary>
    /// Clear the entire screen.
    /// </summary>
    void ClearScreen();

    /// <summary>
    /// Flush any buffered output to the terminal.
    /// </summary>
    void Flush();

    /// <summary>
    /// Enter alternate screen buffer (preserves main buffer for restoration).
    /// </summary>
    void EnterAlternateScreen();

    /// <summary>
    /// Exit alternate screen buffer (restores main buffer).
    /// </summary>
    void ExitAlternateScreen();

    /// <summary>
    /// Enable mouse tracking. Captures clicks, releases, drags, and the wheel as application
    /// input via SGR escape sequences (CSI ?1000h + CSI ?1006h). Side effect: the host terminal
    /// stops handling native click-drag text selection while this mode is active. Most apps
    /// should prefer <see cref="EnableWheelScroll"/>, which only takes the wheel and leaves
    /// selection to the terminal.
    /// </summary>
    void EnableMouse();

    /// <summary>
    /// Disable mouse tracking.
    /// </summary>
    void DisableMouse();

    /// <summary>
    /// Enable wheel-only scrolling via the terminal's alternate-scroll mode (CSI ?1007h).
    /// While in the alternate screen buffer, the terminal translates mouse-wheel events into
    /// cursor up/down key sequences instead of mouse events. The application gets scrolling
    /// without the side effects of full mouse tracking, so native click-drag selection,
    /// triple-click word selection, and OS clipboard integration continue to work.
    /// </summary>
    void EnableWheelScroll();

    /// <summary>
    /// Disable wheel-only scrolling (CSI ?1007l).
    /// </summary>
    void DisableWheelScroll();

    /// <summary>
    /// Request that the terminal copy text to the user's clipboard.
    /// </summary>
    void CopyToClipboard(string text);
}
