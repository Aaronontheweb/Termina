// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// Represents a static (non-animated) text segment that can be tracked for removal/replacement.
/// Wraps a StyledSegment to enable tracking without animation overhead.
/// </summary>
public sealed class StaticTextSegment : ITextSegment
{
    private readonly StyledSegment _segment;

    /// <summary>
    /// Creates a static text segment from a styled segment.
    /// </summary>
    public StaticTextSegment(StyledSegment segment)
    {
        _segment = segment;
    }

    /// <summary>
    /// Creates a static text segment from text and optional style.
    /// </summary>
    public StaticTextSegment(string text, TextStyle? style = null)
    {
        _segment = new StyledSegment(text, style ?? TextStyle.Default);
    }

    /// <summary>
    /// Creates a static text segment with explicit colors.
    /// </summary>
    public StaticTextSegment(string text, Color? foreground = null, Color? background = null,
        TextDecoration decoration = TextDecoration.None)
    {
        var style = new TextStyle(
            foreground ?? Color.Default,
            background ?? Color.Default,
            decoration);
        _segment = new StyledSegment(text, style);
    }

    /// <inheritdoc />
    public StyledSegment GetCurrentSegment() => _segment;

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose for static segments
    }
}
