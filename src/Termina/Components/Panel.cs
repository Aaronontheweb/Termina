using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Components;

/// <summary>
/// A layout container component that wraps content in a bordered panel.
/// </summary>
public class ContentPanel : Component
{
    private IRenderable _content = new Text("");
    private string _title = string.Empty;
    private bool _expand = true;
    private BoxBorder _border = BoxBorder.Rounded;
    private Color _borderColor = Color.Grey;

    /// <summary>
    /// Gets or sets the content to display inside the panel.
    /// </summary>
    public IRenderable Content
    {
        get => _content;
        set => _content = value ?? new Text("");
    }

    /// <summary>
    /// Gets or sets the panel title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => _title = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets whether the panel expands to fill available width.
    /// </summary>
    public bool Expand
    {
        get => _expand;
        set => _expand = value;
    }

    /// <summary>
    /// Gets or sets the border style.
    /// </summary>
    public BoxBorder Border
    {
        get => _border;
        set => _border = value;
    }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color BorderColor
    {
        get => _borderColor;
        set => _borderColor = value;
    }

    /// <summary>
    /// Renders the panel.
    /// </summary>
    public override IRenderable Render()
    {
        var panel = new Spectre.Console.Panel(_content)
            .Border(_border)
            .BorderColor(_borderColor);

        if (_expand)
        {
            panel.Expand();
        }

        if (!string.IsNullOrEmpty(_title))
        {
            panel.Header(_title);
        }

        return panel;
    }
}
