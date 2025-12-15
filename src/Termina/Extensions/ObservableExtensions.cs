// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Extensions;

/// <summary>
/// Extension methods for binding observables to regions.
/// </summary>
public static class ObservableExtensions
{
    /// <summary>
    /// Subscribe an observable to render to a region.
    /// Each value emitted by the observable will be rendered to the specified region.
    /// </summary>
    /// <typeparam name="T">The type of the observable values (must implement IRenderable).</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="coordinator">The render coordinator.</param>
    /// <param name="regionId">The ID of the region to render to.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    public static IDisposable RenderTo<T>(
        this IObservable<T> source,
        RenderCoordinator coordinator,
        string regionId)
        where T : IRenderable
    {
        return source.Subscribe(
            onNext: renderable => coordinator.RenderRegion(regionId, renderable),
            onError: _ => { },
            onCompleted: () => { });
    }

    /// <summary>
    /// Subscribe an observable to render to a region with a transform function.
    /// Each value emitted by the observable will be transformed and rendered.
    /// </summary>
    /// <typeparam name="T">The type of the observable values.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="coordinator">The render coordinator.</param>
    /// <param name="regionId">The ID of the region to render to.</param>
    /// <param name="transform">Function to transform the value into an IRenderable.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    public static IDisposable RenderTo<T>(
        this IObservable<T> source,
        RenderCoordinator coordinator,
        string regionId,
        Func<T, IRenderable> transform)
    {
        return source.Subscribe(
            onNext: value => coordinator.RenderRegion(regionId, transform(value)),
            onError: _ => { },
            onCompleted: () => { });
    }
}
