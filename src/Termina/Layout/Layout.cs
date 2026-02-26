// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Factory for creating layout containers.
/// </summary>
public static class Layouts
{
    /// <summary>
    /// Create a vertical stack layout (children arranged top to bottom).
    /// </summary>
    public static VerticalLayout Vertical(params ILayoutNode[] children) => new(children);

    /// <summary>
    /// Create a horizontal layout (children arranged left to right).
    /// </summary>
    public static HorizontalLayout Horizontal(params ILayoutNode[] children) => new(children);

    /// <summary>
    /// Create a stack layout (children overlapping, last on top).
    /// </summary>
    public static StackLayout Stack(params ILayoutNode[] children) => new(children);

    /// <summary>
    /// Create an empty node (renders nothing, takes no space).
    /// </summary>
    public static EmptyNode Empty() => new();

    /// <summary>
    /// Create a modal overlay component.
    /// </summary>
    /// <remarks>
    /// Use the focus manager to show/hide modals:
    /// <code>
    /// Focus.PushFocus(modal);  // Show modal
    /// Focus.PopFocus();        // Hide modal
    /// </code>
    /// </remarks>
    public static ModalNode Modal() => new();

    /// <summary>
    /// Create a selection list with typed items.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="items">The items to display.</param>
    /// <param name="displaySelector">Function to convert items to display text.</param>
    public static SelectionListNode<T> SelectionList<T>(
        IEnumerable<T> items,
        Func<T, string> displaySelector) => new(items, displaySelector);

    /// <summary>
    /// Create a selection list with string items.
    /// </summary>
    /// <param name="items">The string items to display.</param>
    public static SelectionListNode<string> SelectionList(params string[] items) =>
        new(items, s => s);

    /// <summary>
    /// Create a selection list with string items from an enumerable.
    /// </summary>
    /// <param name="items">The string items to display.</param>
    public static SelectionListNode<string> SelectionList(IEnumerable<string> items) =>
        new(items, s => s);

    /// <summary>
    /// Create a deferred node that delegates to a lazily-obtained node without owning it.
    /// </summary>
    /// <remarks>
    /// Use this when you need to render a node owned elsewhere (like a ViewModel) that should
    /// not be disposed when the layout changes. Useful for modals and overlays that need to
    /// be shown/hidden without being recreated.
    /// </remarks>
    /// <param name="getNode">A function that returns the node to render, or null for no content.</param>
    public static DeferredNode Deferred(Func<ILayoutNode?> getNode) => new(getNode);

    /// <summary>
    /// Create a multi-step wizard component.
    /// </summary>
    /// <typeparam name="TStep">Enum type representing the wizard steps.</typeparam>
    public static WizardNode<TStep> Wizard<TStep>() where TStep : struct, Enum => new();

    /// <summary>
    /// Create a dynamic layout node that re-evaluates a factory on every render cycle.
    /// </summary>
    /// <remarks>
    /// Use this for imperative, page-local state (e.g., switch/case on an enum) where
    /// the state doesn't need to live in a ViewModel as an observable.
    /// The factory is called on each Measure/Render — use reference equality to avoid
    /// unnecessary child lifecycle transitions.
    /// </remarks>
    /// <param name="factory">Factory that returns the current child node.</param>
    public static DynamicLayoutNode Dynamic(Func<ILayoutNode> factory) => new(factory);
}
