// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Components.Streaming;

/// <summary>
/// Extension methods for <see cref="ITextSegment"/>.
/// </summary>
public static class TextSegmentExtensions
{
    /// <summary>
    /// Wraps this segment as a block element that starts on a new line.
    /// Block segments word-wrap to fit the container width automatically.
    /// </summary>
    /// <param name="segment">The segment to wrap.</param>
    /// <returns>A <see cref="BlockSegment"/> wrapping the original segment.</returns>
    /// <example>
    /// <code>
    /// // Create a block segment that starts on its own line
    /// var block = new StaticTextSegment("Long text that will wrap...").AsBlock();
    /// streamingNode.AppendTracked(id, block);
    ///
    /// // Works with composite segments too
    /// var composite = new CompositeTextSegment(
    ///     new SpinnerSegment(),
    ///     new StaticTextSegment(" Processing...")
    /// ).AsBlock();
    /// </code>
    /// </example>
    public static BlockSegment AsBlock(this ITextSegment segment)
    {
        // If already a BlockSegment, return as-is to avoid double-wrapping
        if (segment is BlockSegment block)
            return block;

        return new BlockSegment(segment);
    }
}
