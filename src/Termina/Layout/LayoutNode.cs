// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// Base class for layout nodes providing common functionality.
/// </summary>
public abstract class LayoutNode : ILayoutNode
{
    private SizeConstraint _widthConstraint = new SizeConstraint.Auto();
    private SizeConstraint _heightConstraint = new SizeConstraint.Auto();

    /// <inheritdoc />
    public SizeConstraint WidthConstraint
    {
        get => _widthConstraint;
        protected set => _widthConstraint = value;
    }

    /// <inheritdoc />
    public SizeConstraint HeightConstraint
    {
        get => _heightConstraint;
        protected set => _heightConstraint = value;
    }

    /// <inheritdoc />
    public abstract Size Measure(Size available);

    /// <inheritdoc />
    public abstract void Render(IRenderContext context, Rect bounds);

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Set width to a fixed value.
    /// </summary>
    public LayoutNode Width(int value)
    {
        _widthConstraint = new SizeConstraint.Fixed(value);
        return this;
    }

    /// <summary>
    /// Set width to fill remaining space.
    /// </summary>
    public LayoutNode WidthFill(int weight = 1)
    {
        _widthConstraint = new SizeConstraint.Fill { Weight = weight };
        return this;
    }

    /// <summary>
    /// Set width to auto-size based on content.
    /// </summary>
    public LayoutNode WidthAuto(int min = 0, int max = int.MaxValue)
    {
        _widthConstraint = new SizeConstraint.Auto { Min = min, Max = max };
        return this;
    }

    /// <summary>
    /// Set width to a percentage.
    /// </summary>
    public LayoutNode WidthPercent(int value)
    {
        _widthConstraint = new SizeConstraint.Percent(value);
        return this;
    }

    /// <summary>
    /// Set height to a fixed value.
    /// </summary>
    public LayoutNode Height(int value)
    {
        _heightConstraint = new SizeConstraint.Fixed(value);
        return this;
    }

    /// <summary>
    /// Set height to fill remaining space.
    /// </summary>
    public LayoutNode Fill(int weight = 1)
    {
        _heightConstraint = new SizeConstraint.Fill { Weight = weight };
        return this;
    }

    /// <summary>
    /// Set height to auto-size based on content.
    /// </summary>
    public LayoutNode HeightAuto(int min = 0, int max = int.MaxValue)
    {
        _heightConstraint = new SizeConstraint.Auto { Min = min, Max = max };
        return this;
    }

    /// <summary>
    /// Set height to a percentage.
    /// </summary>
    public LayoutNode HeightPercent(int value)
    {
        _heightConstraint = new SizeConstraint.Percent(value);
        return this;
    }
}

/// <summary>
/// Base class for container nodes that arrange children.
/// </summary>
public abstract class ContainerNode : LayoutNode, IContainerNode
{
    private readonly List<ILayoutNode> _children = new();

    /// <inheritdoc />
    public IReadOnlyList<ILayoutNode> Children => _children;

    /// <summary>
    /// Add a child node.
    /// </summary>
    protected void AddChild(ILayoutNode child)
    {
        _children.Add(child);
    }

    /// <summary>
    /// Add multiple child nodes.
    /// </summary>
    protected void AddChildren(IEnumerable<ILayoutNode> children)
    {
        _children.AddRange(children);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        foreach (var child in _children)
        {
            child.Dispose();
        }
        base.Dispose();
    }
}
