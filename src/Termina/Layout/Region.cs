// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// Defines a region on the screen with layout constraints.
/// Regions are non-overlapping areas that can be independently rendered.
/// </summary>
public sealed class Region
{
    /// <summary>
    /// Create a new region with the specified constraints.
    /// </summary>
    /// <param name="id">Unique identifier for this region.</param>
    /// <param name="x">X constraint (column positioning).</param>
    /// <param name="y">Y constraint (row positioning).</param>
    /// <param name="width">Width constraint.</param>
    /// <param name="height">Height constraint.</param>
    public Region(string id, LayoutConstraint x, LayoutConstraint y, LayoutConstraint width, LayoutConstraint height)
    {
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Unique identifier for this region.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// X position constraint.
    /// </summary>
    public LayoutConstraint X { get; }

    /// <summary>
    /// Y position constraint.
    /// </summary>
    public LayoutConstraint Y { get; }

    /// <summary>
    /// Width constraint.
    /// </summary>
    public LayoutConstraint Width { get; }

    /// <summary>
    /// Height constraint.
    /// </summary>
    public LayoutConstraint Height { get; }

    /// <summary>
    /// Whether this region can receive focus.
    /// </summary>
    public bool Focusable { get; init; }

    /// <summary>
    /// Tab order for focus navigation (lower = earlier).
    /// </summary>
    public int TabOrder { get; init; }

    /// <summary>
    /// The current content to render in this region.
    /// </summary>
    public IRenderable? Content { get; private set; }

    /// <summary>
    /// The computed screen bounds after layout calculation.
    /// </summary>
    public ScreenBounds Bounds { get; internal set; }

    /// <summary>
    /// Whether this region needs to be redrawn.
    /// </summary>
    public bool IsDirty { get; internal set; } = true;

    /// <summary>
    /// Set the content for this region and mark it dirty.
    /// </summary>
    /// <param name="content">The new content to render.</param>
    public void SetContent(IRenderable? content)
    {
        Content = content;
        IsDirty = true;
    }

    /// <summary>
    /// Mark this region as needing redraw.
    /// </summary>
    public void Invalidate()
    {
        IsDirty = true;
    }

    /// <summary>
    /// Create a region that fills the entire screen.
    /// </summary>
    public static Region FullScreen(string id)
        => new(id,
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Remaining(),
            new LayoutConstraint.Remaining());

    /// <summary>
    /// Create a region at a fixed position with fixed size.
    /// </summary>
    public static Region Fixed(string id, int x, int y, int width, int height)
        => new(id,
            new LayoutConstraint.Fixed(x),
            new LayoutConstraint.Fixed(y),
            new LayoutConstraint.Fixed(width),
            new LayoutConstraint.Fixed(height));

    /// <summary>
    /// Create a region that takes a horizontal slice at the top.
    /// </summary>
    public static Region TopRow(string id, int height)
        => new(id,
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Remaining(),
            new LayoutConstraint.Fixed(height));

    /// <summary>
    /// Create a region that takes a horizontal slice at the bottom.
    /// </summary>
    public static Region BottomRow(string id, int height)
        => new(id,
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.FromBottom(0, height),
            new LayoutConstraint.Remaining(),
            new LayoutConstraint.Fixed(height));
}
