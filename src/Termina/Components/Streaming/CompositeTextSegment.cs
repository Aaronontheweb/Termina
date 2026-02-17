// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Text;
using R3;

namespace Termina.Components.Streaming;

/// <summary>
/// A composite text segment that groups multiple child segments together.
/// Useful for combining static and animated segments into a single trackable unit.
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
/// var composite = new CompositeTextSegment(
///     new StaticTextSegment("["),
///     new SpinnerSegment(SpinnerStyle.Dots, Color.Blue),
///     new StaticTextSegment(" Processing...]")
/// );
/// </code>
/// </remarks>
public sealed class CompositeTextSegment : ICompositeTextSegment, IAnimatedTextSegment
{
    private readonly List<ITextSegment> _children;
    private readonly Subject<Unit> _invalidated = new();
    private readonly CompositeDisposable _subscriptions = new();
    private bool _disposed;
    private bool _isAnimating;

    /// <summary>
    /// Creates a composite segment from the provided children.
    /// </summary>
    /// <param name="children">The child segments to compose together.</param>
    public CompositeTextSegment(params ITextSegment[] children) : this((IEnumerable<ITextSegment>)children)
    {
    }

    /// <summary>
    /// Creates a composite segment from the provided children.
    /// </summary>
    /// <param name="children">The child segments to compose together.</param>
    public CompositeTextSegment(IEnumerable<ITextSegment> children)
    {
        _children = children.ToList();

        // Subscribe to any animated children's invalidation events
        foreach (var child in _children)
        {
            if (child is IAnimatedTextSegment animated)
            {
                var subscription = animated.Invalidated.Subscribe(_ =>
                {
                    if (!_disposed)
                    {
                        _invalidated.OnNext(Unit.Default);
                    }
                });
                _subscriptions.Add(subscription);
                _isAnimating = _isAnimating || animated.IsAnimating;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ITextSegment> Children => _children.AsReadOnly();

    /// <inheritdoc />
    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    /// <inheritdoc />
    public bool IsAnimating => _isAnimating && _children.OfType<IAnimatedTextSegment>().Any(a => a.IsAnimating);

    /// <summary>
    /// Gets the combined text of all children as a single segment.
    /// Note: Individual child styles are not preserved in this representation.
    /// To render with proper styles, iterate <see cref="Children"/> and call GetCurrentSegment on each.
    /// </summary>
    public StyledSegment GetCurrentSegment()
    {
        var sb = new StringBuilder();
        foreach (var child in _children)
        {
            sb.Append(child.GetCurrentSegment().Text);
        }
        return new StyledSegment(sb.ToString());
    }

    /// <inheritdoc />
    public void Start()
    {
        foreach (var child in _children.OfType<IAnimatedTextSegment>())
        {
            child.Start();
        }
        _isAnimating = true;
    }

    /// <inheritdoc />
    public void Stop()
    {
        foreach (var child in _children.OfType<IAnimatedTextSegment>())
        {
            child.Stop();
        }
        _isAnimating = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _subscriptions.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();

        foreach (var child in _children)
        {
            child.Dispose();
        }
    }
}
