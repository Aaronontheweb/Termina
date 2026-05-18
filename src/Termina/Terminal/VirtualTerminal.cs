// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Termina.Terminal;

/// <summary>
/// Virtual terminal implementation for testing.
/// Captures all output to an in-memory buffer that can be inspected.
/// </summary>
public sealed class VirtualTerminal : IAnsiTerminal
{
    private readonly char[,] _buffer;
    private readonly Color[,] _foreground;
    private readonly Color[,] _background;
    private readonly List<string> _rawOutput = new();

    private int _cursorX;
    private int _cursorY;
    private int _savedCursorX;
    private int _savedCursorY;
    private Color _currentForeground = Color.Default;
    private Color _currentBackground = Color.Default;

    /// <summary>
    /// Create a virtual terminal with the specified dimensions.
    /// </summary>
    public VirtualTerminal(int width = 80, int height = 24)
    {
        Width = width;
        Height = height;
        _buffer = new char[height, width];
        _foreground = new Color[height, width];
        _background = new Color[height, width];
        Clear();
    }

    /// <inheritdoc />
    public int Width { get; private set; }

    /// <inheritdoc />
    public int Height { get; private set; }

    /// <summary>
    /// Current cursor X position (0-indexed).
    /// </summary>
    public int CursorX => _cursorX;

    /// <summary>
    /// Current cursor Y position (0-indexed).
    /// </summary>
    public int CursorY => _cursorY;

    /// <summary>
    /// Whether the cursor is visible.
    /// </summary>
    public bool CursorVisible { get; private set; } = true;

    /// <summary>
    /// Whether the terminal is in alternate screen mode.
    /// </summary>
    public bool InAlternateScreen { get; private set; }

    /// <summary>
    /// Whether mouse tracking is enabled.
    /// </summary>
    public bool MouseEnabled { get; private set; }

    /// <summary>
    /// Whether alternate-scroll (wheel-only) mode is enabled.
    /// </summary>
    public bool WheelScrollEnabled { get; private set; }

    /// <summary>
    /// Raw output strings written to the terminal (for debugging).
    /// </summary>
    public IReadOnlyList<string> RawOutput => _rawOutput;

    /// <inheritdoc />
    public void MoveTo(int x, int y)
    {
        _cursorX = Math.Clamp(x, 0, Width - 1);
        _cursorY = Math.Clamp(y, 0, Height - 1);
    }

    /// <inheritdoc />
    public void Write(string text)
    {
        _rawOutput.Add(text);
        foreach (var c in text)
        {
            WriteChar(c);
        }
    }

    /// <inheritdoc />
    public void Write(char c)
    {
        _rawOutput.Add(c.ToString());
        WriteChar(c);
    }

    private void WriteChar(char c)
    {
        if (c == '\n')
        {
            _cursorX = 0;
            _cursorY++;
            if (_cursorY >= Height)
            {
                _cursorY = Height - 1;
                // Could scroll here if needed
            }
            return;
        }

        if (c == '\r')
        {
            _cursorX = 0;
            return;
        }

        if (_cursorX < Width && _cursorY < Height)
        {
            _buffer[_cursorY, _cursorX] = c;
            _foreground[_cursorY, _cursorX] = _currentForeground;
            _background[_cursorY, _cursorX] = _currentBackground;
            _cursorX++;

            if (_cursorX >= Width)
            {
                _cursorX = 0;
                _cursorY++;
                if (_cursorY >= Height)
                {
                    _cursorY = Height - 1;
                }
            }
        }
    }

    /// <inheritdoc />
    public void SetForeground(Color color)
    {
        _currentForeground = color;
    }

    /// <inheritdoc />
    public void SetBackground(Color color)
    {
        _currentBackground = color;
    }

    /// <inheritdoc />
    public void ResetColors()
    {
        _currentForeground = Color.Default;
        _currentBackground = Color.Default;
    }

    /// <inheritdoc />
    public void SaveCursor()
    {
        _savedCursorX = _cursorX;
        _savedCursorY = _cursorY;
    }

    /// <inheritdoc />
    public void RestoreCursor()
    {
        _cursorX = _savedCursorX;
        _cursorY = _savedCursorY;
    }

    /// <inheritdoc />
    public void SetCursorVisible(bool visible)
    {
        CursorVisible = visible;
    }

    /// <inheritdoc />
    public void ClearRegion(int x, int y, int width, int height)
    {
        for (var row = y; row < y + height && row < Height; row++)
        {
            for (var col = x; col < x + width && col < Width; col++)
            {
                if (row >= 0 && col >= 0)
                {
                    _buffer[row, col] = ' ';
                    _foreground[row, col] = Color.Default;
                    _background[row, col] = Color.Default;
                }
            }
        }
    }

