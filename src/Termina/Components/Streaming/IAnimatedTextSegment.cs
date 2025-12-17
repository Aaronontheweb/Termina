// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;

namespace Termina.Components.Streaming;

/// <summary>
/// Represents a text segment that can animate over time (spinner, blink, timer, etc).
/// Extends ITextSegment with animation lifecycle and invalidation events.
/// </summary>
public interface IAnimatedTextSegment : ITextSegment
{
    /// <summary>
    /// Observable that fires when the segment's display text changes and needs re-rendering.
    /// Subscribe to this to trigger redraws when animation frames change.
    /// </summary>
    IObservable<Unit> Invalidated { get; }

    /// <summary>
    /// Start the animation.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop the animation.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets whether the animation is currently running.
    /// </summary>
    bool IsAnimating { get; }
}
