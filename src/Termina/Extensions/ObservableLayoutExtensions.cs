// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Layout;

namespace Termina.Extensions;

/// <summary>
/// Extension methods for binding observables to the layout tree.
/// </summary>
public static class ObservableLayoutExtensions
{
    /// <summary>
    /// Convert an observable of layout nodes into a reactive layout node.
    /// Each emission replaces the rendered content.
    /// </summary>
    /// <param name="source">Observable that emits layout nodes.</param>
    /// <returns>A layout node that updates when the observable emits.</returns>
    public static ReactiveLayoutNode AsLayout(this Observable<ILayoutNode> source)
    {
        return new ReactiveLayoutNode(source);
    }

    /// <summary>
    /// Convert an observable of layout nodes into a reactive layout node, marshaling
    /// observable callbacks through the supplied frame provider.
    /// </summary>
    /// <param name="source">Observable that emits layout nodes.</param>
    /// <param name="frameProvider">Frame provider used to deliver updates.</param>
    /// <returns>A layout node that updates when the observable emits.</returns>
    public static ReactiveLayoutNode AsLayout(
        this Observable<ILayoutNode> source,
        FrameProvider frameProvider)
    {
        ArgumentNullException.ThrowIfNull(frameProvider);
        return new ReactiveLayoutNode(source.ObserveOn(frameProvider));
    }

    /// <summary>
    /// Convert an observable of values into a reactive layout node using a transform function.
    /// </summary>
    /// <typeparam name="T">The type of values in the observable.</typeparam>
    /// <param name="source">Observable that emits values.</param>
    /// <param name="transform">Function to transform values into layout nodes.</param>
    /// <returns>A layout node that updates when the observable emits.</returns>
    public static ReactiveLayoutNode<T> AsLayout<T>(
        this Observable<T> source,
        Func<T, ILayoutNode> transform)
    {
        return new ReactiveLayoutNode<T>(source, transform);
    }

    /// <summary>
    /// Convert an observable of values into a reactive layout node using a transform
    /// function, marshaling observable callbacks through the supplied frame provider.
    /// </summary>
    /// <typeparam name="T">The type of values in the observable.</typeparam>
    /// <param name="source">Observable that emits values.</param>
    /// <param name="transform">Function to transform values into layout nodes.</param>
    /// <param name="frameProvider">Frame provider used to deliver updates.</param>
    /// <returns>A layout node that updates when the observable emits.</returns>
    public static ReactiveLayoutNode<T> AsLayout<T>(
        this Observable<T> source,
        Func<T, ILayoutNode> transform,
        FrameProvider frameProvider)
    {
        ArgumentNullException.ThrowIfNull(frameProvider);
        return new ReactiveLayoutNode<T>(source.ObserveOn(frameProvider), transform);
    }

    /// <summary>
    /// Convert an observable of strings into a reactive text node.
    /// </summary>
    /// <param name="source">Observable that emits strings.</param>
    /// <returns>A layout node that displays the current string.</returns>
    public static ReactiveLayoutNode<string> AsTextLayout(this Observable<string> source)
    {
        return new ReactiveLayoutNode<string>(source, text => new TextNode(text));
    }

    /// <summary>
    /// Convert an observable of strings into a reactive text node, marshaling
    /// observable callbacks through the supplied frame provider.
    /// </summary>
    /// <param name="source">Observable that emits strings.</param>
    /// <param name="frameProvider">Frame provider used to deliver updates.</param>
    /// <returns>A layout node that displays the current string.</returns>
    public static ReactiveLayoutNode<string> AsTextLayout(
        this Observable<string> source,
        FrameProvider frameProvider)
    {
        ArgumentNullException.ThrowIfNull(frameProvider);
        return new ReactiveLayoutNode<string>(source.ObserveOn(frameProvider), text => new TextNode(text));
    }

    /// <summary>
    /// Create a <see cref="DynamicLayoutNode"/> from a factory, subscribing an observable trigger
    /// to call <see cref="DynamicLayoutNode.Invalidate"/> whenever it emits.
    /// </summary>
    /// <param name="factory">Factory that returns the current child node.</param>
    /// <param name="invalidateOn">Observable that triggers re-evaluation of the factory.</param>
    /// <returns>A dynamic layout node that updates when the trigger emits.</returns>
    public static DynamicLayoutNode AsDynamicLayout(
        this Func<ILayoutNode> factory,
        Observable<Unit> invalidateOn)
    {
        var node = new DynamicLayoutNode(factory);
        invalidateOn.Subscribe(_ => node.Invalidate());
        return node;
    }

    /// <summary>
    /// Create a <see cref="DynamicLayoutNode" /> from a factory, subscribing an observable trigger
    /// to call <see cref="DynamicLayoutNode.Invalidate" /> on the supplied frame provider.
    /// </summary>
    /// <param name="factory">Factory that returns the current child node.</param>
    /// <param name="invalidateOn">Observable that triggers re-evaluation of the factory.</param>
    /// <param name="frameProvider">Frame provider used to deliver invalidation callbacks.</param>
    /// <returns>A dynamic layout node that updates when the trigger emits.</returns>
    public static DynamicLayoutNode AsDynamicLayout(
        this Func<ILayoutNode> factory,
        Observable<Unit> invalidateOn,
        FrameProvider frameProvider)
    {
        ArgumentNullException.ThrowIfNull(frameProvider);

        var node = new DynamicLayoutNode(factory);
        invalidateOn.ObserveOn(frameProvider).Subscribe(_ => node.Invalidate());
        return node;
    }
}
