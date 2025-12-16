// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Defines how a layout node should be sized along one axis.
/// </summary>
public abstract record SizeConstraint
{
    /// <summary>
    /// Compute the actual size given available and remaining space.
    /// </summary>
    /// <param name="available">Total available space.</param>
    /// <param name="contentSize">Measured content size (for auto-sizing).</param>
    /// <param name="remaining">Remaining space after fixed allocations.</param>
    public abstract int Compute(int available, int contentSize, int remaining);

    /// <summary>
    /// Whether this constraint needs to know the remaining space.
    /// </summary>
    public virtual bool NeedsRemaining => false;

    /// <summary>
    /// Whether this constraint needs content measurement.
    /// </summary>
    public virtual bool NeedsContentSize => false;

    /// <summary>
    /// Fixed size in units (columns or rows).
    /// </summary>
    public sealed record Fixed(int Value) : SizeConstraint
    {
        public override int Compute(int available, int contentSize, int remaining) =>
            Math.Min(Value, available);
    }

    /// <summary>
    /// Take all remaining space after other constraints are satisfied.
    /// </summary>
    public sealed record Fill : SizeConstraint
    {
        /// <summary>
        /// Weight for proportional fill when multiple fills exist.
        /// </summary>
        public int Weight { get; init; } = 1;

        public override bool NeedsRemaining => true;

        public override int Compute(int available, int contentSize, int remaining) =>
            Math.Max(0, remaining);
    }

    /// <summary>
    /// Auto-size based on content measurement.
    /// </summary>
    public sealed record Auto : SizeConstraint
    {
        /// <summary>
        /// Minimum size.
        /// </summary>
        public int Min { get; init; } = 0;

        /// <summary>
        /// Maximum size (int.MaxValue for unbounded).
        /// </summary>
        public int Max { get; init; } = int.MaxValue;

        public override bool NeedsContentSize => true;

        public override int Compute(int available, int contentSize, int remaining)
        {
            var size = Math.Max(Min, contentSize);
            size = Math.Min(size, Max);
            return Math.Min(size, available);
        }
    }

    /// <summary>
    /// Percentage of available space.
    /// </summary>
    public sealed record Percent(int Value) : SizeConstraint
    {
        public override int Compute(int available, int contentSize, int remaining) =>
            available * Math.Clamp(Value, 0, 100) / 100;
    }

    // Factory methods for fluent API

    /// <summary>
    /// Fixed size.
    /// </summary>
    public static SizeConstraint Exactly(int value) => new Fixed(value);

    /// <summary>
    /// Fill remaining space.
    /// </summary>
    public static SizeConstraint FillRemaining(int weight = 1) => new Fill { Weight = weight };

    /// <summary>
    /// Auto-size to content.
    /// </summary>
    public static SizeConstraint AutoSize(int min = 0, int max = int.MaxValue) =>
        new Auto { Min = min, Max = max };

    /// <summary>
    /// Percentage of available space.
    /// </summary>
    public static SizeConstraint Percentage(int value) => new Percent(value);
}
