using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Components;

/// <summary>
/// A text input component that handles its own keyboard input and cursor management.
/// Supports an optional blinking cursor when focused.
/// </summary>
public class TextInput : Component, IDisposable
{
    private string _text = string.Empty;
    private string _label = string.Empty;
    private string _placeholder = string.Empty;
    private bool _isFocused;
    private int _cursorPosition;
    private Color _focusedColor = Color.Blue;
    private Color _unfocusedColor = Color.Grey;

    // Cursor blinking state
    private Timer? _blinkTimer;
    private bool _cursorVisible = true;
    private int _blinkIntervalMs = 530; // Standard cursor blink rate

    /// <summary>
    /// Event raised when Enter is pressed.
    /// </summary>
    public event Action<string>? OnSubmit;

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
        set => _isFocused = value;
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
    /// Gets or sets the color when focused.
    /// </summary>
    public Color FocusedColor
    {
        get => _focusedColor;
        set => _focusedColor = value;
    }

    /// <summary>
    /// Gets or sets the color when unfocused.
    /// </summary>
    public Color UnfocusedColor
    {
        get => _unfocusedColor;
        set => _unfocusedColor = value;
    }

    /// <summary>
    /// Gets or sets whether the cursor should blink when focused.
    /// Default is true. When enabled, the cursor blinks at the standard rate.
    /// </summary>
    public bool CursorBlink
    {
        get => _blinkTimer != null;
        set
        {
            if (value == CursorBlink)
                return;

            if (value)
            {
                StartBlinking();
            }
            else
            {
                StopBlinking();
            }
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
                }
                return true;

            case ConsoleKey.Delete:
                if (_cursorPosition < _text.Length)
                {
                    _text = _text.Remove(_cursorPosition, 1);
                }
                return true;

            case ConsoleKey.LeftArrow:
                if (_cursorPosition > 0)
                    _cursorPosition--;
                return true;

            case ConsoleKey.RightArrow:
                if (_cursorPosition < _text.Length)
                    _cursorPosition++;
                return true;

            case ConsoleKey.Home:
                _cursorPosition = 0;
                return true;

            case ConsoleKey.End:
                _cursorPosition = _text.Length;
                return true;

            default:
                // Insert printable character
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _text = _text.Insert(_cursorPosition, key.KeyChar.ToString());
                    _cursorPosition++;
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
    }

    /// <summary>
    /// Renders the text input.
    /// </summary>
    public override IRenderable Render()
    {
        var color = _isFocused ? _focusedColor : _unfocusedColor;

        string displayText;
        if (string.IsNullOrEmpty(_text))
        {
            // Empty input - show placeholder or blinking cursor
            if (_isFocused && _cursorVisible)
            {
                displayText = string.IsNullOrEmpty(_placeholder)
                    ? "[invert] [/]"
                    : $"[invert]{Markup.Escape(_placeholder[..1])}[/][dim]{Markup.Escape(_placeholder[1..])}[/]";
            }
            else
            {
                displayText = string.IsNullOrEmpty(_placeholder)
                    ? " "
                    : $"[dim]{Markup.Escape(_placeholder)}[/]";
            }
        }
        else if (_isFocused)
        {
            // Show cursor position (blinking or solid based on _cursorVisible)
            var beforeCursor = _text[.._cursorPosition];
            var atCursor = _cursorPosition < _text.Length ? _text[_cursorPosition].ToString() : " ";
            var afterCursor = _cursorPosition < _text.Length ? _text[(_cursorPosition + 1)..] : "";

            if (_cursorVisible)
            {
                displayText = $"{Markup.Escape(beforeCursor)}[invert]{Markup.Escape(atCursor)}[/]{Markup.Escape(afterCursor)}";
            }
            else
            {
                // Cursor hidden during blink - show text without invert
                displayText = Markup.Escape(_text) + (_cursorPosition >= _text.Length ? " " : "");
            }
        }
        else
        {
            displayText = Markup.Escape(_text);
        }

        var labelPart = string.IsNullOrEmpty(_label)
            ? ""
            : $"[{color}]{Markup.Escape(_label)}:[/] ";

        return new Markup($"{labelPart}{displayText}");
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
