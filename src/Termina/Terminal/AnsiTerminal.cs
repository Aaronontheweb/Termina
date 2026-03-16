// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Termina.Diagnostics;

namespace Termina.Terminal;

/// <summary>
/// Real terminal implementation using ANSI escape sequences.
/// Writes to standard output.
/// </summary>
public sealed class AnsiTerminal : IAnsiTerminal, IDisposable
{
    private readonly TextWriter _output;
    private readonly StringBuilder _buffer = new();
    private readonly bool _useAlternateScreen;
    private bool _inAlternateScreen;
    private bool _mouseEnabled;
    private long _totalBytesWritten;
    private int _flushCount;

    /// <summary>
    /// Create an AnsiTerminal writing to standard output.
    /// </summary>
    /// <param name="useAlternateScreen">Whether to use alternate screen buffer on startup.</param>
    public AnsiTerminal(bool useAlternateScreen = true)
        : this(Console.Out, useAlternateScreen)
    {
    }

    /// <summary>
    /// Create an AnsiTerminal writing to a specific TextWriter.
    /// </summary>
    internal AnsiTerminal(TextWriter output, bool useAlternateScreen = true)
    {
        _output = output;
        _useAlternateScreen = useAlternateScreen;

        TerminaTrace.Platform.Debug(this, "AnsiTerminal created: output={0}, useAlternateScreen={1}",
            output.GetType().Name, useAlternateScreen);

        // Set console to UTF-8
        Console.OutputEncoding = Encoding.UTF8;

        if (_useAlternateScreen)
        {
            EnterAlternateScreen();
        }

        TerminaTrace.Platform.Debug(this, "AnsiTerminal initialization complete");
    }

    /// <inheritdoc />
    public int Width => GetConsoleWidth();

    /// <inheritdoc />
    public int Height => GetConsoleHeight();

    /// <summary>
    /// Gets the console width, with fallback for non-TTY environments.
    /// </summary>
    private static int GetConsoleWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            // No TTY available (e.g., CI environment, redirected output)
            return 80;
        }
    }

    /// <summary>
    /// Gets the console height, with fallback for non-TTY environments.
    /// </summary>
    private static int GetConsoleHeight()
    {
        try
        {
            return Console.WindowHeight;
        }
        catch (IOException)
        {
            // No TTY available (e.g., CI environment, redirected output)
            return 24;
        }
    }

    /// <inheritdoc />
    public void MoveTo(int x, int y)
    {
        _buffer.Append(AnsiCodes.MoveTo(y, x));
    }

    /// <inheritdoc />
    public void Write(string text)
    {
        _buffer.Append(text);
    }

    /// <inheritdoc />
    public void Write(char c)
    {
        _buffer.Append(c);
    }

    /// <inheritdoc />
    public void SetForeground(Color color)
    {
        _buffer.Append(color.ToForegroundAnsi());
    }

    /// <inheritdoc />
    public void SetBackground(Color color)
    {
        _buffer.Append(color.ToBackgroundAnsi());
    }

    /// <inheritdoc />
    public void ResetColors()
    {
        _buffer.Append(AnsiCodes.Reset);
    }

    /// <inheritdoc />
    public void SaveCursor()
    {
        _buffer.Append(AnsiCodes.SaveCursor);
    }

    /// <inheritdoc />
    public void RestoreCursor()
    {
        _buffer.Append(AnsiCodes.RestoreCursor);
    }

    /// <inheritdoc />
    public void SetCursorVisible(bool visible)
    {
        _buffer.Append(visible ? AnsiCodes.ShowCursor : AnsiCodes.HideCursor);
    }

    /// <inheritdoc />
    public void ClearRegion(int x, int y, int width, int height)
    {
        var spaces = new string(' ', width);
        for (var row = 0; row < height; row++)
        {
            MoveTo(x, y + row);
            Write(spaces);
        }
    }

    /// <inheritdoc />
    public void ClearScreen()
    {
        _buffer.Append(AnsiCodes.ClearScreen);
        MoveTo(0, 0);
    }

    /// <inheritdoc />
    public void Flush()
    {
        if (_buffer.Length > 0)
        {
            var content = _buffer.ToString();
            var byteCount = Encoding.UTF8.GetByteCount(content);

            _flushCount++;
            _totalBytesWritten += byteCount;

            // Log flush details - truncate content preview for readability
            var preview = content.Length > 100
                ? content.Substring(0, 100).Replace("\x1b", "\\e") + "..."
                : content.Replace("\x1b", "\\e");

            TerminaTrace.Render.Debug(this, "Flush #{0}: {1} chars, {2} bytes",
                _flushCount, content.Length, byteCount);
            TerminaTrace.Render.Debug(this, "Content preview: {0}", preview);

            _output.Write(content);
            _output.Flush();
            _buffer.Clear();

            TerminaTrace.Render.Debug(this, "Flush #{0} complete", _flushCount);
        }
    }

    /// <inheritdoc />
    public void EnterAlternateScreen()
    {
        if (!_inAlternateScreen)
        {
            TerminaTrace.Platform.Debug(this, "Entering alternate screen buffer");
            _buffer.Append(AnsiCodes.EnterAlternateScreen);
            _inAlternateScreen = true;
        }
    }

    /// <inheritdoc />
    public void ExitAlternateScreen()
    {
        if (_inAlternateScreen)
        {
            TerminaTrace.Platform.Debug(this, "Exiting alternate screen buffer");
            _buffer.Append(AnsiCodes.ExitAlternateScreen);
            _inAlternateScreen = false;
        }
    }

    /// <inheritdoc />
    public void EnableMouse()
    {
        if (!_mouseEnabled)
        {
            _buffer.Append(AnsiCodes.EnableMouseNormal);
            _buffer.Append(AnsiCodes.EnableMouseSgr);
            _mouseEnabled = true;
        }
    }

    /// <inheritdoc />
    public void DisableMouse()
    {
        if (_mouseEnabled)
        {
            _buffer.Append(AnsiCodes.DisableMouseSgr);
            _buffer.Append(AnsiCodes.DisableMouseNormal);
            _mouseEnabled = false;
        }
    }

    /// <inheritdoc />
    public void CopyToClipboard(string text)
    {
        var sequence = AnsiCodes.Osc52Clipboard(text);
        if (Environment.GetEnvironmentVariable("TMUX") is not null)
        {
            sequence = AnsiCodes.TmuxPassthrough(sequence);
        }

        _buffer.Append(sequence);
    }

    /// <summary>
    /// Dispose the terminal, restoring original state.
    /// </summary>
    public void Dispose()
    {
        if (_mouseEnabled)
        {
            DisableMouse();
        }

        if (_inAlternateScreen)
        {
            ExitAlternateScreen();
        }

        SetCursorVisible(true);
        ResetColors();
        Flush();
    }
}
