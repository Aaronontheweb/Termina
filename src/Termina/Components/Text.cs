namespace Termina.Components;

/// <summary>
/// Display text with optional styling
/// </summary>
public sealed class Text : Component
{
    private readonly string _content;
    private TextColor _color = TextColor.Default;
    private bool _bold;

    public Text(string content)
    {
        _content = content ?? string.Empty;
    }

    /// <summary>
    /// Set text color
    /// </summary>
    public Text Color(TextColor color)
    {
        _color = color;
        return this;
    }

    /// <summary>
    /// Make text bold
    /// </summary>
    public Text Bold()
    {
        _bold = true;
        return this;
    }

    public override string[] Render(RenderContext context)
    {
        var text = _content;

        // Apply ANSI formatting
        if (_bold)
            text = $"\x1b[1m{text}\x1b[0m";

        if (_color != TextColor.Default)
        {
            var colorCode = GetColorCode(_color);
            text = $"\x1b[{colorCode}m{text}\x1b[0m";
        }

        // Truncate if wider than available space
        if (text.Length > context.Width)
        {
            text = text.Substring(0, Math.Max(0, context.Width - 3)) + "...";
        }

        return new[] { text };
    }

    public override Size MeasureSize(int availableWidth, int availableHeight)
    {
        return new Size(Math.Min(_content.Length, availableWidth), 1);
    }

    private static int GetColorCode(TextColor color) => color switch
    {
        TextColor.Red => 31,
        TextColor.Green => 32,
        TextColor.Yellow => 33,
        TextColor.Blue => 34,
        TextColor.Magenta => 35,
        TextColor.Cyan => 36,
        TextColor.Dim => 2,
        _ => 0
    };
}

/// <summary>
/// Text color options
/// </summary>
public enum TextColor
{
    Default,
    Red,
    Green,
    Yellow,
    Blue,
    Magenta,
    Cyan,
    Dim
}
