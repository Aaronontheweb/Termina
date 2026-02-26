// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Input;
using Termina.Layout;
using Termina.Rendering;

namespace Termina.Tests.Input;

/// <summary>
/// Tests for FocusManager.CollectFocusables and CycleFocus functionality.
/// </summary>
public class FocusManagerAutoFocusTests
{
    [Fact]
    public void CollectFocusables_EmptyTree_ReturnsEmpty()
    {
        using var fm = new FocusManager();
        var root = new EmptyNode();

        var result = fm.CollectFocusables(root);

        Assert.Empty(result);
    }

    [Fact]
    public void CollectFocusables_SingleFocusable_ReturnsIt()
    {
        using var fm = new FocusManager();
        var focusable = new TestFocusableNode();
        var root = Layouts.Vertical(focusable);

        var result = fm.CollectFocusables(root);

        Assert.Single(result);
        Assert.Same(focusable, result[0]);
    }

    [Fact]
    public void CollectFocusables_NestedLayout_FindsAll()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var f2 = new TestFocusableNode();
        var f3 = new TestFocusableNode();

        var root = Layouts.Vertical(
            f1,
            Layouts.Horizontal(f2, new TextNode("text")),
            f3
        );

        var result = fm.CollectFocusables(root);

        Assert.Equal(3, result.Count);
        Assert.Same(f1, result[0]);
        Assert.Same(f2, result[1]);
        Assert.Same(f3, result[2]);
    }

    [Fact]
    public void CollectFocusables_FiltersCanFocusFalse()
    {
        using var fm = new FocusManager();
        var focusable = new TestFocusableNode();
        var disabled = new TestFocusableNode { CanFocusValue = false };

        var root = Layouts.Vertical(focusable, disabled);

        var result = fm.CollectFocusables(root);

        Assert.Single(result);
        Assert.Same(focusable, result[0]);
    }

    [Fact]
    public void CollectFocusables_TraversesDynamicLayoutNode()
    {
        using var fm = new FocusManager();
        var focusable = new TestFocusableNode();
        var dynamic = new DynamicLayoutNode(() => focusable);

        // Must evaluate factory first
        dynamic.Measure(new Size(80, 24));

        var root = Layouts.Vertical(dynamic);
        var result = fm.CollectFocusables(root);

        Assert.Single(result);
        Assert.Same(focusable, result[0]);
    }

    [Fact]
    public void CollectFocusables_TraversesReactiveLayoutNode()
    {
        using var fm = new FocusManager();
        var focusable = new TestFocusableNode();
        var reactive = new ReactiveLayoutNode(
            R3.Observable.Return<ILayoutNode>(focusable));

        var root = Layouts.Vertical(reactive);
        var result = fm.CollectFocusables(root);

        Assert.Single(result);
        Assert.Same(focusable, result[0]);
    }

    [Fact]
    public void CycleFocus_Forward_MovesToNext()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var f2 = new TestFocusableNode();
        var f3 = new TestFocusableNode();
        var focusables = new List<IFocusable> { f1, f2, f3 };

        fm.PushFocus(f1);
        fm.CycleFocus(focusables);

        Assert.Same(f2, fm.CurrentFocus);
    }

    [Fact]
    public void CycleFocus_Forward_WrapsAround()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var f2 = new TestFocusableNode();
        var focusables = new List<IFocusable> { f1, f2 };

        fm.PushFocus(f2);
        fm.CycleFocus(focusables);

        Assert.Same(f1, fm.CurrentFocus);
    }

    [Fact]
    public void CycleFocus_Backward_MovesToPrevious()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var f2 = new TestFocusableNode();
        var f3 = new TestFocusableNode();
        var focusables = new List<IFocusable> { f1, f2, f3 };

        fm.PushFocus(f2);
        fm.CycleFocus(focusables, reverse: true);

        Assert.Same(f1, fm.CurrentFocus);
    }

    [Fact]
    public void CycleFocus_Backward_WrapsAround()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var f2 = new TestFocusableNode();
        var focusables = new List<IFocusable> { f1, f2 };

        fm.PushFocus(f1);
        fm.CycleFocus(focusables, reverse: true);

        Assert.Same(f2, fm.CurrentFocus);
    }

    [Fact]
    public void CycleFocus_NoCurrentFocus_FocusesFirst()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var f2 = new TestFocusableNode();
        var focusables = new List<IFocusable> { f1, f2 };

        fm.CycleFocus(focusables);

        Assert.Same(f1, fm.CurrentFocus);
    }

    [Fact]
    public void CycleFocus_NoCurrentFocus_Reverse_FocusesLast()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var f2 = new TestFocusableNode();
        var focusables = new List<IFocusable> { f1, f2 };

        fm.CycleFocus(focusables, reverse: true);

        Assert.Same(f2, fm.CurrentFocus);
    }

    [Fact]
    public void CycleFocus_EmptyList_DoesNothing()
    {
        using var fm = new FocusManager();
        var focusables = new List<IFocusable>();

        // Should not throw
        fm.CycleFocus(focusables);

        Assert.Null(fm.CurrentFocus);
    }

    [Fact]
    public void ReactiveProperty_DistinctUntilChanged_DoesNotEmitDuplicates()
    {
        using var fm = new FocusManager();
        var f1 = new TestFocusableNode();
        var emissions = new List<IFocusable?>();

        fm.FocusChanged.Subscribe(f => emissions.Add(f));

        // Initial null emission from ReactiveProperty on subscribe
        Assert.Single(emissions);

        fm.PushFocus(f1);
        Assert.Equal(2, emissions.Count);

        // ClearFocus sets to null (same as initial), but null → f1 → null should still emit
        fm.ClearFocus();
        Assert.Equal(3, emissions.Count);
    }

    /// <summary>
    /// Test focusable that extends LayoutNode so GetChildNodes() tree walk works.
    /// </summary>
    private class TestFocusableNode : LayoutNode, IFocusable
    {
        public bool CanFocusValue { get; set; } = true;
        public bool HasFocus { get; private set; }
        public bool CanFocus => CanFocusValue;
        public int FocusPriority => 10;

        public void OnFocused() => HasFocus = true;
        public void OnBlurred() => HasFocus = false;
        public bool HandleInput(ConsoleKeyInfo key) => false;

        public override Size Measure(Size available) => new(10, 1);
        public override void Render(IRenderContext context, Rect bounds) { }
    }
}
