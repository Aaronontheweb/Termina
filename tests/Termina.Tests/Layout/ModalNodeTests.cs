// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for the ModalNode component.
/// </summary>
public class ModalNodeTests
{
    [Fact]
    public void ModalNode_CanBeCreated()
    {
        using var modal = new ModalNode();
        Assert.NotNull(modal);
    }

    [Fact]
    public void ModalNode_HasDefaultValues()
    {
        using var modal = new ModalNode();

        Assert.True(modal.CanFocus);
        Assert.False(modal.HasFocus);
        Assert.Equal(100, modal.FocusPriority);
    }

    [Fact]
    public void ModalNode_FluentApi_ReturnsThis()
    {
        using var modal = new ModalNode();

        var result = modal
            .WithTitle("Test")
            .WithTitleColor(Color.White)
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(Color.Cyan)
            .WithBackdrop(BackdropStyle.Dim)
            .WithBackdropColor(Color.BrightBlack)
            .WithBackdropChar('░')
            .WithPosition(ModalPosition.Center)
            .WithPadding(1)
            .WithDismissOnEscape(true);

        Assert.Same(modal, result);
    }

    [Fact]
    public void ModalNode_WithContent_SetsContent()
    {
        using var content = new TextNode("Test content");
        using var modal = new ModalNode().WithContent(content);

        Assert.Same(content, modal.Content);
    }

    [Fact]
    public void ModalNode_OnFocused_SetsHasFocus()
    {
        using var modal = new ModalNode();
        Assert.False(modal.HasFocus);

        modal.OnFocused();

        Assert.True(modal.HasFocus);
    }

    [Fact]
    public void ModalNode_OnBlurred_ClearsHasFocus()
    {
        using var modal = new ModalNode();
        modal.OnFocused();
        Assert.True(modal.HasFocus);

        modal.OnBlurred();

        Assert.False(modal.HasFocus);
    }

    [Fact]
    public void ModalNode_HandleInput_Escape_EmitsDismissed()
    {
        using var modal = new ModalNode().WithDismissOnEscape(true);
        var dismissed = false;
        modal.Dismissed.Subscribe(_ => dismissed = true);

        var escapeKey = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
        var result = modal.HandleInput(escapeKey);

        Assert.True(result);
        Assert.True(dismissed);
    }

    [Fact]
    public void ModalNode_HandleInput_Escape_DismissDisabled_DoesNotEmit()
    {
        using var modal = new ModalNode().WithDismissOnEscape(false);
        var dismissed = false;
        modal.Dismissed.Subscribe(_ => dismissed = true);

        var escapeKey = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
        var result = modal.HandleInput(escapeKey);

        Assert.True(result); // Modal still consumes input
        Assert.False(dismissed);
    }

    [Fact]
    public void ModalNode_HandleInput_OtherKeys_AreConsumed()
    {
        using var modal = new ModalNode();

        var aKey = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var result = modal.HandleInput(aKey);

        Assert.True(result); // Modal consumes all input to prevent it reaching underlying UI
    }

    [Fact]
    public void ModalNode_HandleInput_ForwardsToFocusableContent()
    {
        var focusable = new FocusableTestContent();
        using var modal = new ModalNode().WithContent(focusable);

        var aKey = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        modal.HandleInput(aKey);

        Assert.Equal(aKey, focusable.LastHandledKey);
    }

    [Fact]
    public void ModalNode_Invalidated_EmitsOnPropertyChanges()
    {
        using var modal = new ModalNode();
        var invalidated = false;
        modal.Invalidated.Subscribe(_ => invalidated = true);

        // Setting content triggers invalidation
        modal.Content = new TextNode("Test");

        Assert.True(invalidated);
    }

    [Fact]
    public void ModalNode_Measure_ReturnsFillSize()
    {
        using var modal = new ModalNode();
        var available = new Size(100, 50);

        var size = modal.Measure(available);

        Assert.Equal(available, size);
    }

    [Fact]
    public void ModalNode_Dispose_CompletesObservables()
    {
        var modal = new ModalNode();
        var invalidatedCompleted = false;
        var dismissedCompleted = false;

        modal.Invalidated.Subscribe(
            onNext: _ => { },
            onCompleted: () => invalidatedCompleted = true);
        modal.Dismissed.Subscribe(
            onNext: _ => { },
            onCompleted: () => dismissedCompleted = true);

        modal.Dispose();

        Assert.True(invalidatedCompleted);
        Assert.True(dismissedCompleted);
    }

    [Fact]
    public void ModalNode_WidthConstraint_IsFillRemaining()
    {
        using var modal = new ModalNode();

        Assert.IsType<SizeConstraint.Fill>(modal.WidthConstraint);
    }

    [Fact]
    public void ModalNode_HeightConstraint_IsFillRemaining()
    {
        using var modal = new ModalNode();

        Assert.IsType<SizeConstraint.Fill>(modal.HeightConstraint);
    }

    /// <summary>
    /// Test implementation of IFocusable for testing input forwarding.
    /// </summary>
    private class FocusableTestContent : IFocusable
    {
        public ConsoleKeyInfo? LastHandledKey { get; private set; }

        public SizeConstraint WidthConstraint => SizeConstraint.AutoSize();
        public SizeConstraint HeightConstraint => SizeConstraint.AutoSize();
        public bool CanFocus => true;
        public bool HasFocus => false;
        public int FocusPriority => 1;

        public void OnFocused() { }
        public void OnBlurred() { }

        public bool HandleInput(ConsoleKeyInfo key)
        {
            LastHandledKey = key;
            return true;
        }

        public Size Measure(Size available) => new(10, 1);
        public void Render(IRenderContext context, Rect bounds) { }
        public void Dispose() { }
    }
}
