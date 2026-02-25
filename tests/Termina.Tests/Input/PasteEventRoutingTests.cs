// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Layout;
using Termina.Rendering;

namespace Termina.Tests.Input;

/// <summary>
/// Tests for the automatic paste event routing via layout tree walk
/// in <see cref="TerminaApplication"/>.
/// </summary>
public class PasteEventRoutingTests
{
    [Fact]
    public void FindPasteReceiver_ReturnsNull_WhenTreeHasNoReceiver()
    {
        // Arrange - a layout tree with no IPasteReceiver
        var root = Layouts.Vertical()
            .WithChild(new TextNode("hello"))
            .WithChild(new TextNode("world"));

        // Act
        var result = FindPasteReceiver(root);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindPasteReceiver_FindsReceiverAtRoot()
    {
        // Arrange - the root itself is an IPasteReceiver
        var input = new TextInputNode();

        // Act
        var result = FindPasteReceiver(input);

        // Assert
        Assert.Same(input, result);

        input.Dispose();
    }

    [Fact]
    public void FindPasteReceiver_FindsReceiverInsideContainer()
    {
        // Arrange - TextInputNode inside a VerticalLayout
        var input = new TextInputNode();
        var root = Layouts.Vertical()
            .WithChild(new TextNode("header"))
            .WithChild(input);

        // Act
        var result = FindPasteReceiver(root);

        // Assert
        Assert.Same(input, result);

        root.Dispose();
    }

    [Fact]
    public void FindPasteReceiver_FindsReceiverInsidePanelNode()
    {
        // Arrange - TextInputNode wrapped in PanelNode inside VerticalLayout
        var input = new TextInputNode();
        var root = Layouts.Vertical()
            .WithChild(new TextNode("header"))
            .WithChild(
                new PanelNode()
                    .WithTitle("Input")
                    .WithContent(input));

        // Act
        var result = FindPasteReceiver(root);

        // Assert
        Assert.Same(input, result);

        root.Dispose();
    }

    [Fact]
    public void FindPasteReceiver_FindsDeeplyNestedReceiver()
    {
        // Arrange - ContainerNode > PanelNode > TextInputNode
        var input = new TextInputNode();
        var root = Layouts.Vertical()
            .WithChild(new TextNode("header"))
            .WithChild(
                Layouts.Horizontal()
                    .WithChild(new TextNode("sidebar"))
                    .WithChild(
                        new PanelNode()
                            .WithTitle("Deep")
                            .WithContent(input)));

        // Act
        var result = FindPasteReceiver(root);

        // Assert
        Assert.Same(input, result);

        root.Dispose();
    }

    [Fact]
    public void FindPasteReceiver_FindsReceiverInsideScrollableContainer()
    {
        // Arrange - TextInputNode inside ScrollableContainerNode
        var input = new TextInputNode();
        var root = Layouts.Vertical()
            .WithChild(
                new ScrollableContainerNode()
                    .WithContent(input));

        // Act
        var result = FindPasteReceiver(root);

        // Assert
        Assert.Same(input, result);

        root.Dispose();
    }

    [Fact]
    public void FindPasteReceiver_ReturnsFirstReceiverFound()
    {
        // Arrange - two TextInputNodes, should find the first one (depth-first)
        var first = new TextInputNode();
        var second = new TextInputNode();
        var root = Layouts.Vertical()
            .WithChild(first)
            .WithChild(second);

        // Act
        var result = FindPasteReceiver(root);

        // Assert
        Assert.Same(first, result);

        root.Dispose();
    }

    [Fact]
    public void FindPasteReceiver_ReturnsNull_ForNullNode()
    {
        var result = FindPasteReceiver(null);
        Assert.Null(result);
    }

    /// <summary>
    /// Uses reflection to call the private static FindPasteReceiver method on TerminaApplication.
    /// </summary>
    private static IPasteReceiver? FindPasteReceiver(ILayoutNode? node)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "FindPasteReceiver",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);
        return (IPasteReceiver?)method!.Invoke(null, [node]);
    }
}
