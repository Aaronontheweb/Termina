// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Rendering;

/// <summary>
/// Commands sent to the RenderCoordinator for batched rendering.
/// </summary>
public abstract record RenderCommand
{
    /// <summary>
    /// Request to render a specific region.
    /// </summary>
    /// <param name="RegionId">The unique identifier for the region to render.</param>
    /// <param name="Renderable">The renderable content to display.</param>
    public sealed record RenderRegion(string RegionId, IRenderable Renderable) : RenderCommand;

    /// <summary>
    /// Request to clear a specific region.
    /// </summary>
    /// <param name="RegionId">The unique identifier for the region to clear.</param>
    public sealed record ClearRegion(string RegionId) : RenderCommand;

    /// <summary>
    /// Request to show or hide the cursor.
    /// </summary>
    /// <param name="Visible">Whether the cursor should be visible.</param>
    public sealed record SetCursorVisible(bool Visible) : RenderCommand;

    /// <summary>
    /// Request to move the cursor to a specific position.
    /// </summary>
    /// <param name="X">The X position (column).</param>
    /// <param name="Y">The Y position (row).</param>
    public sealed record MoveCursor(int X, int Y) : RenderCommand;

    /// <summary>
    /// Request a full screen refresh.
    /// </summary>
    public sealed record RefreshAll : RenderCommand;
}
