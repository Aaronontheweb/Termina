// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Components.Streaming;

/// <summary>
/// Represents a group of text segments composed together.
/// Enables grouping multiple segments (static or animated) as a single trackable unit.
/// </summary>
/// <remarks>
/// Composition pattern for combining segments. Example: "[🔄 Processing...]" could be:
/// - CompositeSegment([, SpinnerSegment, " Processing..."])
/// </remarks>
public interface ICompositeTextSegment : ITextSegment
{
    /// <summary>
    /// Gets the child segments that make up this composite.
    /// </summary>
    IReadOnlyList<ITextSegment> Children { get; }
}
