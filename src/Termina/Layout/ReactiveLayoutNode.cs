// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A layout node that updates its content based on an observable stream.
/// When the observable emits, the child node is replaced and invalidation is signaled.
/// </summary>
public sealed class ReactiveLayoutNode : LayoutNode, IInvalidatingNode
{
    private readonly IDisposable _subscription;
    private readonly Subject<Unit> _invalidated = new();
    private ILayoutNode _currentChild;
    private Size _lastMeasuredSize;

    /// <inheritdoc />
    public IObservable<Unit> Invalidated => _invalidated;

    /// <summary>
    /// Create a reactive layout node from an observable of layout nodes.
    /// </summary>
    public ReactiveLayoutNode(IObservable<ILayoutNode> source, ILayoutNode? initialChild = null)
    {
        _currentChild = initialChild ?? new EmptyNode();

        _subscription = source.Subscribe(
            onNext: node =>
            {
                // Dispose old child
                _currentChild.Dispose();
                _currentChild = node;
                _invalidated.OnNext(Unit.Default);
            },
            onError: _ => { },
            onCompleted: () => { });
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        var childSize = _currentChild.Measure(available);

        // Apply our own constraints
        var width = WidthConstraint.Compute(available.Width, childSize.Width, available.Width);
        var height = HeightConstraint.Compute(available.Height, childSize.Height, available.Height);

        _lastMeasuredSize = new Size(width, height);
        return _lastMeasuredSize;
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        _currentChild.Render(context, bounds);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _subscription.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        _currentChild.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// A layout node that updates based on an observable of values, transformed to layout nodes.
/// </summary>
public sealed class ReactiveLayoutNode<T> : LayoutNode, IInvalidatingNode
{
    private readonly Func<T, ILayoutNode> _transform;
    private readonly IDisposable _subscription;
    private readonly Subject<Unit> _invalidated = new();
    private ILayoutNode _currentChild;

    /// <inheritdoc />
    public IObservable<Unit> Invalidated => _invalidated;

    /// <summary>
    /// Create a reactive layout node from an observable with a transform function.
    /// </summary>
    public ReactiveLayoutNode(IObservable<T> source, Func<T, ILayoutNode> transform)
    {
        _transform = transform;
        _currentChild = new EmptyNode();

        _subscription = source.Subscribe(
            onNext: value =>
            {
                _currentChild.Dispose();
                _currentChild = _transform(value);
                _invalidated.OnNext(Unit.Default);
            },
            onError: _ => { },
            onCompleted: () => { });
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        var childSize = _currentChild.Measure(available);

        var width = WidthConstraint.Compute(available.Width, childSize.Width, available.Width);
        var height = HeightConstraint.Compute(available.Height, childSize.Height, available.Height);

        return new Size(width, height);
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        _currentChild.Render(context, bounds);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _subscription.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        _currentChild.Dispose();
        base.Dispose();
    }
}
