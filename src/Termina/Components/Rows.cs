namespace Termina.Components;

/// <summary>
/// Vertical layout - stacks children vertically
/// </summary>
public sealed class Rows : Component
{
    /// <summary>
    /// Add a child component
    /// </summary>
    public Rows Add(IComponent child)
    {
        AddChild(child);
        return this;
    }

    public override string[] Render(RenderContext context)
    {
        var lines = new List<string>();
        var remainingHeight = context.Height;

        foreach (var child in Children)
        {
            if (remainingHeight <= 0)
                break;

            var childContext = context with { Height = remainingHeight };
            var childLines = child.Render(childContext);

            lines.AddRange(childLines);
            remainingHeight -= childLines.Length;
        }

        return lines.ToArray();
    }

    public override Size MeasureSize(int availableWidth, int availableHeight)
    {
        var totalHeight = 0;
        var maxWidth = 0;

        foreach (var child in Children)
        {
            var childSize = child.MeasureSize(availableWidth, availableHeight - totalHeight);
            totalHeight += childSize.Height;
            maxWidth = Math.Max(maxWidth, childSize.Width);
        }

        return new Size(maxWidth, totalHeight);
    }
}
