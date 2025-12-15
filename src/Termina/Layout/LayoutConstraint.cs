// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Defines how a dimension (width or height) should be calculated.
/// </summary>
public abstract record LayoutConstraint
{
    /// <summary>
    /// Calculate the actual size based on available space.
    /// </summary>
    /// <param name="available">The total available space.</param>
    /// <param name="remaining">The remaining space after fixed allocations.</param>
    /// <returns>The computed size.</returns>
    public abstract int Compute(int available, int remaining);

    /// <summary>
    /// Whether this constraint requires the remaining space calculation.
    /// </summary>
    public virtual bool NeedsRemaining => false;

    /// <summary>
    /// Fixed size in units (columns or rows).
    /// </summary>
    /// <param name="Size">The fixed size.</param>
    public sealed record Fixed(int Size) : LayoutConstraint
    {
        public override int Compute(int available, int remaining) => Math.Min(Size, available);
    }

    /// <summary>
    /// Percentage of available space.
    /// </summary>
    /// <param name="Value">The percentage (0-100).</param>
    public sealed record Percent(int Value) : LayoutConstraint
    {
        public override int Compute(int available, int remaining) => available * Value / 100;
    }

    /// <summary>
    /// Take all remaining space after other constraints are satisfied.
    /// </summary>
    public sealed record Remaining : LayoutConstraint
    {
        public override bool NeedsRemaining => true;
        public override int Compute(int available, int remaining) => Math.Max(0, remaining);
    }

    /// <summary>
    /// Auto-size based on content (uses content's measured size).
    /// Currently behaves like Fixed(1) - actual auto-sizing happens at render time.
    /// </summary>
    /// <param name="PreferredSize">The preferred size if content measurement isn't available.</param>
    public sealed record Auto(int PreferredSize = 1) : LayoutConstraint
    {
        public override int Compute(int available, int remaining) => Math.Min(PreferredSize, available);
    }

    /// <summary>
    /// Position from the bottom edge of the container.
    /// Used for Y positioning.
    /// </summary>
    /// <param name="Offset">Distance from the bottom.</param>
    /// <param name="Size">Height of the region.</param>
    public sealed record FromBottom(int Offset, int Size) : LayoutConstraint
    {
        public override int Compute(int available, int remaining) => Size;

        /// <summary>
        /// Calculate the Y position based on container height.
        /// </summary>
        public int ComputePosition(int containerHeight) => containerHeight - Offset - Size;
    }

    /// <summary>
    /// Position from the right edge of the container.
    /// Used for X positioning.
    /// </summary>
    /// <param name="Offset">Distance from the right.</param>
    /// <param name="Size">Width of the region.</param>
    public sealed record FromRight(int Offset, int Size) : LayoutConstraint
    {
        public override int Compute(int available, int remaining) => Size;

        /// <summary>
        /// Calculate the X position based on container width.
        /// </summary>
        public int ComputePosition(int containerWidth) => containerWidth - Offset - Size;
    }
}
