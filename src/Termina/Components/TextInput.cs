using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Components;

/// <summary>
/// A text input component for entering text.
/// State is managed externally by the ViewModel and set via properties.
/// </summary>
public class TextInput : Component
{
    private string _text = string.Empty;
    private string _label = string.Empty;
    private string _placeholder = string.Empty;
    private bool _isFocused;
    private int _cursorPosition;
    private Color _focusedColor = Color.Blue;
    private Color _unfocusedColor = Color.Grey;

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
    /// Renders the text input.
    /// </summary>
    public override IRenderable Render()
    {
        var color = _isFocused ? _focusedColor : _unfocusedColor;

        string displayText;
        if (string.IsNullOrEmpty(_text))
        {
            displayText = string.IsNullOrEmpty(_placeholder)
                ? " "
                : $"[dim]{Markup.Escape(_placeholder)}[/]";
        }
        else if (_isFocused)
        {
            // Show cursor position
            var beforeCursor = _text[.._cursorPosition];
            var atCursor = _cursorPosition < _text.Length ? _text[_cursorPosition].ToString() : " ";
            var afterCursor = _cursorPosition < _text.Length ? _text[(_cursorPosition + 1)..] : "";

            displayText = $"{Markup.Escape(beforeCursor)}[invert]{Markup.Escape(atCursor)}[/]{Markup.Escape(afterCursor)}";
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
}
