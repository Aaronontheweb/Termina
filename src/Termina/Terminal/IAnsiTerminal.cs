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
    /// Enable mouse tracking.
    /// </summary>
    void EnableMouse();

    /// <summary>
    /// Disable mouse tracking.
    /// </summary>
    void DisableMouse();

    /// <summary>
    /// Request that the terminal copy text to the user's clipboard.
    /// </summary>
    void CopyToClipboard(string text);

    /// <summary>
    /// Send a raw ANSI escape sequence to the terminal.
    /// </summary>
    /// <param name="sequence">The raw escape sequence to send.</param>
    void SendRaw(string sequence);
}
