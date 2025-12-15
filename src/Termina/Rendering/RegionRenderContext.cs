// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Rendering;

/// <summary>
/// Render context that translates relative coordinates to absolute screen positions.
/// All rendering is clipped to the region bounds.
/// </summary>
public sealed class RegionRenderContext : IRenderContext
{
    private readonly IAnsiTerminal _terminal;
    private readonly int _offsetX;
    private readonly int _offsetY;

    /// <summary>
    /// Create a render context for a specific screen region.
    /// </summary>
    /// <param name="terminal">The terminal to render to.</param>
    /// <param name="offsetX">The X offset (screen column) of the region's top-left corner.</param>
    /// <param name="offsetY">The Y offset (screen row) of the region's top-left corner.</param>
    /// <param name="width">The width of the region.</param>
    /// <param name="height">The height of the region.</param>
    public RegionRenderContext(IAnsiTerminal terminal, int offsetX, int offsetY, int width, int height)
    {
        _terminal = terminal;
        _offsetX = offsetX;
        _offsetY = offsetY;
        Width = width;
        Height = height;
    }

    /// <inheritdoc />
    public int Width { get; }

    /// <inheritdoc />
    public int Height { get; }

    /// <inheritdoc />
    public void WriteAt(int x, int y, string text)
    {
        if (y < 0 || y >= Height || x >= Width)
            return;

        // Clip text to fit within region
        var startX = Math.Max(0, x);
        var skipChars = startX - x;
        var availableWidth = Width - startX;

        if (skipChars >= text.Length || availableWidth <= 0)
            return;

        var clippedText = text.Substring(skipChars);
        if (clippedText.Length > availableWidth)
            clippedText = clippedText.Substring(0, availableWidth);

        _terminal.MoveTo(_offsetX + startX, _offsetY + y);
        _terminal.Write(clippedText);
    }

    /// <inheritdoc />
    public void WriteAt(int x, int y, char c)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        _terminal.MoveTo(_offsetX + x, _offsetY + y);
        _terminal.Write(c);
    }

    /// <inheritdoc />
    public void SetForeground(Color color)
    {
        _terminal.SetForeground(color);
    }

    /// <inheritdoc />
    public void SetBackground(Color color)
    {
        _terminal.SetBackground(color);
    }

    /// <inheritdoc />
    public void ResetColors()
    {
        _terminal.ResetColors();
    }

    /// <inheritdoc />
    public void Fill(int x, int y, int width, int height, char c = ' ')
    {
        // Clip to region bounds
        var startX = Math.Max(0, x);
        var startY = Math.Max(0, y);
        var endX = Math.Min(Width, x + width);
        var endY = Math.Min(Height, y + height);

        if (startX >= endX || startY >= endY)
            return;

        var fillWidth = endX - startX;
        var fillLine = new string(c, fillWidth);

        for (var row = startY; row < endY; row++)
        {
            _terminal.MoveTo(_offsetX + startX, _offsetY + row);
            _terminal.Write(fillLine);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        Fill(0, 0, Width, Height);
    }
}
