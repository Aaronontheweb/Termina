using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Components;

/// <summary>
/// A status bar component for displaying status messages and hints.
/// Typically displayed at the bottom of the screen.
/// </summary>
public class StatusBar : Component
{
    private string _message = string.Empty;
    private string _hints = string.Empty;
    private Color _messageColor = Color.White;
    private Color _hintsColor = Color.Grey;
    private bool _showSeparator = true;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string Message
    {
        get => _message;
        set => _message = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the hints text (typically keyboard shortcuts).
    /// </summary>
    public string Hints
    {
        get => _hints;
        set => _hints = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the color for the message.
    /// </summary>
    public Color MessageColor
    {
        get => _messageColor;
        set => _messageColor = value;
    }

    /// <summary>
    /// Gets or sets the color for hints.
    /// </summary>
    public Color HintsColor
    {
        get => _hintsColor;
        set => _hintsColor = value;
    }

    /// <summary>
    /// Gets or sets whether to show a separator line above the status bar.
    /// </summary>
    public bool ShowSeparator
    {
        get => _showSeparator;
        set => _showSeparator = value;
    }

    /// <summary>
    /// Renders the status bar.
    /// </summary>
    public override IRenderable Render()
    {
        var items = new List<IRenderable>();

        if (_showSeparator)
        {
            items.Add(new Rule().RuleStyle(Style.Parse("dim")));
        }

        if (!string.IsNullOrEmpty(_message))
        {
            items.Add(new Markup($"[{_messageColor}]{Markup.Escape(_message)}[/]"));
        }

        if (!string.IsNullOrEmpty(_hints))
        {
            items.Add(new Markup($"[{_hintsColor}]{Markup.Escape(_hints)}[/]"));
        }

        return items.Count > 0
            ? new Rows(items)
            : new Text("");
    }
}
