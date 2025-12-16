// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// An empty layout node that renders nothing and takes no space.
/// Useful as a placeholder or for conditional rendering.
/// </summary>
public sealed class EmptyNode : LayoutNode
{
    public EmptyNode()
    {
        WidthConstraint = new SizeConstraint.Fixed(0);
        HeightConstraint = new SizeConstraint.Fixed(0);
    }

    /// <inheritdoc />
    public override Size Measure(Size available) => Size.Zero;

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        // Nothing to render
    }
}
