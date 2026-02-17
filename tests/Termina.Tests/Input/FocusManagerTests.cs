// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;
using Termina.Rendering;
using R3;

namespace Termina.Tests.Input;

/// <summary>
/// Tests for the FocusManager class.
/// </summary>
public class FocusManagerTests
{
    [Fact]
    public void FocusManager_InitialState_HasNoFocus()
    {
        using var focusManager = new FocusManager();

        Assert.Null(focusManager.CurrentFocus);
    }

    [Fact]
    public void FocusManager_PushFocus_SetsFocusToComponent()
    {
        using var focusManager = new FocusManager();
        var focusable = new TestFocusable();

        focusManager.PushFocus(focusable);

        Assert.Same(focusable, focusManager.CurrentFocus);
        Assert.True(focusable.HasFocus);
    }

    [Fact]
    public void FocusManager_PushFocus_CallsOnFocused()
    {
        using var focusManager = new FocusManager();
        var focusable = new TestFocusable();

        focusManager.PushFocus(focusable);

        Assert.True(focusable.OnFocusedCalled);
    }

    [Fact]
    public void FocusManager_PushFocus_BlursPreviousFocus()
    {
        using var focusManager = new FocusManager();
        var first = new TestFocusable();
        var second = new TestFocusable();

        focusManager.PushFocus(first);
        focusManager.PushFocus(second);

        Assert.False(first.HasFocus);
        Assert.True(first.OnBlurredCalled);
        Assert.True(second.HasFocus);
    }

    [Fact]
    public void FocusManager_PopFocus_RestoresPreviousFocus()
    {
        using var focusManager = new FocusManager();
        var first = new TestFocusable();
        var second = new TestFocusable();

        focusManager.PushFocus(first);
        focusManager.PushFocus(second);
        focusManager.PopFocus();

        Assert.Same(first, focusManager.CurrentFocus);
        Assert.True(first.HasFocus);
        Assert.False(second.HasFocus);
    }

    [Fact]
    public void FocusManager_PopFocus_EmptyStack_DoesNotThrow()
    {
        using var focusManager = new FocusManager();

        // Should not throw
        focusManager.PopFocus();

        Assert.Null(focusManager.CurrentFocus);
    }

    [Fact]
    public void FocusManager_SetFocus_ReplacesCurrentFocus()
    {
        using var focusManager = new FocusManager();
        var first = new TestFocusable();
        var second = new TestFocusable();

        focusManager.PushFocus(first);
        focusManager.SetFocus(second);

        Assert.Same(second, focusManager.CurrentFocus);
        Assert.False(first.HasFocus);
        Assert.True(second.HasFocus);
    }

    [Fact]
    public void FocusManager_ClearFocus_RemovesAllFocus()
    {
        using var focusManager = new FocusManager();
        var first = new TestFocusable();
        var second = new TestFocusable();

        focusManager.PushFocus(first);
        focusManager.PushFocus(second);
        focusManager.ClearFocus();

        Assert.Null(focusManager.CurrentFocus);
        Assert.False(first.HasFocus);
        Assert.False(second.HasFocus);
    }

    [Fact]
    public void FocusManager_RouteInput_RoutesToCurrentFocus()
    {
        using var focusManager = new FocusManager();
        var focusable = new TestFocusable { HandleInputResult = true };

        focusManager.PushFocus(focusable);
        var key = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var result = focusManager.RouteInput(key);

        Assert.True(result);
        Assert.Equal(key, focusable.LastKey);
    }

    [Fact]
    public void FocusManager_RouteInput_NoFocus_ReturnsFalse()
    {
        using var focusManager = new FocusManager();
        var key = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);

        var result = focusManager.RouteInput(key);

        Assert.False(result);
    }

    [Fact]
    public void FocusManager_RouteInput_ComponentDoesNotConsume_ReturnsFalse()
    {
        using var focusManager = new FocusManager();
        var focusable = new TestFocusable { HandleInputResult = false };

        focusManager.PushFocus(focusable);
        var key = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var result = focusManager.RouteInput(key);

        Assert.False(result);
    }

    [Fact]
    public void FocusManager_FocusChanged_EmitsFocusChanges()
    {
        using var focusManager = new FocusManager();
        var focusable = new TestFocusable();
        var focusChanges = new List<IFocusable?>();

        focusManager.FocusChanged.Subscribe(f => focusChanges.Add(f));

        focusManager.PushFocus(focusable);
        focusManager.PopFocus();

        Assert.Equal(3, focusChanges.Count); // Initial null + focus + pop
        Assert.Null(focusChanges[0]); // Initial state (BehaviorSubject)
        Assert.Same(focusable, focusChanges[1]);
        Assert.Null(focusChanges[2]);
    }

    [Fact]
    public void FocusManager_PushFocus_CannotFocus_DoesNothing()
    {
        using var focusManager = new FocusManager();
        var focusable = new TestFocusable { CanFocusValue = false };

        focusManager.PushFocus(focusable);

        Assert.Null(focusManager.CurrentFocus);
        Assert.False(focusable.OnFocusedCalled);
    }

    [Fact]
    public void FocusManager_Dispose_ClearsAllFocus()
    {
        var focusManager = new FocusManager();
        var first = new TestFocusable();
        var second = new TestFocusable();

        focusManager.PushFocus(first);
        focusManager.PushFocus(second);
        focusManager.Dispose();

        // After dispose, both should be blurred
        Assert.False(first.HasFocus);
        Assert.False(second.HasFocus);
    }

    /// <summary>
    /// Test implementation of IFocusable for testing focus behavior.
    /// </summary>
    private class TestFocusable : IFocusable
    {
        public bool CanFocusValue { get; set; } = true;
        public bool HasFocus { get; private set; }
        public bool OnFocusedCalled { get; private set; }
        public bool OnBlurredCalled { get; private set; }
        public bool HandleInputResult { get; set; } = true;
        public ConsoleKeyInfo? LastKey { get; private set; }

        public bool CanFocus => CanFocusValue;
        public int FocusPriority => 10;

        public void OnFocused()
        {
            OnFocusedCalled = true;
            HasFocus = true;
        }

        public void OnBlurred()
        {
            OnBlurredCalled = true;
            HasFocus = false;
        }

        public bool HandleInput(ConsoleKeyInfo key)
        {
            LastKey = key;
            return HandleInputResult;
        }

        // ILayoutNode implementation (minimal)
        public SizeConstraint WidthConstraint => SizeConstraint.AutoSize();
        public SizeConstraint HeightConstraint => SizeConstraint.AutoSize();
        public Size Measure(Size available) => new(0, 0);
        public void Render(IRenderContext context, Rect bounds) { }
        public void Dispose() { }
    }
}
