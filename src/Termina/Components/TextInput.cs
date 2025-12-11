using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Input;

namespace Termina.Components;

// Business events for TextInput

/// <summary>
/// Command: Set the text content programmatically.
/// </summary>
public sealed record SetText(string Text);

/// <summary>
/// Notification: The text content changed (user typed or deleted).
/// </summary>
public sealed record TextChanged(string Text);

/// <summary>
/// Notification: User submitted the text (pressed Enter).
/// </summary>
public sealed record TextSubmitted(string Text);

/// <summary>
/// A text input component.
/// Handles character input and backspace internally, emits business events on changes and submit.
/// </summary>
public sealed class TextInput : Component
{
    private string _text = "";
    private readonly string _placeholder;
    private readonly string _label;

    public TextInput(string label = "", string placeholder = "")
    {
        _label = label;
        _placeholder = placeholder;

        // Subscribe to business commands
        Subscribe<SetText>(OnSetText);
    }

    private void OnSetText(SetText evt)
    {
        _text = evt.Text;
    }

    /// <summary>
    /// Handle keyboard input for text entry.
    /// </summary>
    public override void HandleInput(KeyPressed key)
    {
        var info = key.KeyInfo;

        // Enter submits
        if (info.Key == ConsoleKey.Enter)
        {
            Emit(new TextSubmitted(_text));
            return;
        }

        // Backspace deletes last character
        if (info.Key == ConsoleKey.Backspace && _text.Length > 0)
        {
            _text = _text[..^1];
            Emit(new TextChanged(_text));
            return;
        }

        // Regular character input
        if (!char.IsControl(info.KeyChar))
        {
            _text += info.KeyChar;
            Emit(new TextChanged(_text));
        }
    }

    /// <summary>
    /// Get the current text value.
    /// </summary>
    public string Text => _text;

    public override IRenderable Render()
    {
        string displayText;
        if (string.IsNullOrEmpty(_text))
        {
            displayText = string.IsNullOrEmpty(_placeholder)
                ? "[blink]|[/]"
                : $"[grey]{Markup.Escape(_placeholder)}[/][blink]|[/]";
        }
        else
        {
            displayText = $"{Markup.Escape(_text)}[blink]|[/]";
        }

        IRenderable content = new Markup(displayText);

        if (!string.IsNullOrEmpty(_label))
        {
            content = new Panel(content)
                .Header($"[bold]{Markup.Escape(_label)}[/]")
                .Border(BoxBorder.Rounded);
        }

        return content;
    }
}
