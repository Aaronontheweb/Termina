// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for LayoutEngine.
/// </summary>
public class LayoutEngineTests
{
    [Fact]
    public void ComputeLayout_FixedRegion_CalculatesCorrectBounds()
    {
        var region = Region.Fixed("test", 10, 5, 20, 15);
        var regions = new[] { region };

        LayoutEngine.ComputeLayout(regions, 80, 24);

        Assert.Equal(10, region.Bounds.X);
        Assert.Equal(5, region.Bounds.Y);
        Assert.Equal(20, region.Bounds.Width);
        Assert.Equal(15, region.Bounds.Height);
    }

    [Fact]
    public void ComputeLayout_FullScreenRegion_FillsScreen()
    {
        var region = Region.FullScreen("full");
        var regions = new[] { region };

        LayoutEngine.ComputeLayout(regions, 80, 24);

        Assert.Equal(0, region.Bounds.X);
        Assert.Equal(0, region.Bounds.Y);
        Assert.Equal(80, region.Bounds.Width);
        Assert.Equal(24, region.Bounds.Height);
    }

    [Fact]
    public void ComputeLayout_TopRow_SpansWidth()
    {
        var region = Region.TopRow("top", 3);
        var regions = new[] { region };

        LayoutEngine.ComputeLayout(regions, 80, 24);

        Assert.Equal(0, region.Bounds.X);
        Assert.Equal(0, region.Bounds.Y);
        Assert.Equal(80, region.Bounds.Width);
        Assert.Equal(3, region.Bounds.Height);
    }

    [Fact]
    public void ComputeLayout_BottomRow_PositionedAtBottom()
    {
        var region = Region.BottomRow("bottom", 1);
        var regions = new[] { region };

        LayoutEngine.ComputeLayout(regions, 80, 24);

        Assert.Equal(0, region.Bounds.X);
        Assert.Equal(23, region.Bounds.Y); // 24 - 0 - 1 = 23
        Assert.Equal(80, region.Bounds.Width);
        Assert.Equal(1, region.Bounds.Height);
    }

    [Fact]
    public void ComputeLayout_PercentWidth_CalculatesCorrectly()
    {
        var region = new Region("test",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Percent(50),
            new LayoutConstraint.Fixed(10));
        var regions = new[] { region };

        LayoutEngine.ComputeLayout(regions, 80, 24);

        Assert.Equal(40, region.Bounds.Width);
    }

    [Fact]
    public void ComputeLayout_FromRight_PositionsFromRightEdge()
    {
        var region = new Region("test",
            new LayoutConstraint.FromRight(5, 20),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(20),
            new LayoutConstraint.Fixed(10));
        var regions = new[] { region };

        LayoutEngine.ComputeLayout(regions, 80, 24);

        Assert.Equal(55, region.Bounds.X); // 80 - 5 - 20 = 55
        Assert.Equal(20, region.Bounds.Width);
    }

    [Fact]
    public void ComputeLayout_ClampsToBounds()
    {
        // Region that would exceed screen
        var region = Region.Fixed("test", 70, 20, 50, 30);
        var regions = new[] { region };

        LayoutEngine.ComputeLayout(regions, 80, 24);

        // Should be clamped
        Assert.True(region.Bounds.Right <= 80);
        Assert.True(region.Bounds.Bottom <= 24);
    }

    [Fact]
    public void ComputeVerticalStack_StacksRegionsVertically()
    {
        var top = Region.Fixed("top", 0, 0, 80, 3);
        var middle = new Region("middle",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0), // Y will be computed
            new LayoutConstraint.Remaining(),
            new LayoutConstraint.Remaining());
        var bottom = Region.Fixed("bottom", 0, 0, 80, 1);

        var regions = new List<Region> { top, middle, bottom };

        LayoutEngine.ComputeVerticalStack(regions, 80, 24);

        Assert.Equal(0, top.Bounds.Y);
        Assert.Equal(3, top.Bounds.Height);

        Assert.Equal(3, middle.Bounds.Y);
        Assert.Equal(20, middle.Bounds.Height); // 24 - 3 - 1 = 20

        Assert.Equal(23, bottom.Bounds.Y);
        Assert.Equal(1, bottom.Bounds.Height);
    }

    [Fact]
    public void Overlaps_OverlappingRegions_ReturnsTrue()
    {
        var a = Region.Fixed("a", 0, 0, 20, 20);
        var b = Region.Fixed("b", 10, 10, 20, 20);

        LayoutEngine.ComputeLayout(new[] { a, b }, 80, 24);

        Assert.True(LayoutEngine.Overlaps(a, b));
    }

    [Fact]
    public void Overlaps_NonOverlappingRegions_ReturnsFalse()
    {
        var a = Region.Fixed("a", 0, 0, 10, 10);
        var b = Region.Fixed("b", 20, 20, 10, 10);

        LayoutEngine.ComputeLayout(new[] { a, b }, 80, 24);

        Assert.False(LayoutEngine.Overlaps(a, b));
    }

    [Fact]
    public void FindRegionAt_ReturnsCorrectRegion()
    {
        var a = Region.Fixed("a", 0, 0, 20, 20);
        var b = Region.Fixed("b", 30, 0, 20, 20);

        LayoutEngine.ComputeLayout(new[] { a, b }, 80, 24);

        Assert.Equal(a, LayoutEngine.FindRegionAt(new[] { a, b }, 10, 10));
        Assert.Equal(b, LayoutEngine.FindRegionAt(new[] { a, b }, 35, 10));
    }

    [Fact]
    public void FindRegionAt_NoMatch_ReturnsNull()
    {
        var a = Region.Fixed("a", 0, 0, 20, 20);

        LayoutEngine.ComputeLayout(new[] { a }, 80, 24);

        Assert.Null(LayoutEngine.FindRegionAt(new[] { a }, 50, 50));
    }

    [Fact]
    public void FindRegionAt_OverlappingRegions_ReturnsLast()
    {
        var a = Region.Fixed("a", 0, 0, 20, 20);
        var b = Region.Fixed("b", 10, 10, 20, 20);

        LayoutEngine.ComputeLayout(new[] { a, b }, 80, 24);

        // At (15, 15) both regions contain the point, should return b (last)
        Assert.Equal(b, LayoutEngine.FindRegionAt(new[] { a, b }, 15, 15));
    }
}
