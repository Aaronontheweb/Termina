// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Termina.Layout;
using Termina.Terminal;

namespace Termina.Rendering;

/// <summary>
/// Coordinates all rendering through a single channel.
/// Batches and deduplicates render requests for efficient screen updates.
/// </summary>
public sealed class RenderCoordinator : IAsyncDisposable
{
    private readonly IAnsiTerminal _terminal;
    private readonly Channel<RenderCommand> _channel;
    private readonly Dictionary<string, Region> _regions = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _renderLoopTask;

    // Track which regions need rendering in the current batch
    private readonly HashSet<string> _pendingRegions = new();
    private bool _pendingRefreshAll;
    private bool _disposed;

    /// <summary>
    /// Create a new RenderCoordinator.
    /// </summary>
    /// <param name="terminal">The terminal to render to.</param>
    public RenderCoordinator(IAnsiTerminal terminal)
    {
        _terminal = terminal;

        // Unbounded channel - producers never block
        _channel = Channel.CreateUnbounded<RenderCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        });

        // Start the render loop
        _renderLoopTask = RenderLoopAsync(_cts.Token);
    }

    /// <summary>
    /// The terminal being rendered to.
    /// </summary>
    public IAnsiTerminal Terminal => _terminal;

    /// <summary>
    /// Register a region with the coordinator.
    /// </summary>
    /// <param name="region">The region to register.</param>
    public void RegisterRegion(Region region)
    {
        _regions[region.Id] = region;
        region.IsDirty = true;
    }

    /// <summary>
    /// Unregister a region from the coordinator.
    /// </summary>
    /// <param name="regionId">The ID of the region to remove.</param>
    public void UnregisterRegion(string regionId)
    {
        _regions.Remove(regionId);
    }

    /// <summary>
    /// Get a region by ID.
    /// </summary>
    public Region? GetRegion(string regionId)
    {
        return _regions.TryGetValue(regionId, out var region) ? region : null;
    }

    /// <summary>
    /// Get all registered regions.
    /// </summary>
    public IEnumerable<Region> Regions => _regions.Values;

    /// <summary>
    /// Recompute layout for all regions.
    /// Call this after terminal resize.
    /// </summary>
    public void RecalculateLayout()
    {
        LayoutEngine.ComputeLayout(_regions.Values, _terminal.Width, _terminal.Height);

        // Mark all regions dirty after layout change
        foreach (var region in _regions.Values)
        {
            region.IsDirty = true;
        }
    }

    /// <summary>
    /// Queue a render command.
    /// Thread-safe - can be called from any thread.
    /// </summary>
    /// <param name="command">The command to queue.</param>
    public void QueueRender(RenderCommand command)
    {
        if (!_channel.Writer.TryWrite(command))
        {
            // Channel is closed
            throw new ObjectDisposedException(nameof(RenderCoordinator));
        }
    }

    /// <summary>
    /// Request a region to be re-rendered with new content.
    /// Thread-safe - can be called from any thread.
    /// </summary>
    /// <param name="regionId">The region to render.</param>
    /// <param name="content">The content to render.</param>
    public void RenderRegion(string regionId, IRenderable content)
    {
        QueueRender(new RenderCommand.RenderRegion(regionId, content));
    }

    /// <summary>
    /// Request a region to be cleared.
    /// Thread-safe - can be called from any thread.
    /// </summary>
    /// <param name="regionId">The region to clear.</param>
    public void ClearRegion(string regionId)
    {
        QueueRender(new RenderCommand.ClearRegion(regionId));
    }

    /// <summary>
    /// Request cursor visibility change.
    /// </summary>
    public void SetCursorVisible(bool visible)
    {
        QueueRender(new RenderCommand.SetCursorVisible(visible));
    }

    /// <summary>
    /// Request cursor movement.
    /// </summary>
    public void MoveCursor(int x, int y)
    {
        QueueRender(new RenderCommand.MoveCursor(x, y));
    }

    /// <summary>
    /// Request a full screen refresh.
    /// </summary>
    public void RefreshAll()
    {
        QueueRender(new RenderCommand.RefreshAll());
    }

    /// <summary>
    /// The main render loop that processes commands from the channel.
    /// </summary>
    private async Task RenderLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var command in _channel.Reader.ReadAllAsync(ct))
            {
                ProcessCommand(command);

                // Batch: drain any additional pending commands
                while (_channel.Reader.TryRead(out var nextCommand))
                {
                    ProcessCommand(nextCommand);
                }

                // Now perform the actual render
                FlushPendingRenders();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    /// <summary>
    /// Process a single command, accumulating state for batching.
    /// </summary>
    private void ProcessCommand(RenderCommand command)
    {
        switch (command)
        {
            case RenderCommand.RenderRegion renderRegion:
                if (_regions.TryGetValue(renderRegion.RegionId, out var region))
                {
                    region.SetContent(renderRegion.Renderable);
                    _pendingRegions.Add(renderRegion.RegionId);
                }
                break;

            case RenderCommand.ClearRegion clearRegion:
                if (_regions.TryGetValue(clearRegion.RegionId, out var regionToClear))
                {
                    regionToClear.SetContent(null);
                    _pendingRegions.Add(clearRegion.RegionId);
                }
                break;

            case RenderCommand.SetCursorVisible setCursor:
                _terminal.SetCursorVisible(setCursor.Visible);
                break;

            case RenderCommand.MoveCursor moveCursor:
                _terminal.MoveTo(moveCursor.X, moveCursor.Y);
                break;

            case RenderCommand.RefreshAll:
                _pendingRefreshAll = true;
                break;
        }
    }

    /// <summary>
    /// Perform the actual rendering for all pending regions.
    /// </summary>
    private void FlushPendingRenders()
    {
        if (_pendingRefreshAll)
        {
            // Render all regions
            foreach (var region in _regions.Values)
            {
                RenderRegionInternal(region);
            }
            _pendingRefreshAll = false;
            _pendingRegions.Clear();
        }
        else if (_pendingRegions.Count > 0)
        {
            // Render only pending regions
            foreach (var regionId in _pendingRegions)
            {
                if (_regions.TryGetValue(regionId, out var region))
                {
                    RenderRegionInternal(region);
                }
            }
            _pendingRegions.Clear();
        }

        // Flush all buffered output to terminal
        _terminal.Flush();
    }

    /// <summary>
    /// Render a single region to the terminal.
    /// </summary>
    private void RenderRegionInternal(Region region)
    {
        var bounds = region.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var context = new RegionRenderContext(_terminal, bounds.X, bounds.Y, bounds.Width, bounds.Height);

        // Clear the region first
        context.Clear();

        // Render content if present
        region.Content?.Render(context);

        // Reset colors after rendering
        _terminal.ResetColors();

        region.IsDirty = false;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Signal the render loop to stop
        _cts.Cancel();
        _channel.Writer.Complete();

        try
        {
            await _renderLoopTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        _cts.Dispose();
    }
}
