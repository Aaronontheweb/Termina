// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// Base interface for any text segment (static or animated) that can be rendered.
/// All tracked segments must implement this interface to provide their current display content.
/// </summary>
public interface ITextSegment : IDisposable
{
    /// <summary>
    /// Gets the current styled segment to display.
    /// For static segments, this always returns the same content.
    /// For animated segments, this returns different content based on the current animation frame.
    /// </summary>
    StyledSegment GetCurrentSegment();
}
