// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Layout;
using Termina.Rendering;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for ReactiveLayoutNode invalidation event propagation.
/// These tests diagnose issues with reactive updates not triggering re-renders.
/// </summary>
public class ReactiveLayoutNodeInvalidationTests
{
    [Fact]
    public void ReactiveLayoutNode_BeforeActivation_DoesNotActivateChildrenOnEmit()
    {
        // Arrange
        var subject = new BehaviorSubject<ILayoutNode>(new TextNode("Initial"));
        var reactive = new ReactiveLayoutNode(subject);

        // Create a spy node that tracks OnActivate calls
        var spyNode = new SpyNode();

        // Act - emit BEFORE activating the reactive node
        subject.OnNext(spyNode);

        // Assert - child should NOT be activated yet
        Assert.False(spyNode.WasActivated, "Child should not be activated before ReactiveLayoutNode is activated");
    }

    [Fact]
    public void ReactiveLayoutNode_AfterActivation_ActivatesNewChildren()
    {
        // Arrange
        var subject = new BehaviorSubject<ILayoutNode>(new TextNode("Initial"));
        var reactive = new ReactiveLayoutNode(subject);
        var spyNode = new SpyNode();

        // Act - activate first, then emit
        reactive.OnActivate();
        subject.OnNext(spyNode);

        // Assert - child SHOULD be activated
        Assert.True(spyNode.WasActivated, "Child should be activated after ReactiveLayoutNode is activated");
    }

    [Fact]
    public void ReactiveLayoutNode_EmitsInvalidation_WhenObservableEmits()
    {
        // Arrange
        var subject = new BehaviorSubject<ILayoutNode>(new TextNode("Initial"));
        var reactive = new ReactiveLayoutNode(subject);
        var invalidationCount = 0;

        reactive.Invalidated.Subscribe(_ => invalidationCount++);

        // Act
        subject.OnNext(new TextNode("Updated"));

        // Assert
        Assert.Equal(1, invalidationCount);
    }

    [Fact]
    public void ReactiveLayoutNode_EmitsInvalidation_AfterActivation()
    {
        // Arrange
        var subject = new BehaviorSubject<ILayoutNode>(new TextNode("Initial"));
        var reactive = new ReactiveLayoutNode(subject);
        var invalidationCount = 0;

        reactive.Invalidated.Subscribe(_ => invalidationCount++);
        reactive.OnActivate();

        // Act
        subject.OnNext(new TextNode("Updated"));

        // Assert - should still emit invalidation after activation
        Assert.Equal(1, invalidationCount);
    }

    [Fact]
    public void ReactiveLayoutNode_DoesNotEmitInvalidation_WhenDeactivated()
    {
        // Arrange
        var subject = new BehaviorSubject<ILayoutNode>(new TextNode("Initial"));
        var reactive = new ReactiveLayoutNode(subject);
        var invalidationCount = 0;

        reactive.Invalidated.Subscribe(_ => invalidationCount++);
        reactive.OnActivate();

        // Act - deactivate then emit
        reactive.OnDeactivate();
        subject.OnNext(new TextNode("Updated"));

        // Assert - should NOT emit invalidation when deactivated (subscription disposed)
        Assert.Equal(0, invalidationCount);
    }

    [Fact]
    public void ReactiveLayoutNode_EmitsInvalidation_AfterReactivation()
    {
        // Arrange
        var subject = new BehaviorSubject<ILayoutNode>(new TextNode("Initial"));
        var reactive = new ReactiveLayoutNode(subject);
        var invalidationCount = 0;

        reactive.Invalidated.Subscribe(_ => invalidationCount++);
        reactive.OnActivate();
        reactive.OnDeactivate();

        // Act - reactivate then emit
        reactive.OnActivate(); // BehaviorSubject will emit current value on re-subscription
        subject.OnNext(new TextNode("Updated"));

        // Assert - should emit 2 invalidations: one for re-subscription to BehaviorSubject, one for new value
        Assert.Equal(2, invalidationCount);
    }

    [Fact]
    public void ReactiveLayoutNode_WithModal_ActivatesModalContent()
    {
        // Arrange - simulate TodoListPage modal scenario
        var showModal = new BehaviorSubject<bool>(false);
        var textInput = new TextInputNode().WithPlaceholder("Enter text...");
        var modal = new ModalNode()
            .WithTitle("Test Modal")
            .WithContent(textInput);

        var reactive = new ReactiveLayoutNode(
            showModal.Select(show => show ? (ILayoutNode)modal : new EmptyNode())
        );

        // Track activation
        var spyContent = new SpyNode();
        modal.WithContent(spyContent);

        // Act - activate reactive node, then show modal
        reactive.OnActivate();
        showModal.OnNext(true);

        // Assert - modal content should be activated
        Assert.True(spyContent.WasActivated, "Modal content should be activated when modal is shown");
    }

    [Fact]
    public void ReactiveLayoutNode_WithModal_ForwardsFocusToContent()
    {
        // Arrange
        var showModal = new BehaviorSubject<bool>(false);
        var textInput = new TextInputNode().WithPlaceholder("Enter text...");
        var modal = new ModalNode()
            .WithTitle("Test Modal")
            .WithContent(textInput);

        var reactive = new ReactiveLayoutNode(
            showModal.Select(show => show ? (ILayoutNode)modal : new EmptyNode())
        );

        // Act - activate, show modal, and focus it
        reactive.OnActivate();
        showModal.OnNext(true);
        modal.OnFocused();

        // Assert - text input should have focus
        Assert.True(textInput.HasFocus, "TextInputNode should have focus when modal is focused");
        Assert.True(textInput.IsAnimating, "TextInputNode should be animating when focused");
    }

    /// <summary>
    /// Helper node to track activation lifecycle.
    /// </summary>
    private class SpyNode : LayoutNode
    {
        public bool WasActivated { get; private set; }
        public bool WasDeactivated { get; private set; }

        public override void OnActivate()
        {
            WasActivated = true;
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            WasDeactivated = true;
            base.OnDeactivate();
        }

        public override Size Measure(Size available) => new(0, 0);

        public override void Render(IRenderContext context, Rect bounds)
        {
            // No rendering needed
        }
    }
}
