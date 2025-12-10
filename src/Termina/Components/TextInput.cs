namespace Termina.Components;

/// <summary>
/// Interactive single-line text input component
/// </summary>
public sealed class TextInput : Component
{
    private string _buffer = "";
    private string _placeholder = "";
    private Action<string>? _onSubmit;
    private Action<string>? _onChange;

    public override bool CanFocus => true;

    /// <summary>
    /// Set placeholder text shown when empty
    /// </summary>
    public TextInput Placeholder(string placeholder)
    {
        _placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// Set handler called when Enter is pressed
    /// </summary>
    public TextInput OnSubmit(Action<string> handler)
    {
        _onSubmit = handler;
        return this;
    }

    /// <summary>
    /// Set handler called on every keystroke
    /// </summary>
    public TextInput OnChange(Action<string> handler)
    {
        _onChange = handler;
        return this;
    }

    public override void OnKeyPress(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                _onSubmit?.Invoke(_buffer);
                break;

            case ConsoleKey.Backspace when _buffer.Length > 0:
                _buffer = _buffer[..^1];
                _onChange?.Invoke(_buffer);
                break;

            case ConsoleKey.Escape:
                _buffer = "";
                _onChange?.Invoke(_buffer);
                break;

            default:
                if (!char.IsControl(key.KeyChar))
                {
                    _buffer += key.KeyChar;
                    _onChange?.Invoke(_buffer);
                }
                break;
        }
    }

    public override string[] Render(RenderContext context)
    {
        var isFocused = context.FocusedComponent == this;

        var displayText = string.IsNullOrEmpty(_buffer)
            ? $"\x1b[2m{_placeholder}\x1b[0m"  // Dim placeholder
            : _buffer;

        var cursor = isFocused ? "_" : "";
        var prefix = isFocused ? "\x1b[32m>\x1b[0m " : "> "; // Green > when focused

        var line = $"{prefix}{displayText}{cursor}";

        // Truncate if too wide
        if (line.Length > context.Width)
        {
            line = line.Substring(0, context.Width);
        }

        return new[] { line };
    }

    public override Size MeasureSize(int availableWidth, int availableHeight)
    {
        return new Size(availableWidth, 1);
    }

    /// <summary>
    /// Get current buffer value (for testing)
    /// </summary>
    public string GetValue() => _buffer;

    /// <summary>
    /// Set buffer value (for testing)
    /// </summary>
    public void SetValue(string value)
    {
        _buffer = value;
        _onChange?.Invoke(_buffer);
    }
}
