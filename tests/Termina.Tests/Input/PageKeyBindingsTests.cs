// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;

namespace Termina.Tests.Input;

/// <summary>
/// Tests for the PageKeyBindings class.
/// </summary>
public class PageKeyBindingsTests
{
    [Fact]
    public void PageKeyBindings_Register_AddsBinding()
    {
        var bindings = new PageKeyBindings();

        bindings.Register(ConsoleKey.Escape, () => { });

        Assert.Equal(1, bindings.Count);
    }

    [Fact]
    public void PageKeyBindings_TryHandle_CallsRegisteredHandler()
    {
        var bindings = new PageKeyBindings();
        var called = false;

        bindings.Register(ConsoleKey.Escape, () => called = true);
        var keyInfo = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
        var handled = bindings.TryHandle(keyInfo);

        Assert.True(handled);
        Assert.True(called);
    }

    [Fact]
    public void PageKeyBindings_TryHandle_UnregisteredKey_ReturnsFalse()
    {
        var bindings = new PageKeyBindings();
        var keyInfo = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);

        var handled = bindings.TryHandle(keyInfo);

        Assert.False(handled);
    }

    [Fact]
    public void PageKeyBindings_TryHandle_WithModifiers_MatchesExactly()
    {
        var bindings = new PageKeyBindings();
        var ctrlSCalled = false;
        var sCalled = false;

        bindings.Register(ConsoleKey.S, ConsoleModifiers.Control, () => ctrlSCalled = true);
        bindings.Register(ConsoleKey.S, () => sCalled = true);

        // Press Ctrl+S
        var ctrlS = new ConsoleKeyInfo('s', ConsoleKey.S, false, false, true);
        bindings.TryHandle(ctrlS);

        Assert.True(ctrlSCalled);
        Assert.False(sCalled);
    }

    [Fact]
    public void PageKeyBindings_TryHandle_WithoutModifiers_DoesNotMatchModified()
    {
        var bindings = new PageKeyBindings();
        var sCalled = false;

        bindings.Register(ConsoleKey.S, () => sCalled = true);

        // Press Ctrl+S - should NOT match plain S binding
        var ctrlS = new ConsoleKeyInfo('s', ConsoleKey.S, false, false, true);
        var handled = bindings.TryHandle(ctrlS);

        Assert.False(handled);
        Assert.False(sCalled);
    }

    [Fact]
    public void PageKeyBindings_Unregister_RemovesBinding()
    {
        var bindings = new PageKeyBindings();
        var called = false;

        bindings.Register(ConsoleKey.Escape, () => called = true);
        var removed = bindings.Unregister(ConsoleKey.Escape);

        Assert.True(removed);
        Assert.Equal(0, bindings.Count);

        var keyInfo = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
        var handled = bindings.TryHandle(keyInfo);

        Assert.False(handled);
        Assert.False(called);
    }

    [Fact]
    public void PageKeyBindings_Clear_RemovesAllBindings()
    {
        var bindings = new PageKeyBindings();

        bindings.Register(ConsoleKey.Escape, () => { });
        bindings.Register(ConsoleKey.Tab, () => { });
        bindings.Register(ConsoleKey.Q, () => { });

        Assert.Equal(3, bindings.Count);

        bindings.Clear();

        Assert.Equal(0, bindings.Count);
    }

    [Fact]
    public void PageKeyBindings_Register_OverwritesExistingBinding()
    {
        var bindings = new PageKeyBindings();
        var firstCalled = false;
        var secondCalled = false;

        bindings.Register(ConsoleKey.Escape, () => firstCalled = true);
        bindings.Register(ConsoleKey.Escape, () => secondCalled = true);

        Assert.Equal(1, bindings.Count);

        var keyInfo = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
        bindings.TryHandle(keyInfo);

        Assert.False(firstCalled);
        Assert.True(secondCalled);
    }
}
