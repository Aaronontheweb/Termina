// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Represents the absolute bounds of a region on screen.
/// </summary>
/// <param name="X">X position (column) of top-left corner.</param>
/// <param name="Y">Y position (row) of top-left corner.</param>
/// <param name="Width">Width in columns.</param>
/// <param name="Height">Height in rows.</param>
public readonly record struct ScreenBounds(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Empty bounds at origin with zero size.
    /// </summary>
    public static ScreenBounds Empty => new(0, 0, 0, 0);

    /// <summary>
    /// The right edge X coordinate (exclusive).
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// The bottom edge Y coordinate (exclusive).
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Check if a point is within these bounds.
    /// </summary>
    public bool Contains(int x, int y)
        => x >= X && x < Right && y >= Y && y < Bottom;

    /// <summary>
    /// Check if these bounds intersect with other bounds.
    /// </summary>
    public bool Intersects(ScreenBounds other)
        => X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    /// <summary>
    /// Compute the intersection of these bounds with other bounds.
    /// </summary>
    public ScreenBounds Intersect(ScreenBounds other)
    {
        var newX = Math.Max(X, other.X);
        var newY = Math.Max(Y, other.Y);
        var newRight = Math.Min(Right, other.Right);
        var newBottom = Math.Min(Bottom, other.Bottom);

        if (newRight <= newX || newBottom <= newY)
            return Empty;

        return new ScreenBounds(newX, newY, newRight - newX, newBottom - newY);
    }
}
