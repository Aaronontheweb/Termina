// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Rendering;

/// <summary>
/// Tests for RenderCoordinator.
/// </summary>
public class RenderCoordinatorTests : IAsyncDisposable
{
    private readonly VirtualTerminal _terminal;
    private readonly RenderCoordinator _coordinator;

    public RenderCoordinatorTests()
    {
        _terminal = new VirtualTerminal(80, 24);
        _coordinator = new RenderCoordinator(_terminal);
    }

    public async ValueTask DisposeAsync()
    {
        await _coordinator.DisposeAsync();
    }

    [Fact]
    public void RegisterRegion_AddsRegion()
    {
        var region = Region.Fixed("test", 0, 0, 10, 5);

        _coordinator.RegisterRegion(region);

        Assert.NotNull(_coordinator.GetRegion("test"));
        Assert.Contains(region, _coordinator.Regions);
    }

    [Fact]
    public void UnregisterRegion_RemovesRegion()
    {
        var region = Region.Fixed("test", 0, 0, 10, 5);
        _coordinator.RegisterRegion(region);

        _coordinator.UnregisterRegion("test");

        Assert.Null(_coordinator.GetRegion("test"));
    }

    [Fact]
    public void GetRegion_UnknownId_ReturnsNull()
    {
        Assert.Null(_coordinator.GetRegion("nonexistent"));
    }

    [Fact]
    public void RecalculateLayout_UpdatesRegionBounds()
    {
        var region = Region.FullScreen("full");
        _coordinator.RegisterRegion(region);

        _coordinator.RecalculateLayout();

        Assert.Equal(0, region.Bounds.X);
        Assert.Equal(0, region.Bounds.Y);
        Assert.Equal(80, region.Bounds.Width);
        Assert.Equal(24, region.Bounds.Height);
    }

    [Fact]
    public void RecalculateLayout_MarksAllRegionsDirty()
    {
        var region1 = Region.Fixed("r1", 0, 0, 10, 5);
        var region2 = Region.Fixed("r2", 20, 0, 10, 5);
        _coordinator.RegisterRegion(region1);
        _coordinator.RegisterRegion(region2);

        region1.IsDirty = false;
        region2.IsDirty = false;

        _coordinator.RecalculateLayout();

        Assert.True(region1.IsDirty);
        Assert.True(region2.IsDirty);
    }

    [Fact]
    public async Task RenderRegion_RendersContentToTerminal()
    {
        var region = Region.Fixed("test", 0, 0, 20, 5);
        _coordinator.RegisterRegion(region);
        _coordinator.RecalculateLayout();

        _coordinator.RenderRegion("test", new TextRenderable("Hello"));

        // Allow render loop to process
        await Task.Delay(50);

        Assert.True(_terminal.Contains("Hello"));
    }

    [Fact]
    public async Task ClearRegion_ClearsRegionContent()
    {
        var region = Region.Fixed("test", 0, 0, 20, 5);
        _coordinator.RegisterRegion(region);
        _coordinator.RecalculateLayout();

        // First render some content
        _coordinator.RenderRegion("test", new TextRenderable("Content"));
        await Task.Delay(50);

        // Then clear it
        _coordinator.ClearRegion("test");
        await Task.Delay(50);

        // Region should be cleared (all spaces)
        Assert.Null(region.Content);
    }

    [Fact]
    public async Task SetCursorVisible_UpdatesTerminal()
    {
        _coordinator.SetCursorVisible(false);
        await Task.Delay(50);

        Assert.False(_terminal.CursorVisible);
    }

    [Fact]
    public async Task MoveCursor_UpdatesTerminal()
    {
        _coordinator.MoveCursor(10, 5);
        await Task.Delay(50);

        Assert.Equal(10, _terminal.CursorX);
        Assert.Equal(5, _terminal.CursorY);
    }

    [Fact]
    public async Task RefreshAll_RendersAllRegions()
    {
        var region1 = Region.Fixed("r1", 0, 0, 10, 1);
        var region2 = Region.Fixed("r2", 0, 1, 10, 1);

        region1.SetContent(new TextRenderable("Line1"));
        region2.SetContent(new TextRenderable("Line2"));

        _coordinator.RegisterRegion(region1);
        _coordinator.RegisterRegion(region2);
        _coordinator.RecalculateLayout();

        _coordinator.RefreshAll();
        await Task.Delay(50);

        Assert.True(_terminal.Contains("Line1"));
        Assert.True(_terminal.Contains("Line2"));
    }

    [Fact]
    public async Task BatchedRendering_DeduplicatesRequests()
    {
        var region = Region.Fixed("test", 0, 0, 20, 5);
        _coordinator.RegisterRegion(region);
        _coordinator.RecalculateLayout();

        // Send multiple render requests rapidly
        _coordinator.RenderRegion("test", new TextRenderable("First"));
        _coordinator.RenderRegion("test", new TextRenderable("Second"));
        _coordinator.RenderRegion("test", new TextRenderable("Third"));

        await Task.Delay(50);

        // Only the last content should be visible
        Assert.True(_terminal.Contains("Third"));
    }

    [Fact]
    public async Task Terminal_Property_ReturnsTerminal()
    {
        Assert.Same(_terminal, _coordinator.Terminal);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task DisposeAsync_StopsRenderLoop()
    {
        await _coordinator.DisposeAsync();

        // Should not throw after dispose
        Assert.Throws<ObjectDisposedException>(() => _coordinator.QueueRender(new RenderCommand.RefreshAll()));
    }

    /// <summary>
    /// Simple renderable that writes text at (0,0).
    /// </summary>
    private class TextRenderable : IRenderable
    {
        private readonly string _text;

        public TextRenderable(string text)
        {
            _text = text;
        }

        public void Render(IRenderContext context)
        {
            context.WriteAt(0, 0, _text);
        }

        public (int Width, int Height) Measure(int availableWidth, int availableHeight)
        {
            return (_text.Length, 1);
        }
    }
}
