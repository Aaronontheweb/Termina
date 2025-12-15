// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Focus;
using Termina.Layout;

namespace Termina.Tests.Focus;

/// <summary>
/// Tests for the FocusManager.
/// </summary>
public class FocusManagerTests
{
    #region Registration Tests

    [Fact]
    public void Register_AddsToFocusableList()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };

        manager.Register(region);

        Assert.Contains(region, manager.FocusableRegions);
    }

    [Fact]
    public void Register_NonFocusable_NotAdded()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = false };

        manager.Register(region);

        Assert.DoesNotContain(region, manager.FocusableRegions);
    }

    [Fact]
    public void Unregister_RemovesFromList()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };

        manager.Register(region);
        manager.Unregister(region);

        Assert.DoesNotContain(region, manager.FocusableRegions);
    }

    [Fact]
    public void Clear_RemovesAllRegions()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };

        manager.Register(region1);
        manager.Register(region2);
        manager.Clear();

        Assert.Empty(manager.FocusableRegions);
    }

    #endregion

    #region Focus Navigation Tests

    [Fact]
    public void Focus_SetsFocusedRegion()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region);

        manager.Focus(region);

        Assert.Equal(region, manager.FocusedRegion);
    }

    [Fact]
    public void Focus_NonFocusable_DoesNotFocus()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = false };

        manager.Focus(region);

        Assert.Null(manager.FocusedRegion);
    }

    [Fact]
    public void FocusById_FocusesMatchingRegion()
    {
        var manager = new FocusManager();
        var region = new Region("test-id", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region);

        var result = manager.FocusById("test-id");

        Assert.True(result);
        Assert.Equal(region, manager.FocusedRegion);
    }

    [Fact]
    public void FocusById_NoMatch_ReturnsFalse()
    {
        var manager = new FocusManager();

        var result = manager.FocusById("nonexistent");

        Assert.False(result);
        Assert.Null(manager.FocusedRegion);
    }

    [Fact]
    public void FocusNext_CyclesToNextRegion()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 0 };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 1 };
        manager.Register(region1);
        manager.Register(region2);
        manager.Focus(region1);

        manager.FocusNext();

        Assert.Equal(region2, manager.FocusedRegion);
    }

    [Fact]
    public void FocusNext_WrapsAround()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 0 };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 1 };
        manager.Register(region1);
        manager.Register(region2);
        manager.Focus(region2);

        manager.FocusNext();

        Assert.Equal(region1, manager.FocusedRegion);
    }

    [Fact]
    public void FocusPrevious_CyclesToPreviousRegion()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 0 };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 1 };
        manager.Register(region1);
        manager.Register(region2);
        manager.Focus(region2);

        manager.FocusPrevious();

        Assert.Equal(region1, manager.FocusedRegion);
    }

    [Fact]
    public void FocusPrevious_WrapsAround()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 0 };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 1 };
        manager.Register(region1);
        manager.Register(region2);
        manager.Focus(region1);

        manager.FocusPrevious();

        Assert.Equal(region2, manager.FocusedRegion);
    }

    [Fact]
    public void FocusFirst_FocusesLowestTabOrder()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 5 };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 1 };
        manager.Register(region1);
        manager.Register(region2);

        manager.FocusFirst();

        Assert.Equal(region2, manager.FocusedRegion);
    }

    [Fact]
    public void FocusLast_FocusesHighestTabOrder()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 1 };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 5 };
        manager.Register(region1);
        manager.Register(region2);

        manager.FocusLast();

        Assert.Equal(region2, manager.FocusedRegion);
    }

    #endregion

    #region Tab Order Tests

    [Fact]
    public void FocusNext_RespectsTabOrder()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 2 };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 1 };
        var region3 = new Region("r3", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true, TabOrder = 3 };

        // Register out of order
        manager.Register(region3);
        manager.Register(region1);
        manager.Register(region2);

        manager.FocusFirst();
        Assert.Equal(region2, manager.FocusedRegion); // TabOrder 1

        manager.FocusNext();
        Assert.Equal(region1, manager.FocusedRegion); // TabOrder 2

        manager.FocusNext();
        Assert.Equal(region3, manager.FocusedRegion); // TabOrder 3
    }

    #endregion

    #region Event Tests

    [Fact]
    public void Focus_RaisesOnFocusChanged()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region);

        Region? oldRegion = null;
        Region? newRegion = null;
        manager.OnFocusChanged += (o, n) =>
        {
            oldRegion = o;
            newRegion = n;
        };

        manager.Focus(region);

        Assert.Null(oldRegion);
        Assert.Equal(region, newRegion);
    }

    [Fact]
    public void FocusChange_RaisesWithOldAndNew()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region1);
        manager.Register(region2);
        manager.Focus(region1);

        Region? oldRegion = null;
        Region? newRegion = null;
        manager.OnFocusChanged += (o, n) =>
        {
            oldRegion = o;
            newRegion = n;
        };

        manager.Focus(region2);

        Assert.Equal(region1, oldRegion);
        Assert.Equal(region2, newRegion);
    }

    [Fact]
    public void ClearFocus_RemovesFocus()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region);
        manager.Focus(region);

        manager.ClearFocus();

        Assert.Null(manager.FocusedRegion);
    }

    [Fact]
    public void ClearFocus_RaisesEvent()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region);
        manager.Focus(region);

        Region? oldRegion = null;
        Region? newRegion = region; // Should become null
        manager.OnFocusChanged += (o, n) =>
        {
            oldRegion = o;
            newRegion = n;
        };

        manager.ClearFocus();

        Assert.Equal(region, oldRegion);
        Assert.Null(newRegion);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FocusNext_NoRegions_DoesNotThrow()
    {
        var manager = new FocusManager();

        // Should not throw
        manager.FocusNext();

        Assert.Null(manager.FocusedRegion);
    }

    [Fact]
    public void FocusPrevious_NoRegions_DoesNotThrow()
    {
        var manager = new FocusManager();

        // Should not throw
        manager.FocusPrevious();

        Assert.Null(manager.FocusedRegion);
    }

    [Fact]
    public void FocusNext_SingleRegion_StaysFocused()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region);
        manager.Focus(region);

        manager.FocusNext();

        Assert.Equal(region, manager.FocusedRegion);
    }

    [Fact]
    public void HasFocus_ReturnsCorrectValue()
    {
        var manager = new FocusManager();
        var region = new Region("test", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region);

        Assert.False(manager.HasFocus);

        manager.Focus(region);

        Assert.True(manager.HasFocus);
    }

    [Fact]
    public void IsFocused_ChecksSpecificRegion()
    {
        var manager = new FocusManager();
        var region1 = new Region("r1", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        var region2 = new Region("r2", new LayoutConstraint.Fixed(0), new LayoutConstraint.Fixed(0),
            new LayoutConstraint.Fixed(10), new LayoutConstraint.Fixed(5)) { Focusable = true };
        manager.Register(region1);
        manager.Register(region2);
        manager.Focus(region1);

        Assert.True(manager.IsFocused(region1));
        Assert.False(manager.IsFocused(region2));
    }

    #endregion
}
