// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for ScreenBounds.
/// </summary>
public class ScreenBoundsTests
{
    [Fact]
    public void Empty_HasZeroSize()
    {
        var empty = ScreenBounds.Empty;

        Assert.Equal(0, empty.X);
        Assert.Equal(0, empty.Y);
        Assert.Equal(0, empty.Width);
        Assert.Equal(0, empty.Height);
    }

    [Fact]
    public void Right_ReturnsXPlusWidth()
    {
        var bounds = new ScreenBounds(10, 5, 20, 15);

        Assert.Equal(30, bounds.Right);
    }

    [Fact]
    public void Bottom_ReturnsYPlusHeight()
    {
        var bounds = new ScreenBounds(10, 5, 20, 15);

        Assert.Equal(20, bounds.Bottom);
    }

    [Theory]
    [InlineData(10, 5, true)]   // Inside
    [InlineData(0, 0, false)]   // Outside top-left
    [InlineData(29, 19, true)]  // Inside at right-bottom edge
    [InlineData(30, 20, false)] // Just outside right-bottom
    [InlineData(15, 10, true)]  // Center
    public void Contains_ReturnsCorrectResult(int x, int y, bool expected)
    {
        var bounds = new ScreenBounds(10, 5, 20, 15);

        Assert.Equal(expected, bounds.Contains(x, y));
    }

    [Fact]
    public void Intersects_OverlappingBounds_ReturnsTrue()
    {
        var a = new ScreenBounds(0, 0, 20, 20);
        var b = new ScreenBounds(10, 10, 20, 20);

        Assert.True(a.Intersects(b));
        Assert.True(b.Intersects(a));
    }

    [Fact]
    public void Intersects_NonOverlappingBounds_ReturnsFalse()
    {
        var a = new ScreenBounds(0, 0, 10, 10);
        var b = new ScreenBounds(20, 20, 10, 10);

        Assert.False(a.Intersects(b));
        Assert.False(b.Intersects(a));
    }

    [Fact]
    public void Intersects_AdjacentBounds_ReturnsFalse()
    {
        var a = new ScreenBounds(0, 0, 10, 10);
        var b = new ScreenBounds(10, 0, 10, 10); // Touching but not overlapping

        Assert.False(a.Intersects(b));
    }

    [Fact]
    public void Intersect_ReturnsOverlapArea()
    {
        var a = new ScreenBounds(0, 0, 20, 20);
        var b = new ScreenBounds(10, 10, 20, 20);

        var intersection = a.Intersect(b);

        Assert.Equal(10, intersection.X);
        Assert.Equal(10, intersection.Y);
        Assert.Equal(10, intersection.Width);
        Assert.Equal(10, intersection.Height);
    }

    [Fact]
    public void Intersect_NoOverlap_ReturnsEmpty()
    {
        var a = new ScreenBounds(0, 0, 10, 10);
        var b = new ScreenBounds(20, 20, 10, 10);

        var intersection = a.Intersect(b);

        Assert.Equal(ScreenBounds.Empty, intersection);
    }

    [Fact]
    public void Intersect_ContainedBounds_ReturnsSmallerBounds()
    {
        var outer = new ScreenBounds(0, 0, 100, 100);
        var inner = new ScreenBounds(25, 25, 50, 50);

        var intersection = outer.Intersect(inner);

        Assert.Equal(inner, intersection);
    }
}
