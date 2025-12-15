// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Computes absolute screen positions for regions based on their constraints.
/// </summary>
public static class LayoutEngine
{
    /// <summary>
    /// Compute the bounds for all regions given the screen dimensions.
    /// </summary>
    /// <param name="regions">The regions to layout.</param>
    /// <param name="screenWidth">The total screen width.</param>
    /// <param name="screenHeight">The total screen height.</param>
    public static void ComputeLayout(IEnumerable<Region> regions, int screenWidth, int screenHeight)
    {
        foreach (var region in regions)
        {
            var bounds = ComputeBounds(region, screenWidth, screenHeight);
            region.Bounds = bounds;
        }
    }

    /// <summary>
    /// Compute the bounds for a single region.
    /// </summary>
    private static ScreenBounds ComputeBounds(Region region, int screenWidth, int screenHeight)
    {
        // Compute X position
        int x;
        if (region.X is LayoutConstraint.FromRight fromRight)
        {
            x = fromRight.ComputePosition(screenWidth);
        }
        else
        {
            x = region.X.Compute(screenWidth, screenWidth);
        }

        // Compute Y position
        int y;
        if (region.Y is LayoutConstraint.FromBottom fromBottom)
        {
            y = fromBottom.ComputePosition(screenHeight);
        }
        else
        {
            y = region.Y.Compute(screenHeight, screenHeight);
        }

        // Compute width
        var remainingWidth = screenWidth - x;
        var width = region.Width.Compute(screenWidth, remainingWidth);

        // Compute height
        var remainingHeight = screenHeight - y;
        var height = region.Height.Compute(screenHeight, remainingHeight);

        // Clamp to screen bounds
        x = Math.Max(0, Math.Min(x, screenWidth - 1));
        y = Math.Max(0, Math.Min(y, screenHeight - 1));
        width = Math.Max(0, Math.Min(width, screenWidth - x));
        height = Math.Max(0, Math.Min(height, screenHeight - y));

        return new ScreenBounds(x, y, width, height);
    }

    /// <summary>
    /// Compute bounds for a set of regions arranged in a vertical stack.
    /// </summary>
    /// <param name="regions">The regions to stack vertically.</param>
    /// <param name="screenWidth">The total screen width.</param>
    /// <param name="screenHeight">The total screen height.</param>
    /// <param name="startY">The starting Y position.</param>
    public static void ComputeVerticalStack(IList<Region> regions, int screenWidth, int screenHeight, int startY = 0)
    {
        var currentY = startY;
        var remainingHeight = screenHeight - startY;

        // First pass: compute non-remaining heights
        var remainingRegions = new List<Region>();
        var totalFixed = 0;

        foreach (var region in regions)
        {
            if (region.Height.NeedsRemaining)
            {
                remainingRegions.Add(region);
            }
            else
            {
                var height = region.Height.Compute(screenHeight, remainingHeight);
                totalFixed += height;
            }
        }

        var spaceForRemaining = Math.Max(0, remainingHeight - totalFixed);
        var remainingPerRegion = remainingRegions.Count > 0 ? spaceForRemaining / remainingRegions.Count : 0;

        // Second pass: assign bounds
        foreach (var region in regions)
        {
            var x = region.X.Compute(screenWidth, screenWidth);
            var width = region.Width.Compute(screenWidth, screenWidth - x);

            int height;
            if (region.Height.NeedsRemaining)
            {
                height = remainingPerRegion;
            }
            else
            {
                height = region.Height.Compute(screenHeight, remainingHeight);
            }

            region.Bounds = new ScreenBounds(x, currentY, width, height);
            currentY += height;
        }
    }

    /// <summary>
    /// Check if two regions overlap.
    /// </summary>
    public static bool Overlaps(Region a, Region b)
        => a.Bounds.Intersects(b.Bounds);

    /// <summary>
    /// Find the region at a specific screen position.
    /// </summary>
    /// <param name="regions">The regions to search.</param>
    /// <param name="x">The screen X position.</param>
    /// <param name="y">The screen Y position.</param>
    /// <returns>The region at the position, or null if none.</returns>
    public static Region? FindRegionAt(IEnumerable<Region> regions, int x, int y)
    {
        // Return the last (topmost) matching region
        Region? result = null;
        foreach (var region in regions)
        {
            if (region.Bounds.Contains(x, y))
            {
                result = region;
            }
        }
        return result;
    }
}