    /// <inheritdoc />
    public void ClearScreen()
    {
        Clear();
        MoveTo(0, 0);
    }

    /// <inheritdoc />
    public void Flush()
    {
        // No-op for virtual terminal - everything is already in buffer
    }

    /// <inheritdoc />
    public void EnterAlternateScreen()
    {
        InAlternateScreen = true;
    }

    /// <inheritdoc />
    public void ExitAlternateScreen()
    {
        InAlternateScreen = false;
    }

    /// <inheritdoc />
    public void EnableMouse()
    {
        MouseEnabled = true;
    }

    /// <inheritdoc />
    public void DisableMouse()
    {
        MouseEnabled = false;
    }

    /// <inheritdoc />
    public void EnableWheelScroll()
    {
        WheelScrollEnabled = true;
    }

    /// <inheritdoc />
    public void DisableWheelScroll()
    {
        WheelScrollEnabled = false;
    }

    /// <inheritdoc />
    public void CopyToClipboard(string text)
    {
        _rawOutput.Add(AnsiCodes.Osc52Clipboard(text));
    }

    /// <summary>
    /// Clear the entire buffer.
    /// </summary>
    public void Clear()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                _buffer[y, x] = ' ';
                _foreground[y, x] = Color.Default;
                _background[y, x] = Color.Default;
            }
        }
    }

    /// <summary>
    /// Resize the terminal (for testing resize events).
    /// </summary>
    public void Resize(int newWidth, int newHeight)
    {
        var newBuffer = new char[newHeight, newWidth];
        var newForeground = new Color[newHeight, newWidth];
        var newBackground = new Color[newHeight, newWidth];

        // Initialize with spaces
        for (var y = 0; y < newHeight; y++)
        {
            for (var x = 0; x < newWidth; x++)
            {
                newBuffer[y, x] = ' ';
                newForeground[y, x] = Color.Default;
                newBackground[y, x] = Color.Default;
            }
        }

        // Copy existing content
        var copyHeight = Math.Min(Height, newHeight);
        var copyWidth = Math.Min(Width, newWidth);
        for (var y = 0; y < copyHeight; y++)
        {
            for (var x = 0; x < copyWidth; x++)
            {
                newBuffer[y, x] = _buffer[y, x];
                newForeground[y, x] = _foreground[y, x];
                newBackground[y, x] = _background[y, x];
            }
        }

        // Update dimensions (can't reassign readonly arrays, so we copy reference)
        Width = newWidth;
        Height = newHeight;

        // Clamp cursor
        _cursorX = Math.Min(_cursorX, newWidth - 1);
        _cursorY = Math.Min(_cursorY, newHeight - 1);
    }

    // Query methods for testing assertions

    /// <summary>
    /// Get the character at the specified position.
    /// </summary>
    public char GetChar(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return ' ';
        return _buffer[y, x];
    }

    /// <summary>
    /// Get the foreground color at the specified position.
    /// </summary>
    public Color GetForeground(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return Color.Default;
        return _foreground[y, x];
    }

    /// <summary>
    /// Get the background color at the specified position.
    /// </summary>
    public Color GetBackground(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return Color.Default;
        return _background[y, x];
    }

    /// <summary>
    /// Get a single line from the buffer.
    /// </summary>
    public string GetLine(int y)
    {
        if (y < 0 || y >= Height)
            return string.Empty;

        var sb = new StringBuilder(Width);
        for (var x = 0; x < Width; x++)
        {
            sb.Append(_buffer[y, x]);
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Get text from a rectangular region.
    /// </summary>
    public string GetRegion(int x, int y, int width, int height)
    {
        var sb = new StringBuilder();
        for (var row = y; row < y + height && row < Height; row++)
        {
            if (row > y) sb.AppendLine();
            for (var col = x; col < x + width && col < Width; col++)
            {
                if (row >= 0 && col >= 0)
                {
                    sb.Append(_buffer[row, col]);
                }
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Get all lines as a string array.
    /// </summary>
    public string[] GetAllLines()
    {
        var lines = new string[Height];
        for (var y = 0; y < Height; y++)
        {
            lines[y] = GetLine(y);
        }
        return lines;
    }

    /// <summary>
    /// Check if the buffer contains the specified text anywhere.
    /// </summary>
    public bool Contains(string text)
    {
        var fullText = string.Join("\n", GetAllLines());
        return fullText.Contains(text);
    }

    /// <summary>
    /// Get a snapshot of the entire screen as a string.
    /// </summary>
    public override string ToString()
    {
        return string.Join("\n", GetAllLines());
    }
}
