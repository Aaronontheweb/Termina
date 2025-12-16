// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A container that overlays children (like a z-stack).
/// All children occupy the same space, rendered in order (last on top).
/// Useful for modals, overlays, and toasts.
/// </summary>
public sealed class StackLayout : ContainerNode
{
    public StackLayout(IEnumerable<ILayoutNode> children)
    {
        AddChildren(children);
        // Default to fill
        HeightConstraint = new SizeConstraint.Fill();
        WidthConstraint = new SizeConstraint.Fill();
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        if (Children.Count == 0)
            return Size.Zero;

        var maxWidth = 0;
        var maxHeight = 0;

        foreach (var child in Children)
        {
            var childSize = child.Measure(available);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }

        return new Size(
            WidthConstraint.Compute(available.Width, maxWidth, available.Width),
            HeightConstraint.Compute(available.Height, maxHeight, available.Height));
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        // Render all children in the same bounds, in order
        foreach (var child in Children)
        {
            child.Render(context, bounds);
        }
    }
}
