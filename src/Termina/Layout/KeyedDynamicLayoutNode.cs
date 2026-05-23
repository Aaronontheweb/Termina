// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A dynamic layout node that caches content by key. When the key changes, looks up the cache
/// first — only creates new content on cache miss. Navigating back to a previous key reuses the
/// cached instance, preserving all child state (highlights, typed text, focus).
/// </summary>
/// <typeparam name="TKey">The type of key used to identify content variants.</typeparam>
/// <remarks>
/// This is the preferred API for content that switches based on a key (enum, step index, tab).
/// Use <see cref="Layouts.KeyedDynamic{TKey}"/> to create instances.
/// </remarks>
public sealed class KeyedDynamicLayoutNode<TKey> : LayoutNode, IInvalidatingNode
    where TKey : notnull
{
    private readonly Func<TKey> _keySelector;
    private readonly Func<TKey, ILayoutNode> _contentFactory;
    private readonly Dictionary<TKey, ILayoutNode> _cache = new();
    private readonly Subject<Unit> _invalidated = new();
    private IDisposable? _childInvalidationSubscription;
    private ILayoutNode _currentChild;
    private TKey? _currentKey;
    private bool _isActive;
    private bool _needsEvaluation = true;

    /// <inheritdoc />
    public Observable<Unit> Invalidated => _invalidated;

    /// <summary>
    /// Create a keyed dynamic layout node.
    /// </summary>
    /// <param name="keySelector">Function that returns the current key.</param>
    /// <param name="contentFactory">Function that creates content for a given key (called once per key).</param>
    public KeyedDynamicLayoutNode(Func<TKey> keySelector, Func<TKey, ILayoutNode> contentFactory)
    {
        _keySelector = keySelector;
        _contentFactory = contentFactory;
        _currentChild = new EmptyNode();
    }

    /// <summary>
    /// Signal that the key may have changed.
    /// Eagerly re-evaluates so the new child is immediately available
    /// for tree traversal (e.g., focus propagation).
    /// </summary>
    public void Invalidate()
    {
        _needsEvaluation = true;
        EvaluateFactory();
        _invalidated.OnNext(Unit.Default);
    }

    /// <summary>
    /// Evaluate the key selector, look up or create content, and swap child if changed.
    /// No-op when clean (not invalidated).
    /// </summary>
    private void EvaluateFactory()
    {
        if (!_needsEvaluation)
            return;

        _needsEvaluation = false;
        var key = _keySelector();

        // Look up cache, create on miss
        if (!_cache.TryGetValue(key, out var newChild))
        {
            newChild = _contentFactory(key);
            _cache[key] = newChild;
        }

        _currentKey = key;

        // Same instance — no lifecycle churn needed
        if (ReferenceEquals(newChild, _currentChild))
            return;

        // Deactivate old child
        if (_isActive && _currentChild is LayoutNode oldLayoutNode)
        {
            oldLayoutNode.OnDeactivate();
        }

        _currentChild = newChild;
        SubscribeToChildInvalidation(newChild);

        // Activate new child if we're currently active
        if (_isActive && newChild is LayoutNode newLayoutNode)
        {
            newLayoutNode.OnActivate();
        }
    }

    /// <summary>
    /// Subscribe to a child's invalidation events and propagate them upward.
    /// </summary>
    private void SubscribeToChildInvalidation(ILayoutNode child)
    {
        _childInvalidationSubscription?.Dispose();
        _childInvalidationSubscription = null;

        if (child is IInvalidatingNode invalidating)
        {
            _childInvalidationSubscription = invalidating.Invalidated
                .Subscribe(_ => _invalidated.OnNext(Unit.Default));
        }
    }

    /// <inheritdoc />
    internal override IEnumerable<ILayoutNode> GetChildNodes() => [_currentChild];

    /// <inheritdoc />
    internal override void DisconnectChildInvalidationSubscriptions()
    {
        _childInvalidationSubscription?.Dispose();
        _childInvalidationSubscription = null;
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        EvaluateFactory();

        var childSize = _currentChild.Measure(available);

        var width = WidthConstraint.Compute(available.Width, childSize.Width, available.Width);
        var height = HeightConstraint.Compute(available.Height, childSize.Height, available.Height);

        return new Size(width, height);
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        EvaluateFactory();
        _currentChild.Render(context, bounds);
    }

    /// <inheritdoc />
    public override void OnActivate()
    {
        _isActive = true;
        _needsEvaluation = true;

        // Re-subscribe to current child's invalidation events
        SubscribeToChildInvalidation(_currentChild);

        // Activate current child
        if (_currentChild is LayoutNode childNode)
        {
            childNode.OnActivate();
        }

        base.OnActivate();
    }

    /// <inheritdoc />
    public override void OnDeactivate()
    {
        _isActive = false;

        // Deactivate current child
        if (_currentChild is LayoutNode childNode)
        {
            childNode.OnDeactivate();
        }

        base.OnDeactivate();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _childInvalidationSubscription?.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();

        // Check if _currentChild is in the cache (it won't be if factory was never evaluated)
        var currentChildInCache = _cache.Values.Any(c => ReferenceEquals(c, _currentChild));

        // Dispose ALL cached children
        foreach (var child in _cache.Values)
        {
            child.Dispose();
        }
        _cache.Clear();

        // Dispose the initial EmptyNode if it was never replaced by a cached entry
        if (!currentChildInCache)
        {
            _currentChild.Dispose();
        }

        base.Dispose();
    }
}
