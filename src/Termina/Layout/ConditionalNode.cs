// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Subjects;
using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A layout node that conditionally shows content based on an observable boolean.
/// </summary>
public sealed class ConditionalNode : LayoutNode, IInvalidatingNode
{
    private readonly IObservable<bool> _source;
    private IDisposable? _subscription;
    private readonly Subject<Unit> _invalidated = new();
    private bool _condition;
    private readonly ILayoutNode _thenNode;
    private readonly ILayoutNode _elseNode;
    private bool _isActive = true;

    /// <inheritdoc />
    public IObservable<Unit> Invalidated => _invalidated;

    /// <summary>
    /// Create a conditional node that shows/hides based on an observable condition.
    /// </summary>
    /// <param name="condition">Observable that emits true/false.</param>
    /// <param name="thenNode">Node to show when condition is true.</param>
    /// <param name="elseNode">Node to show when condition is false (optional).</param>
    public ConditionalNode(IObservable<bool> condition, ILayoutNode thenNode, ILayoutNode? elseNode = null)
    {
        _source = condition;
        _thenNode = thenNode;
        _elseNode = elseNode ?? new EmptyNode();

        _subscription = condition.Subscribe(
            onNext: value =>
            {
                if (_condition != value)
                {
                    _condition = value;
                    _invalidated.OnNext(Unit.Default);
                }
            },
            onError: _ => { },
            onCompleted: () => { });
    }

    private ILayoutNode ActiveNode => _condition ? _thenNode : _elseNode;

    /// <inheritdoc />
    internal override IEnumerable<ILayoutNode> GetChildNodes() => [ActiveNode];

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        return ActiveNode.Measure(available);
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        ActiveNode.Render(context, bounds);
    }

    /// <inheritdoc />
    public override void OnActivate()
    {
        _isActive = true;

        // If subscription was disposed during deactivation, recreate it
        if (_subscription == null || _subscription is System.Reactive.Disposables.BooleanDisposable { IsDisposed: true })
        {
            _subscription = _source.Subscribe(
                onNext: value =>
                {
                    if (_condition != value)
                    {
                        _condition = value;
                        _invalidated.OnNext(Unit.Default);
                    }
                },
                onError: _ => { },
                onCompleted: () => { });
        }

        // Activate both branches (both are always kept in memory)
        if (_thenNode is LayoutNode thenLayoutNode)
        {
            thenLayoutNode.OnActivate();
        }
        if (_elseNode is LayoutNode elseLayoutNode)
        {
            elseLayoutNode.OnActivate();
        }

        base.OnActivate();
    }

    /// <inheritdoc />
    public override void OnDeactivate()
    {
        _isActive = false;

        // Deactivate both branches
        if (_thenNode is LayoutNode thenLayoutNode)
        {
            thenLayoutNode.OnDeactivate();
        }
        if (_elseNode is LayoutNode elseLayoutNode)
        {
            elseLayoutNode.OnDeactivate();
        }

        // Dispose subscription to pause updates
        _subscription?.Dispose();
        _subscription = null;

        base.OnDeactivate();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _subscription?.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        _thenNode.Dispose();
        _elseNode.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Factory for conditional rendering.
/// </summary>
public static class When
{
    /// <summary>
    /// Show content when condition is true.
    /// </summary>
    public static ConditionalNode True(IObservable<bool> condition, ILayoutNode content)
    {
        return new ConditionalNode(condition, content);
    }

    /// <summary>
    /// Show content when condition is true, otherwise show else content.
    /// </summary>
    public static ConditionalNode TrueElse(
        IObservable<bool> condition,
        ILayoutNode thenContent,
        ILayoutNode elseContent)
    {
        return new ConditionalNode(condition, thenContent, elseContent);
    }

    /// <summary>
    /// Show content when condition is false.
    /// </summary>
    public static ConditionalNode False(IObservable<bool> condition, ILayoutNode content)
    {
        return new ConditionalNode(
            System.Reactive.Linq.Observable.Select(condition, c => !c),
            content);
    }
}
