using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Components;

/// <summary>
/// A list component for selecting from a list of options.
/// State is managed externally by the ViewModel and set via properties.
/// </summary>
public class SelectList : Component
{
    private IReadOnlyList<string> _options = Array.Empty<string>();
    private int _selectedIndex;
    private string _title = string.Empty;
    private bool _showBorder = true;
    private Color _selectedColor = Color.Blue;
    private Color _normalColor = Color.White;

    /// <summary>
    /// Gets or sets the title displayed above the list.
    /// </summary>
    public string Title
    {
        get => _title;
        set => _title = value;
    }

    /// <summary>
    /// Gets or sets the list of options to display.
    /// </summary>
    public IReadOnlyList<string> Options
    {
        get => _options;
        set => _options = value ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets or sets the currently selected index.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => _selectedIndex = Math.Max(0, Math.Min(value, _options.Count - 1));
    }

    /// <summary>
    /// Gets or sets whether to show a border around the list.
    /// </summary>
    public bool ShowBorder
    {
        get => _showBorder;
        set => _showBorder = value;
    }

    /// <summary>
    /// Gets or sets the color for selected items.
    /// </summary>
    public Color SelectedColor
    {
        get => _selectedColor;
        set => _selectedColor = value;
    }

    /// <summary>
    /// Gets or sets the color for normal (unselected) items.
    /// </summary>
    public Color NormalColor
    {
        get => _normalColor;
        set => _normalColor = value;
    }

    /// <summary>
    /// Gets or sets whether to interpret Spectre.Console markup in option text.
    /// When false (default), markup characters are escaped for safety.
    /// When true, markup like [bold], [red], etc. will be rendered.
    /// </summary>
    public bool AllowMarkup { get; set; }

    /// <summary>
    /// Gets the currently selected option, or null if no options exist.
    /// </summary>
    public string? SelectedOption =>
        _options.Count > 0 && _selectedIndex < _options.Count
            ? _options[_selectedIndex]
            : null;

    /// <summary>
    /// Renders the select list.
    /// </summary>
    public override IRenderable Render()
    {
        if (_options.Count == 0)
        {
            var emptyText = new Markup("[dim]No items[/]");
            return _showBorder
                ? new Panel(emptyText).Header(_title).Expand()
                : emptyText;
        }

        var rows = new List<IRenderable>();

        for (var i = 0; i < _options.Count; i++)
        {
            var option = _options[i];
            var isSelected = i == _selectedIndex;
            var displayText = AllowMarkup ? option : Markup.Escape(option);

            var text = isSelected
                ? new Markup($"[bold {_selectedColor}]> {displayText}[/]")
                : new Markup($"[{_normalColor}]  {displayText}[/]");

            rows.Add(text);
        }

        var content = new Rows(rows);

        if (_showBorder)
        {
            var panel = new Panel(content).Expand();
            if (!string.IsNullOrEmpty(_title))
            {
                panel.Header(_title);
            }
            return panel;
        }

        return content;
    }
}
