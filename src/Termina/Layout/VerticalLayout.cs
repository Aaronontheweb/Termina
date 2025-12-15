// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A container that arranges children vertically (top to bottom).
/// </summary>
public sealed class VerticalLayout : ContainerNode
{
    /// <summary>
    /// Spacing between children in rows.
    /// </summary>
    public int Spacing { get; private set; }

    public VerticalLayout(IEnumerable<ILayoutNode> children)
    {
        AddChildren(children);
        // Default to fill
        HeightConstraint = new SizeConstraint.Fill();
        WidthConstraint = new SizeConstraint.Fill();
    }

    /// <summary>
    /// Set spacing between children.
    /// </summary>
    public VerticalLayout WithSpacing(int spacing)
    {
        Spacing = spacing;
        return this;
    }

    /// <summary>
    /// Add a child node fluently.
    /// </summary>
    public VerticalLayout WithChild(ILayoutNode child)
    {
        AddChild(child);
        return this;
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        if (Children.Count == 0)
            return Size.Zero;

        var totalHeight = 0;
        var maxWidth = 0;
        var fillCount = 0;

        // First pass: measure fixed and auto children
        foreach (var child in Children)
        {
            if (child.HeightConstraint is SizeConstraint.Fill)
            {
                fillCount++;
                continue;
            }

            var childSize = child.Measure(available with { Height = available.Height - totalHeight });
            var height = child.HeightConstraint.Compute(available.Height, childSize.Height, 0);
            totalHeight += height;
            maxWidth = Math.Max(maxWidth, childSize.Width);
        }

        // Add spacing
        totalHeight += Math.Max(0, (Children.Count - 1) * Spacing);

        // Second pass: fill children get remaining space
        var remaining = Math.Max(0, available.Height - totalHeight);
        if (fillCount > 0)
        {
            var totalWeight = Children
                .Where(c => c.HeightConstraint is SizeConstraint.Fill)
                .Sum(c => ((SizeConstraint.Fill)c.HeightConstraint).Weight);

            foreach (var child in Children.Where(c => c.HeightConstraint is SizeConstraint.Fill))
            {
                var fill = (SizeConstraint.Fill)child.HeightConstraint;
                var childHeight = remaining * fill.Weight / Math.Max(1, totalWeight);
                var childSize = child.Measure(new Size(available.Width, childHeight));
                maxWidth = Math.Max(maxWidth, childSize.Width);
            }

            totalHeight += remaining;
        }

        return new Size(
            WidthConstraint.Compute(available.Width, maxWidth, available.Width),
            HeightConstraint.Compute(available.Height, totalHeight, available.Height));
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        if (Children.Count == 0 || !bounds.HasArea)
            return;

        // Calculate heights for each child
        var heights = new int[Children.Count];
        var totalFixed = 0;
        var fillCount = 0;
        var totalWeight = 0;

        // First pass: fixed and auto heights
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.HeightConstraint is SizeConstraint.Fill fill)
            {
                fillCount++;
                totalWeight += fill.Weight;
            }
            else
            {
                var measured = child.Measure(new Size(bounds.Width, bounds.Height - totalFixed));
                heights[i] = child.HeightConstraint.Compute(bounds.Height, measured.Height, 0);
                totalFixed += heights[i];
            }
        }

        // Add spacing to total
        var totalSpacing = Math.Max(0, (Children.Count - 1) * Spacing);
        totalFixed += totalSpacing;

        // Second pass: distribute remaining to fill children
        var remaining = Math.Max(0, bounds.Height - totalFixed);
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i].HeightConstraint is SizeConstraint.Fill fill)
            {
                heights[i] = remaining * fill.Weight / Math.Max(1, totalWeight);
            }
        }

        // Render children
        var y = bounds.Y;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var childHeight = heights[i];

            if (childHeight > 0 && y < bounds.Bottom)
            {
                var actualHeight = Math.Min(childHeight, bounds.Bottom - y);
                var childBounds = new Rect(bounds.X, y, bounds.Width, actualHeight);
                child.Render(context, childBounds);
            }

            y += childHeight + Spacing;
        }
    }
}
