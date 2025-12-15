// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for LayoutConstraint types.
/// </summary>
public class LayoutConstraintTests
{
    [Theory]
    [InlineData(10, 100, 50, 10)]   // Fixed smaller than available
    [InlineData(100, 50, 25, 50)]  // Fixed larger than available
    [InlineData(0, 100, 50, 0)]    // Zero fixed
    public void Fixed_Compute_ReturnsMinOfSizeAndAvailable(int size, int available, int remaining, int expected)
    {
        var constraint = new LayoutConstraint.Fixed(size);

        Assert.Equal(expected, constraint.Compute(available, remaining));
    }

    [Theory]
    [InlineData(50, 100, 50, 50)]  // 50%
    [InlineData(100, 100, 0, 100)] // 100%
    [InlineData(25, 80, 40, 20)]   // 25% of 80
    [InlineData(0, 100, 50, 0)]    // 0%
    public void Percent_Compute_ReturnsPercentageOfAvailable(int percent, int available, int remaining, int expected)
    {
        var constraint = new LayoutConstraint.Percent(percent);

        Assert.Equal(expected, constraint.Compute(available, remaining));
    }

    [Theory]
    [InlineData(100, 50, 50)] // Has remaining
    [InlineData(100, 0, 0)]   // No remaining
    [InlineData(100, 100, 100)] // All remaining
    public void Remaining_Compute_ReturnsRemainingSpace(int available, int remaining, int expected)
    {
        var constraint = new LayoutConstraint.Remaining();

        Assert.Equal(expected, constraint.Compute(available, remaining));
    }

    [Fact]
    public void Remaining_NeedsRemaining_ReturnsTrue()
    {
        var constraint = new LayoutConstraint.Remaining();

        Assert.True(constraint.NeedsRemaining);
    }

    [Fact]
    public void Fixed_NeedsRemaining_ReturnsFalse()
    {
        var constraint = new LayoutConstraint.Fixed(10);

        Assert.False(constraint.NeedsRemaining);
    }

    [Theory]
    [InlineData(5, 100, 50, 5)]   // Preferred smaller than available
    [InlineData(100, 50, 25, 50)] // Preferred larger than available
    public void Auto_Compute_ReturnsMinOfPreferredAndAvailable(int preferred, int available, int remaining, int expected)
    {
        var constraint = new LayoutConstraint.Auto(preferred);

        Assert.Equal(expected, constraint.Compute(available, remaining));
    }

    [Fact]
    public void Auto_DefaultPreferredSize_IsOne()
    {
        var constraint = new LayoutConstraint.Auto();

        Assert.Equal(1, constraint.Compute(100, 50));
    }

    [Theory]
    [InlineData(0, 10, 100, 90)]  // Bottom edge
    [InlineData(5, 10, 100, 85)]  // 5 from bottom
    [InlineData(0, 1, 24, 23)]    // Status bar
    public void FromBottom_ComputePosition_CalculatesCorrectY(int offset, int size, int containerHeight, int expectedY)
    {
        var constraint = new LayoutConstraint.FromBottom(offset, size);

        Assert.Equal(expectedY, constraint.ComputePosition(containerHeight));
    }

    [Fact]
    public void FromBottom_Compute_ReturnsSize()
    {
        var constraint = new LayoutConstraint.FromBottom(5, 10);

        Assert.Equal(10, constraint.Compute(100, 50));
    }

    [Theory]
    [InlineData(0, 20, 80, 60)]  // Right edge
    [InlineData(10, 20, 80, 50)] // 10 from right
    public void FromRight_ComputePosition_CalculatesCorrectX(int offset, int size, int containerWidth, int expectedX)
    {
        var constraint = new LayoutConstraint.FromRight(offset, size);

        Assert.Equal(expectedX, constraint.ComputePosition(containerWidth));
    }

    [Fact]
    public void FromRight_Compute_ReturnsSize()
    {
        var constraint = new LayoutConstraint.FromRight(5, 20);

        Assert.Equal(20, constraint.Compute(100, 50));
    }
}
