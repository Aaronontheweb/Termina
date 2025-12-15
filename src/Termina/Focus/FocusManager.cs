// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;

namespace Termina.Focus;

/// <summary>
/// Manages focus navigation between focusable regions.
/// Supports Tab/Shift+Tab navigation and programmatic focus control.
/// </summary>
public sealed class FocusManager
{
    private readonly List<Region> _focusableRegions = new();
    private Region? _focusedRegion;

    /// <summary>
    /// Event raised when the focused region changes.
    /// Parameters are (old region, new region).
    /// </summary>
    public event Action<Region?, Region?>? OnFocusChanged;

    /// <summary>
    /// Gets the currently focused region, or null if none.
    /// </summary>
    public Region? FocusedRegion => _focusedRegion;

    /// <summary>
    /// Gets whether any region is currently focused.
    /// </summary>
    public bool HasFocus => _focusedRegion != null;

    /// <summary>
    /// Gets all registered focusable regions, ordered by TabOrder.
    /// </summary>
    public IReadOnlyList<Region> FocusableRegions => _focusableRegions.OrderBy(r => r.TabOrder).ToList();

    /// <summary>
    /// Register a region for focus management.
    /// Only focusable regions are added.
    /// </summary>
    public void Register(Region region)
    {
        if (region.Focusable && !_focusableRegions.Contains(region))
        {
            _focusableRegions.Add(region);
        }
    }

    /// <summary>
    /// Unregister a region from focus management.
    /// If this region was focused, focus is cleared.
    /// </summary>
    public void Unregister(Region region)
    {
        _focusableRegions.Remove(region);
        if (_focusedRegion == region)
        {
            ClearFocus();
        }
    }

    /// <summary>
    /// Clear all registered regions.
    /// </summary>
    public void Clear()
    {
        _focusableRegions.Clear();
        if (_focusedRegion != null)
        {
            var old = _focusedRegion;
            _focusedRegion = null;
            OnFocusChanged?.Invoke(old, null);
        }
    }

    /// <summary>
    /// Focus a specific region.
    /// </summary>
    public void Focus(Region region)
    {
        if (!region.Focusable)
            return;

        if (_focusedRegion == region)
            return;

        var old = _focusedRegion;
        _focusedRegion = region;
        OnFocusChanged?.Invoke(old, region);
    }

    /// <summary>
    /// Focus a region by its ID.
    /// </summary>
    /// <returns>True if a matching focusable region was found and focused.</returns>
    public bool FocusById(string regionId)
    {
        var region = _focusableRegions.FirstOrDefault(r => r.Id == regionId);
        if (region != null)
        {
            Focus(region);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Move focus to the next region in tab order.
    /// Wraps around to the first region if at the end.
    /// </summary>
    public void FocusNext()
    {
        if (_focusableRegions.Count == 0)
            return;

        var ordered = _focusableRegions.OrderBy(r => r.TabOrder).ToList();

        if (_focusedRegion == null)
        {
            Focus(ordered[0]);
            return;
        }

        var currentIndex = ordered.IndexOf(_focusedRegion);
        var nextIndex = (currentIndex + 1) % ordered.Count;
        Focus(ordered[nextIndex]);
    }

    /// <summary>
    /// Move focus to the previous region in tab order.
    /// Wraps around to the last region if at the beginning.
    /// </summary>
    public void FocusPrevious()
    {
        if (_focusableRegions.Count == 0)
            return;

        var ordered = _focusableRegions.OrderBy(r => r.TabOrder).ToList();

        if (_focusedRegion == null)
        {
            Focus(ordered[^1]);
            return;
        }

        var currentIndex = ordered.IndexOf(_focusedRegion);
        var prevIndex = currentIndex == 0 ? ordered.Count - 1 : currentIndex - 1;
        Focus(ordered[prevIndex]);
    }

    /// <summary>
    /// Focus the first region (lowest tab order).
    /// </summary>
    public void FocusFirst()
    {
        if (_focusableRegions.Count == 0)
            return;

        var first = _focusableRegions.OrderBy(r => r.TabOrder).First();
        Focus(first);
    }

    /// <summary>
    /// Focus the last region (highest tab order).
    /// </summary>
    public void FocusLast()
    {
        if (_focusableRegions.Count == 0)
            return;

        var last = _focusableRegions.OrderBy(r => r.TabOrder).Last();
        Focus(last);
    }

    /// <summary>
    /// Clear focus (no region focused).
    /// </summary>
    public void ClearFocus()
    {
        if (_focusedRegion == null)
            return;

        var old = _focusedRegion;
        _focusedRegion = null;
        OnFocusChanged?.Invoke(old, null);
    }

    /// <summary>
    /// Check if a specific region is currently focused.
    /// </summary>
    public bool IsFocused(Region region)
    {
        return _focusedRegion == region;
    }

    /// <summary>
    /// Check if a region by ID is currently focused.
    /// </summary>
    public bool IsFocused(string regionId)
    {
        return _focusedRegion?.Id == regionId;
    }
}
