// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// Represents a node in the layout tree.
/// Layout nodes form a composable tree structure where each node can measure itself,
/// arrange children, and render to a terminal context.
/// </summary>
public interface ILayoutNode : IDisposable
{
    /// <summary>
    /// Width constraint for this node.
    /// </summary>
    SizeConstraint WidthConstraint { get; }

    /// <summary>
    /// Height constraint for this node.
    /// </summary>
    SizeConstraint HeightConstraint { get; }

    /// <summary>
    /// Measure the desired size of this node given available space.
    /// </summary>
    /// <param name="available">Maximum available space.</param>
    /// <returns>Desired size (may be less than available).</returns>
    Size Measure(Size available);

    /// <summary>
    /// Render this node to the given context within the specified bounds.
    /// </summary>
    /// <param name="context">The render context to draw to.</param>
    /// <param name="bounds">The bounds allocated to this node.</param>
    void Render(IRenderContext context, Rect bounds);
}

/// <summary>
/// A layout node that can contain children and needs to arrange them.
/// </summary>
public interface IContainerNode : ILayoutNode
{
    /// <summary>
    /// Child nodes.
    /// </summary>
    IReadOnlyList<ILayoutNode> Children { get; }
}

/// <summary>
/// A layout node that can notify when it needs to be re-rendered.
/// Used for reactive bindings and stateful components.
/// </summary>
public interface IInvalidatingNode : ILayoutNode
{
    /// <summary>
    /// Raised when this node's content has changed and needs re-rendering.
    /// </summary>
    event Action? Invalidated;
}

/// <summary>
/// A layout node with internal state that ticks over time (spinners, timers).
/// </summary>
public interface IAnimatedNode : ILayoutNode
{
    /// <summary>
    /// Whether this node is currently animating.
    /// </summary>
    bool IsAnimating { get; }

    /// <summary>
    /// Start the animation timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop the animation timer.
    /// </summary>
    void Stop();
}
