// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

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
    public static ReactiveLayoutNode AsLayout(this IObservable<ILayoutNode> source)
    {
        return new ReactiveLayoutNode(source);
    }

    /// <summary>
    /// Convert an observable of values into a reactive layout node using a transform function.
    /// </summary>
    /// <typeparam name="T">The type of values in the observable.</typeparam>
    /// <param name="source">Observable that emits values.</param>
    /// <param name="transform">Function to transform values into layout nodes.</param>
    /// <returns>A layout node that updates when the observable emits.</returns>
    public static ReactiveLayoutNode<T> AsLayout<T>(
        this IObservable<T> source,
        Func<T, ILayoutNode> transform)
    {
        return new ReactiveLayoutNode<T>(source, transform);
    }

    /// <summary>
    /// Convert an observable of strings into a reactive text node.
    /// </summary>
    /// <param name="source">Observable that emits strings.</param>
    /// <returns>A layout node that displays the current string.</returns>
    public static ReactiveLayoutNode<string> AsTextLayout(this IObservable<string> source)
    {
        return new ReactiveLayoutNode<string>(source, text => new TextNode(text));
    }
}
