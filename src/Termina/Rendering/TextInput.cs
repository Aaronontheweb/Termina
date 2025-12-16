// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Rendering;

/// <summary>
/// A text input component that handles keyboard input and cursor management.
/// This is the v2 implementation using the new rendering infrastructure.
/// </summary>
public sealed class TextInput : IRenderable, IDisposable
{
    private string _text = string.Empty;
    private string _label = string.Empty;
    private string _placeholder = string.Empty;
    private bool _isFocused;
    private int _cursorPosition;

    // Cursor blinking state
    private Timer? _blinkTimer;
    private bool _cursorVisible = true;
    private int _blinkIntervalMs = 530; // Standard cursor blink rate

    /// <summary>
    /// Event raised when Enter is pressed.
    /// </summary>
    public event Action<string>? OnSubmit;

    /// <summary>
    /// Event raised when the component needs to be re-rendered.
    /// </summary>
    public event Action? OnDirty;

    /// <summary>
    /// Gets or sets the label displayed before the input.
    /// </summary>
    public string Label
    {
        get => _label;
        set => _label = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the current text value.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? string.Empty;
            // Keep cursor within valid range
            _cursorPosition = Math.Min(_cursorPosition, _text.Length);
        }
    }

    /// <summary>
    /// Gets or sets the placeholder text shown when the input is empty.
    /// </summary>
    public string Placeholder
    {
        get => _placeholder;
        set => _placeholder = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets whether the input is focused.
    /// </summary>
    public bool IsFocused
    {
        get => _isFocused;
        set
        {
            if (_isFocused == value)
                return;
            _isFocused = value;
            MarkDirty();
        }
    }

    /// <summary>
    /// Gets or sets the cursor position.
    /// </summary>
    public int CursorPosition
    {
        get => _cursorPosition;
        set => _cursorPosition = Math.Max(0, Math.Min(value, _text.Length));
    }

    /// <summary>
    /// Gets or sets the foreground color for the text.
    /// </summary>
    public Color Foreground { get; set; } = Color.Default;

    /// <summary>
    /// Gets or sets the background color for the text.
    /// </summary>
    public Color Background { get; set; } = Color.Default;

    /// <summary>
    /// Gets or sets the foreground color when focused.
    /// </summary>
    public Color FocusedForeground { get; set; } = Color.BrightBlue;

    /// <summary>
    /// Gets or sets the foreground color for the placeholder text.
    /// </summary>
    public Color PlaceholderForeground { get; set; } = Color.BrightBlack; // Gray

    /// <summary>
    /// Gets or sets whether the cursor should blink when focused.
    /// Default is false to avoid complexity in testing.
    /// </summary>
    public bool CursorBlink
    {
        get => _blinkTimer != null;
        set
        {
            if (value == CursorBlink)
                return;

            if (value)
                StartBlinking();
            else
                StopBlinking();
        }
    }

    /// <summary>
    /// Gets or sets the cursor blink interval in milliseconds.
    /// Default is 530ms (standard cursor blink rate).
    /// </summary>
    public int BlinkIntervalMs
    {
        get => _blinkIntervalMs;
        set
        {
            _blinkIntervalMs = Math.Max(100, value); // Minimum 100ms
            if (_blinkTimer != null)
            {
                // Restart timer with new interval
                StopBlinking();
                StartBlinking();
            }
        }
    }

    /// <summary>
    /// Handles a key press. Returns true if the key was handled.
    /// </summary>
    /// <param name="key">The key info from the console.</param>
    /// <returns>True if the key was consumed by this component.</returns>
    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (!_isFocused)
            return false;

        // Reset cursor visibility on any key press (keeps cursor visible while typing)
        ResetCursorBlink();

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                OnSubmit?.Invoke(_text);
                return true;

            case ConsoleKey.Backspace:
                if (_cursorPosition > 0)
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    MarkDirty();
                }
                return true;

            case ConsoleKey.Delete:
                if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    MarkDirty();
                }
                return true;

            case ConsoleKey.LeftArrow:
                if (_cursorPosition > 0)
                {
                    _cursorPosition--;
                    MarkDirty();
                }
                return true;

            case ConsoleKey.RightArrow:
                if (_cursorPosition < _text.Length)
                {
                    _cursorPosition++;
                    MarkDirty();
                }
                return true;

            case ConsoleKey.Home:
                _cursorPosition = 0;
                MarkDirty();
                return true;

            case ConsoleKey.End:
                _cursorPosition = _text.Length;
                MarkDirty();
                return true;

            default:
                // Insert printable character
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _text = _text.Insert(_cursorPosition, key.KeyChar.ToString());
                    _cursorPosition++;
                    MarkDirty();
                    return true;
                }
                return false;
        }
    }

    /// <summary>
    /// Clears the input text and resets cursor.
    /// </summary>
    public void Clear()
    {
        _text = string.Empty;
        _cursorPosition = 0;
        MarkDirty();
    }

    /// <inheritdoc />
    public void Render(IRenderContext context)
    {
        var labelWidth = 0;

        // Render label if present
        if (!string.IsNullOrEmpty(_label))
        {
            context.SetForeground(_isFocused ? FocusedForeground : Foreground);
            context.SetBackground(Background);
            var labelText = _label + ": ";
            context.WriteAt(0, 0, labelText);
            labelWidth = labelText.Length;
        }

        // Calculate available width for the text field
        var availableWidth = context.Width - labelWidth;
        if (availableWidth <= 0)
            return;

        // Determine what to display
        if (string.IsNullOrEmpty(_text))
        {
            // Empty input
            if (!string.IsNullOrEmpty(_placeholder))
            {
                // Show placeholder
                context.SetForeground(PlaceholderForeground);
                context.SetBackground(Background);
                var placeholderText = _placeholder.Length > availableWidth
                    ? _placeholder[..availableWidth]
                    : _placeholder;
                context.WriteAt(labelWidth, 0, placeholderText);
            }
            else if (_isFocused && _cursorVisible)
            {
                // Show cursor position for empty focused input
                context.SetForeground(Background);
                context.SetBackground(Foreground == Color.Default ? Color.White : Foreground);
                context.WriteAt(labelWidth, 0, ' ');
            }
        }
        else
        {
            // Has text
            context.SetForeground(Foreground);
            context.SetBackground(Background);

            if (_isFocused && _cursorVisible)
            {
                // Render text with cursor
                var beforeCursor = _text[.._cursorPosition];
                var atCursor = _cursorPosition < _text.Length ? _text[_cursorPosition] : ' ';
                var afterCursor = _cursorPosition < _text.Length ? _text[(_cursorPosition + 1)..] : "";

                var x = labelWidth;

                // Truncate if needed
                if (beforeCursor.Length + 1 + afterCursor.Length > availableWidth)
                {
                    // Simple truncation - show as much as fits ending at cursor
                    var totalLen = beforeCursor.Length + 1 + afterCursor.Length;
                    if (totalLen > availableWidth)
                    {
                        var showBefore = Math.Min(beforeCursor.Length, availableWidth - 1);
                        beforeCursor = beforeCursor[(beforeCursor.Length - showBefore)..];
                        var remaining = availableWidth - showBefore - 1;
                        afterCursor = afterCursor.Length > remaining ? afterCursor[..remaining] : afterCursor;
                    }
                }

                // Write before cursor
                if (beforeCursor.Length > 0)
                {
                    context.WriteAt(x, 0, beforeCursor);
                    x += beforeCursor.Length;
                }

                // Write cursor (inverted)
                context.SetForeground(Background);
                context.SetBackground(Foreground == Color.Default ? Color.White : Foreground);
                context.WriteAt(x, 0, atCursor);
                x++;

                // Write after cursor
                if (afterCursor.Length > 0)
                {
                    context.SetForeground(Foreground);
                    context.SetBackground(Background);
                    context.WriteAt(x, 0, afterCursor);
                }
            }
            else
            {
                // Just render text without cursor
                var displayText = _text.Length > availableWidth ? _text[..availableWidth] : _text;
                context.WriteAt(labelWidth, 0, displayText);
            }
        }

        context.ResetColors();
    }

    /// <inheritdoc />
    public (int Width, int Height) Measure(int availableWidth, int availableHeight)
    {
        var labelWidth = string.IsNullOrEmpty(_label) ? 0 : _label.Length + 2; // "label: "
        var textWidth = string.IsNullOrEmpty(_text) ? 1 : _text.Length; // At least 1 for cursor
        var totalWidth = labelWidth + textWidth + 1; // +1 for cursor space

        return (Math.Min(totalWidth, availableWidth), Math.Min(1, availableHeight));
    }

    /// <summary>
    /// Marks the component as needing re-render.
    /// </summary>
    private void MarkDirty()
    {
        OnDirty?.Invoke();
    }

    /// <summary>
    /// Starts the cursor blink timer.
    /// </summary>
    private void StartBlinking()
    {
        if (_blinkTimer != null)
            return;

        _cursorVisible = true;
        _blinkTimer = new Timer(OnBlinkTick, null, _blinkIntervalMs, _blinkIntervalMs);
    }

    /// <summary>
    /// Stops the cursor blink timer.
    /// </summary>
    private void StopBlinking()
    {
        _blinkTimer?.Dispose();
        _blinkTimer = null;
        _cursorVisible = true;
    }

    /// <summary>
    /// Resets the cursor blink cycle, making the cursor visible immediately.
    /// Called when the user types to ensure cursor visibility during active input.
    /// </summary>
    private void ResetCursorBlink()
    {
        if (_blinkTimer == null)
            return;

        _cursorVisible = true;
        _blinkTimer.Change(_blinkIntervalMs, _blinkIntervalMs);
    }

    /// <summary>
    /// Timer callback for cursor blinking.
    /// </summary>
    private void OnBlinkTick(object? state)
    {
        _cursorVisible = !_cursorVisible;
        MarkDirty();
    }

    /// <summary>
    /// Disposes the component, stopping any active timers.
    /// </summary>
    public void Dispose()
    {
        StopBlinking();
        GC.SuppressFinalize(this);
    }
}
