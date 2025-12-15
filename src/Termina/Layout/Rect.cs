// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
/// <param name="X">Left column.</param>
/// <param name="Y">Top row.</param>
/// <param name="Width">Width in columns.</param>
/// <param name="Height">Height in rows.</param>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Empty rectangle at origin.
    /// </summary>
    public static readonly Rect Empty = new(0, 0, 0, 0);

    /// <summary>
    /// Create a rectangle from position and size.
    /// </summary>
    public static Rect FromSize(int x, int y, Size size) => new(x, y, size.Width, size.Height);

    /// <summary>
    /// Right edge (exclusive).
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Bottom edge (exclusive).
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Size of this rectangle.
    /// </summary>
    public Size Size => new(Width, Height);

    /// <summary>
    /// Whether this rectangle has any area.
    /// </summary>
    public bool HasArea => Width > 0 && Height > 0;

    /// <summary>
    /// Returns a new rectangle inset by the given amounts.
    /// </summary>
    public Rect Inset(int left, int top, int right, int bottom) => new(
        X + left,
        Y + top,
        Math.Max(0, Width - left - right),
        Math.Max(0, Height - top - bottom));

    /// <summary>
    /// Returns a new rectangle inset uniformly.
    /// </summary>
    public Rect Inset(int amount) => Inset(amount, amount, amount, amount);

    /// <summary>
    /// Returns a new rectangle offset by the given amounts.
    /// </summary>
    public Rect Offset(int dx, int dy) => new(X + dx, Y + dy, Width, Height);

    /// <summary>
    /// Returns a new rectangle with the given size, keeping position.
    /// </summary>
    public Rect WithSize(Size size) => new(X, Y, size.Width, size.Height);

    /// <summary>
    /// Returns whether this rectangle contains the given point.
    /// </summary>
    public bool Contains(int x, int y) =>
        x >= X && x < Right && y >= Y && y < Bottom;

    /// <summary>
    /// Returns the intersection of this rectangle with another.
    /// </summary>
    public Rect Intersect(Rect other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        if (right <= x || bottom <= y)
            return Empty;

        return new Rect(x, y, right - x, bottom - y);
    }
}
