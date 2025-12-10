namespace Termina.Components;

/// <summary>
/// Container with border and optional header
/// </summary>
public sealed class Panel : Component
{
    private readonly string? _header;
    private BoxBorder _border = BoxBorder.Rounded;

    public Panel(string? header = null)
    {
        _header = header;
    }

    /// <summary>
    /// Add a child component
    /// </summary>
    public Panel Add(IComponent child)
    {
        AddChild(child);
        return this;
    }

    /// <summary>
    /// Set border style
    /// </summary>
    public Panel Border(BoxBorder border)
    {
        _border = border;
        return this;
    }

    public override string[] Render(RenderContext context)
    {
        var lines = new List<string>();
        var (topLeft, topRight, bottomLeft, bottomRight, horizontal, vertical) = GetBorderChars(_border);

        // Top border with optional header
        var headerText = _header ?? "";
        var topBorder = $"{topLeft}{horizontal} {headerText} ";
        var remainingWidth = context.Width - topBorder.Length - 1;
        if (remainingWidth > 0)
        {
            topBorder += new string(horizontal, remainingWidth);
        }
        topBorder += topRight;
        lines.Add(topBorder);

        // Render children with padding
        if (Children.Count > 0)
        {
            var innerWidth = Math.Max(1, context.Width - 4); // 2 for borders, 2 for padding
            var innerHeight = Math.Max(1, context.Height - 2); // 2 for top/bottom borders
            var childContext = context with { Width = innerWidth, Height = innerHeight };

            foreach (var child in Children)
            {
                var childLines = child.Render(childContext);
                foreach (var line in childLines)
                {
                    var paddedLine = line.PadRight(innerWidth);
                    if (paddedLine.Length > innerWidth)
                        paddedLine = paddedLine.Substring(0, innerWidth);

                    lines.Add($"{vertical} {paddedLine} {vertical}");
                }
            }
        }

        // Bottom border
        var bottomBorder = $"{bottomLeft}{new string(horizontal, Math.Max(0, context.Width - 2))}{bottomRight}";
        lines.Add(bottomBorder);

        return lines.ToArray();
    }

    public override Size MeasureSize(int availableWidth, int availableHeight)
    {
        var innerWidth = Math.Max(1, availableWidth - 4);
        var innerHeight = Math.Max(1, availableHeight - 2);

        var contentHeight = 0;
        var contentWidth = 0;

        foreach (var child in Children)
        {
            var childSize = child.MeasureSize(innerWidth, innerHeight - contentHeight);
            contentHeight += childSize.Height;
            contentWidth = Math.Max(contentWidth, childSize.Width);
        }

        return new Size(contentWidth + 4, contentHeight + 2);
    }

    private static (char topLeft, char topRight, char bottomLeft, char bottomRight, char horizontal, char vertical) GetBorderChars(BoxBorder border)
    {
        return border switch
        {
            BoxBorder.Single => ('┌', '┐', '└', '┘', '─', '│'),
            BoxBorder.Double => ('╔', '╗', '╚', '╝', '═', '║'),
            BoxBorder.Rounded => ('╭', '╮', '╰', '╯', '─', '│'),
            BoxBorder.Heavy => ('┏', '┓', '┗', '┛', '━', '┃'),
            _ => ('+', '+', '+', '+', '-', '|')
        };
    }
}

/// <summary>
/// Border styles for panels
/// </summary>
public enum BoxBorder
{
    None,
    Single,
    Double,
    Rounded,
    Heavy
}
