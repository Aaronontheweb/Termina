// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

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

        // Set console to UTF-8
        Console.OutputEncoding = Encoding.UTF8;

        if (_useAlternateScreen)
        {
            EnterAlternateScreen();
        }
    }

    /// <inheritdoc />
    public int Width => Console.WindowWidth;

    /// <inheritdoc />
    public int Height => Console.WindowHeight;

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
            _output.Write(_buffer.ToString());
            _output.Flush();
            _buffer.Clear();
        }
    }

    /// <inheritdoc />
    public void EnterAlternateScreen()
    {
        if (!_inAlternateScreen)
        {
            _buffer.Append(AnsiCodes.EnterAlternateScreen);
            _inAlternateScreen = true;
        }
    }

    /// <inheritdoc />
    public void ExitAlternateScreen()
    {
        if (_inAlternateScreen)
        {
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
