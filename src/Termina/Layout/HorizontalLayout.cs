// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A container that arranges children horizontally (left to right).
/// </summary>
public sealed class HorizontalLayout : ContainerNode
{
    /// <summary>
    /// Spacing between children in columns.
    /// </summary>
    public int Spacing { get; private set; }

    public HorizontalLayout(IEnumerable<ILayoutNode> children)
    {
        AddChildren(children);
        // Default to fill
        HeightConstraint = new SizeConstraint.Fill();
        WidthConstraint = new SizeConstraint.Fill();
    }

    /// <summary>
    /// Set spacing between children.
    /// </summary>
    public HorizontalLayout WithSpacing(int spacing)
    {
        Spacing = spacing;
        return this;
    }

    /// <summary>
    /// Add a child node fluently.
    /// </summary>
    public HorizontalLayout WithChild(ILayoutNode child)
    {
        AddChild(child);
        return this;
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        if (Children.Count == 0)
            return Size.Zero;

        var totalWidth = 0;
        var maxHeight = 0;
        var fillCount = 0;

        // First pass: measure fixed and auto children
        foreach (var child in Children)
        {
            if (child.WidthConstraint is SizeConstraint.Fill)
            {
                fillCount++;
                continue;
            }

            var childSize = child.Measure(available with { Width = available.Width - totalWidth });
            var width = child.WidthConstraint.Compute(available.Width, childSize.Width, 0);
            totalWidth += width;
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }

        // Add spacing
        totalWidth += Math.Max(0, (Children.Count - 1) * Spacing);

        // Second pass: fill children get remaining space
        var remaining = Math.Max(0, available.Width - totalWidth);
        if (fillCount > 0)
        {
            var totalWeight = Children
                .Where(c => c.WidthConstraint is SizeConstraint.Fill)
                .Sum(c => ((SizeConstraint.Fill)c.WidthConstraint).Weight);

            foreach (var child in Children.Where(c => c.WidthConstraint is SizeConstraint.Fill))
            {
                var fill = (SizeConstraint.Fill)child.WidthConstraint;
                var childWidth = remaining * fill.Weight / Math.Max(1, totalWeight);
                var childSize = child.Measure(new Size(childWidth, available.Height));
                maxHeight = Math.Max(maxHeight, childSize.Height);
            }

            totalWidth += remaining;
        }

        return new Size(
            WidthConstraint.Compute(available.Width, totalWidth, available.Width),
            HeightConstraint.Compute(available.Height, maxHeight, available.Height));
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        if (Children.Count == 0 || !bounds.HasArea)
            return;

        // Calculate widths for each child
        var widths = new int[Children.Count];
        var totalFixed = 0;
        var totalWeight = 0;

        // First pass: fixed and auto widths
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (child.WidthConstraint is SizeConstraint.Fill fill)
            {
                totalWeight += fill.Weight;
            }
            else
            {
                var measured = child.Measure(new Size(bounds.Width - totalFixed, bounds.Height));
                widths[i] = child.WidthConstraint.Compute(bounds.Width, measured.Width, 0);
                totalFixed += widths[i];
            }
        }

        // Add spacing to total
        var totalSpacing = Math.Max(0, (Children.Count - 1) * Spacing);
        totalFixed += totalSpacing;

        // Second pass: distribute remaining to fill children
        var remaining = Math.Max(0, bounds.Width - totalFixed);
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i].WidthConstraint is SizeConstraint.Fill fill)
            {
                widths[i] = remaining * fill.Weight / Math.Max(1, totalWeight);
            }
        }

        // Render children
        var x = bounds.X;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var childWidth = widths[i];

            if (childWidth > 0 && x < bounds.Right)
            {
                var actualWidth = Math.Min(childWidth, bounds.Right - x);
                var childBounds = new Rect(x, bounds.Y, actualWidth, bounds.Height);
                child.Render(context, childBounds);
            }

            x += childWidth + Spacing;
        }
    }
}
