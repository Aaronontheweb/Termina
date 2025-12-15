// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Rendering;

/// <summary>
/// Context provided to components during rendering.
/// All coordinates are relative to the region's top-left corner (0,0).
/// The context translates these to actual screen coordinates.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// The width available for rendering.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// The height available for rendering.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Write text at the specified position (relative to region).
    /// </summary>
    /// <param name="x">X position (column) relative to region.</param>
    /// <param name="y">Y position (row) relative to region.</param>
    /// <param name="text">The text to write.</param>
    void WriteAt(int x, int y, string text);

    /// <summary>
    /// Write a single character at the specified position (relative to region).
    /// </summary>
    /// <param name="x">X position (column) relative to region.</param>
    /// <param name="y">Y position (row) relative to region.</param>
    /// <param name="c">The character to write.</param>
    void WriteAt(int x, int y, char c);

    /// <summary>
    /// Set the foreground color for subsequent writes.
    /// </summary>
    /// <param name="color">The foreground color.</param>
    void SetForeground(Color color);

    /// <summary>
    /// Set the background color for subsequent writes.
    /// </summary>
    /// <param name="color">The background color.</param>
    void SetBackground(Color color);

    /// <summary>
    /// Reset colors to terminal defaults.
    /// </summary>
    void ResetColors();

    /// <summary>
    /// Fill a rectangular area with a character.
    /// </summary>
    /// <param name="x">Starting X position.</param>
    /// <param name="y">Starting Y position.</param>
    /// <param name="width">Width of the area.</param>
    /// <param name="height">Height of the area.</param>
    /// <param name="c">Character to fill with (default is space).</param>
    void Fill(int x, int y, int width, int height, char c = ' ');

    /// <summary>
    /// Clear the entire region (fill with spaces).
    /// </summary>
    void Clear();
}
