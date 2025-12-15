// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Represents a 2D size with width and height.
/// </summary>
/// <param name="Width">Width in columns.</param>
/// <param name="Height">Height in rows.</param>
public readonly record struct Size(int Width, int Height)
{
    /// <summary>
    /// Zero size.
    /// </summary>
    public static readonly Size Zero = new(0, 0);

    /// <summary>
    /// Infinite size (for unconstrained measurement).
    /// </summary>
    public static readonly Size Infinite = new(int.MaxValue, int.MaxValue);

    /// <summary>
    /// Create a size with equal width and height.
    /// </summary>
    public static Size Square(int size) => new(size, size);

    /// <summary>
    /// Returns a new size constrained to the given maximum.
    /// </summary>
    public Size Constrain(Size max) => new(
        Math.Min(Width, max.Width),
        Math.Min(Height, max.Height));

    /// <summary>
    /// Returns a new size with at least the given minimum.
    /// </summary>
    public Size AtLeast(Size min) => new(
        Math.Max(Width, min.Width),
        Math.Max(Height, min.Height));

    /// <summary>
    /// Returns a new size expanded by the given padding.
    /// </summary>
    public Size Expand(int horizontal, int vertical) => new(
        Width + horizontal,
        Height + vertical);

    /// <summary>
    /// Returns a new size shrunk by the given padding.
    /// </summary>
    public Size Shrink(int horizontal, int vertical) => new(
        Math.Max(0, Width - horizontal),
        Math.Max(0, Height - vertical));
}
