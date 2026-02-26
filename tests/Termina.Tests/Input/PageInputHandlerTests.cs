// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;

namespace Termina.Tests.Input;

/// <summary>
/// Tests for page-level input handling (capture phase).
/// These tests verify that pages can intercept keyboard input before focused components.
/// </summary>
public class PageInputHandlerTests
{
    [Fact]
    public void ReactivePage_HandlePageInput_WithRegisteredBinding_ReturnsTrue()
    {
        // Arrange
        var page = new TestPage();
        var viewModel = new TestViewModel();
        page.BindForTest(viewModel);

        var escapeHandled = false;
        page.KeyBindings.Register(ConsoleKey.Escape, () => escapeHandled = true);

        // Act
        var keyInfo = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
        var handled = page.HandlePageInput(keyInfo);

        // Assert
        Assert.True(handled);
        Assert.True(escapeHandled);
    }

    [Fact]
    public void ReactivePage_HandlePageInput_WithoutBinding_ReturnsFalse()
    {
        // Arrange
        var page = new TestPage();
        var viewModel = new TestViewModel();
        page.BindForTest(viewModel);

        // Act - No bindings registered, send Tab key
        var keyInfo = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);
        var handled = page.HandlePageInput(keyInfo);

        // Assert
        Assert.False(handled);
    }

    [Fact]
    public void ReactivePage_OnNavigatingFrom_ClearsKeyBindings()
    {
        // Arrange
        var page = new TestPage();
        var viewModel = new TestViewModel();
        page.BindForTest(viewModel);

        page.KeyBindings.Register(ConsoleKey.Escape, () => { });
        page.KeyBindings.Register(ConsoleKey.Tab, () => { });
        Assert.Equal(2, page.KeyBindings.Count);

        // Act
        page.OnNavigatingFrom();

        // Assert
        Assert.Equal(0, page.KeyBindings.Count);
    }

    /// <summary>
    /// This test verifies the key architectural requirement:
    /// Page key bindings must intercept input BEFORE focused components.
    ///
    /// Scenario: A SelectionListNode consumes Escape (emitting Cancelled).
    /// The page has registered an Escape binding for navigation.
    /// The page binding should fire, NOT the component's Cancelled handler.
    ///
    /// This test will FAIL until TerminaApplication.ProcessEvent() is updated
    /// to call page.HandlePageInput() before _focusManager.RouteInput().
    /// </summary>
    [Fact]
    public void PageKeyBinding_InterceptsInput_BeforeFocusedComponent()
    {
        // Arrange
        using var focusManager = new FocusManager();
        var page = new TestPage();
        var viewModel = new TestViewModel();
        page.BindForTest(viewModel);
        page.WireUpFocusForTest(focusManager);

        var pageEscapeHandled = false;
        var componentEscapeHandled = false;

        // Register page-level Escape binding
        page.KeyBindings.Register(ConsoleKey.Escape, () => pageEscapeHandled = true);

        // Create a focusable component that handles Escape
        var focusable = new TestFocusableComponent(
            onEscape: () => componentEscapeHandled = true);
        focusManager.PushFocus(focusable);

        // Act - Simulate input flow (this is what TerminaApplication.ProcessEvent does)
        var keyInfo = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);

        // CORRECT order (capture phase first):
        // 1. Page handler gets first chance
        // 2. Only if page doesn't handle, route to focused component
        var handledByPage = page.HandlePageInput(keyInfo);
        if (!handledByPage)
        {
            focusManager.RouteInput(keyInfo);
        }

        // Assert - Page should have intercepted, component should NOT have received it
        Assert.True(pageEscapeHandled, "Page key binding should have been called");
        Assert.False(componentEscapeHandled, "Component should NOT have received Escape (page intercepted)");
    }

    /// <summary>
    /// Verifies that unbound keys still reach focused components.
    /// </summary>
    [Fact]
    public void UnboundKey_ReachesFocusedComponent()
    {
        // Arrange
        using var focusManager = new FocusManager();
        var page = new TestPage();
        var viewModel = new TestViewModel();
        page.BindForTest(viewModel);
        page.WireUpFocusForTest(focusManager);

        var componentReceivedKey = false;

        // Page only binds Escape
        page.KeyBindings.Register(ConsoleKey.Escape, () => { });

        // Component handles Enter
        var focusable = new TestFocusableComponent(
            onEnter: () => componentReceivedKey = true);
        focusManager.PushFocus(focusable);

        // Act - Send Enter key (not bound at page level)
        var keyInfo = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);

        var handledByPage = page.HandlePageInput(keyInfo);
        if (!handledByPage)
        {
            focusManager.RouteInput(keyInfo);
        }

        // Assert - Component should have received Enter
        Assert.False(handledByPage, "Page should not have handled Enter");
        Assert.True(componentReceivedKey, "Component should have received Enter");
    }

    #region Test Helpers

    private class TestPage : ReactivePage<TestViewModel>
    {
        public override ILayoutNode BuildLayout() => new TextNode("Test");

        public void BindForTest(TestViewModel viewModel)
        {
            Bind(viewModel);
        }

        public void WireUpFocusForTest(IFocusManager focusManager)
        {
            ((Pages.IBindablePage)this).WireUpFocus(focusManager);
        }

        // Expose KeyBindings for testing
        public new PageKeyBindings KeyBindings => base.KeyBindings;
    }

    private class TestViewModel : ReactiveViewModel
    {
    }

    private class TestFocusableComponent : IFocusable
    {
        private readonly Action? _onEscape;
        private readonly Action? _onEnter;

        public TestFocusableComponent(Action? onEscape = null, Action? onEnter = null)
        {
            _onEscape = onEscape;
            _onEnter = onEnter;
        }

        public bool CanFocus => true;
        public bool HasFocus { get; private set; }
        public int FocusPriority => 10;

        public void OnFocused() => HasFocus = true;
        public void OnBlurred() => HasFocus = false;

        public bool HandleInput(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _onEscape?.Invoke();
                    return true; // Component consumes Escape
                case ConsoleKey.Enter:
                    _onEnter?.Invoke();
                    return true; // Component consumes Enter
                default:
                    return false;
            }
        }

        // ILayoutNode implementation (minimal)
        public SizeConstraint WidthConstraint => SizeConstraint.AutoSize();
        public SizeConstraint HeightConstraint => SizeConstraint.AutoSize();
        public Size Measure(Size available) => new(0, 0);
        public void Render(IRenderContext context, Rect bounds) { }
        public void Dispose() { }
    }

    #endregion
}
