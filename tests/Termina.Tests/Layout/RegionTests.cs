// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Rendering;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for Region.
/// </summary>
public class RegionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var x = new LayoutConstraint.Fixed(10);
        var y = new LayoutConstraint.Fixed(5);
        var width = new LayoutConstraint.Fixed(20);
        var height = new LayoutConstraint.Fixed(15);

        var region = new Region("test", x, y, width, height);

        Assert.Equal("test", region.Id);
        Assert.Equal(x, region.X);
        Assert.Equal(y, region.Y);
        Assert.Equal(width, region.Width);
        Assert.Equal(height, region.Height);
    }

    [Fact]
    public void IsDirty_DefaultsToTrue()
    {
        var region = Region.Fixed("test", 0, 0, 10, 10);

        Assert.True(region.IsDirty);
    }

    [Fact]
    public void SetContent_MarksRegionDirty()
    {
        var region = Region.Fixed("test", 0, 0, 10, 10);
        region.IsDirty = false;

        region.SetContent(new TestRenderable());

        Assert.True(region.IsDirty);
        Assert.NotNull(region.Content);
    }

    [Fact]
    public void SetContent_Null_MarksRegionDirty()
    {
        var region = Region.Fixed("test", 0, 0, 10, 10);
        region.SetContent(new TestRenderable());
        region.IsDirty = false;

        region.SetContent(null);

        Assert.True(region.IsDirty);
        Assert.Null(region.Content);
    }

    [Fact]
    public void Invalidate_MarksRegionDirty()
    {
        var region = Region.Fixed("test", 0, 0, 10, 10);
        region.IsDirty = false;

        region.Invalidate();

        Assert.True(region.IsDirty);
    }

    [Fact]
    public void FullScreen_CreatesCorrectConstraints()
    {
        var region = Region.FullScreen("full");

        Assert.IsType<LayoutConstraint.Fixed>(region.X);
        Assert.IsType<LayoutConstraint.Fixed>(region.Y);
        Assert.IsType<LayoutConstraint.Remaining>(region.Width);
        Assert.IsType<LayoutConstraint.Remaining>(region.Height);

        Assert.Equal(0, ((LayoutConstraint.Fixed)region.X).Size);
        Assert.Equal(0, ((LayoutConstraint.Fixed)region.Y).Size);
    }

    [Fact]
    public void Fixed_CreatesCorrectConstraints()
    {
        var region = Region.Fixed("fixed", 10, 20, 30, 40);

        Assert.IsType<LayoutConstraint.Fixed>(region.X);
        Assert.IsType<LayoutConstraint.Fixed>(region.Y);
        Assert.IsType<LayoutConstraint.Fixed>(region.Width);
        Assert.IsType<LayoutConstraint.Fixed>(region.Height);

        Assert.Equal(10, ((LayoutConstraint.Fixed)region.X).Size);
        Assert.Equal(20, ((LayoutConstraint.Fixed)region.Y).Size);
        Assert.Equal(30, ((LayoutConstraint.Fixed)region.Width).Size);
        Assert.Equal(40, ((LayoutConstraint.Fixed)region.Height).Size);
    }

    [Fact]
    public void TopRow_CreatesCorrectConstraints()
    {
        var region = Region.TopRow("top", 3);

        Assert.IsType<LayoutConstraint.Fixed>(region.X);
        Assert.IsType<LayoutConstraint.Fixed>(region.Y);
        Assert.IsType<LayoutConstraint.Remaining>(region.Width);
        Assert.IsType<LayoutConstraint.Fixed>(region.Height);

        Assert.Equal(0, ((LayoutConstraint.Fixed)region.X).Size);
        Assert.Equal(0, ((LayoutConstraint.Fixed)region.Y).Size);
        Assert.Equal(3, ((LayoutConstraint.Fixed)region.Height).Size);
    }

    [Fact]
    public void BottomRow_CreatesCorrectConstraints()
    {
        var region = Region.BottomRow("bottom", 1);

        Assert.IsType<LayoutConstraint.Fixed>(region.X);
        Assert.IsType<LayoutConstraint.FromBottom>(region.Y);
        Assert.IsType<LayoutConstraint.Remaining>(region.Width);
        Assert.IsType<LayoutConstraint.Fixed>(region.Height);

        Assert.Equal(0, ((LayoutConstraint.Fixed)region.X).Size);
        Assert.Equal(1, ((LayoutConstraint.Fixed)region.Height).Size);
    }

    [Fact]
    public void Focusable_CanBeSet()
    {
        var region = new Region("test",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10),
            new LayoutConstraint.Fixed(10)) { Focusable = true };

        Assert.True(region.Focusable);
    }

    [Fact]
    public void TabOrder_CanBeSet()
    {
        var region = new Region("test",
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10),
            new LayoutConstraint.Fixed(10)) { TabOrder = 5 };

        Assert.Equal(5, region.TabOrder);
    }

    private class TestRenderable : IRenderable
    {
        public void Render(IRenderContext context) { }
        public (int Width, int Height) Measure(int availableWidth, int availableHeight) => (1, 1);
    }
}
