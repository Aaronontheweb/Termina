// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A layout node that conditionally shows content based on an observable boolean.
/// </summary>
public sealed class ConditionalNode : LayoutNode, IInvalidatingNode
{
    private readonly IDisposable _subscription;
    private bool _condition;
    private readonly ILayoutNode _thenNode;
    private readonly ILayoutNode _elseNode;

    /// <inheritdoc />
    public event Action? Invalidated;

    /// <summary>
    /// Create a conditional node that shows/hides based on an observable condition.
    /// </summary>
    /// <param name="condition">Observable that emits true/false.</param>
    /// <param name="thenNode">Node to show when condition is true.</param>
    /// <param name="elseNode">Node to show when condition is false (optional).</param>
    public ConditionalNode(IObservable<bool> condition, ILayoutNode thenNode, ILayoutNode? elseNode = null)
    {
        _thenNode = thenNode;
        _elseNode = elseNode ?? new EmptyNode();

        _subscription = condition.Subscribe(
            onNext: value =>
            {
                if (_condition != value)
                {
                    _condition = value;
                    Invalidated?.Invoke();
                }
            },
            onError: _ => { },
            onCompleted: () => { });
    }

    private ILayoutNode ActiveNode => _condition ? _thenNode : _elseNode;

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
    public override void Dispose()
    {
        _subscription.Dispose();
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
