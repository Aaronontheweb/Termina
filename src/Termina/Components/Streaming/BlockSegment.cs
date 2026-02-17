// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;

namespace Termina.Components.Streaming;

/// <summary>
/// A wrapper segment that indicates the content should render as a block element,
/// starting on a new line. The wrapped segment's text will word-wrap to fit
/// the container width automatically.
/// </summary>
/// <remarks>
/// Use the <see cref="TextSegmentExtensions.AsBlock"/> extension method to create
/// block segments fluently:
/// <code>
/// var block = new StaticTextSegment("Long text...").AsBlock();
/// streamingNode.AppendTracked(id, block);
/// </code>
/// </remarks>
public sealed class BlockSegment : ITextSegment, IAnimatedTextSegment
{
    private readonly ITextSegment _inner;

    /// <summary>
    /// Creates a block segment wrapping the specified inner segment.
    /// </summary>
    /// <param name="inner">The segment to wrap as a block element.</param>
    /// <exception cref="ArgumentNullException">Thrown if inner is null.</exception>
    public BlockSegment(ITextSegment inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Gets the wrapped inner segment.
    /// </summary>
    public ITextSegment Inner => _inner;

    /// <inheritdoc />
    public StyledSegment GetCurrentSegment() => _inner.GetCurrentSegment();

    /// <inheritdoc />
    public Observable<Unit> Invalidated =>
        _inner is IAnimatedTextSegment animated
            ? animated.Invalidated
            : Observable.Empty<Unit>();

    /// <inheritdoc />
    public bool IsAnimating =>
        _inner is IAnimatedTextSegment animated && animated.IsAnimating;

    /// <inheritdoc />
    public void Start()
    {
        if (_inner is IAnimatedTextSegment animated)
            animated.Start();
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (_inner is IAnimatedTextSegment animated)
            animated.Stop();
    }

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
