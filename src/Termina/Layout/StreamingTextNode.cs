// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Subjects;
using Termina.Components.Streaming;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// A layout node that displays streaming text content with support for scrolling and word wrapping.
/// </summary>
/// <remarks>
/// StreamingTextNode wraps an IStreamingTextBuffer (either PersistedStreamBuffer or WindowedStreamBuffer)
/// and renders the content with automatic word wrapping and optional scrolling.
/// </remarks>
public sealed class StreamingTextNode : LayoutNode, IInvalidatingNode
{
    private readonly IStreamingTextBuffer _buffer;
    private readonly Subject<Unit> _invalidated = new();

    // Tracked segment infrastructure
    private readonly List<ContentElement> _content = new();  // Ordered list of all content
    private readonly Dictionary<SegmentId, int> _segmentIndices = new();  // ID -> index in _content
    private readonly Dictionary<SegmentId, IDisposable> _subscriptions = new();  // Animation subscriptions
    private readonly object _contentLock = new();  // Thread safety for content mutations

    // Content element types for tracking
    private abstract record ContentElement;
    private record StaticElement(StyledSegment Segment) : ContentElement;  // Untracked text
    private record TrackedElement(SegmentId Id, ITextSegment Segment) : ContentElement;  // Tracked segment

    /// <inheritdoc />
    public IObservable<Unit> Invalidated => _invalidated;

    /// <summary>
    /// Observable that emits when content changes. Alias for Invalidated.
    /// </summary>
    public IObservable<Unit> ContentChanged => _invalidated;

    /// <summary>
    /// Gets or sets the foreground color.
    /// </summary>
    public Color? Foreground { get; private set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color? Background { get; private set; }

    /// <summary>
    /// Gets or sets a prefix to add before each line.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets the color for the prefix.
    /// </summary>
    public Color? PrefixColor { get; set; }

    /// <summary>
    /// Gets the underlying buffer.
    /// </summary>
    public IStreamingTextBuffer Buffer => _buffer;

    /// <summary>
    /// Creates a new StreamingTextNode with a persisted buffer (retains all content).
    /// </summary>
    public static StreamingTextNode Create()
    {
        return new StreamingTextNode(new PersistedStreamBuffer());
    }

    /// <summary>
    /// Creates a new StreamingTextNode with a windowed buffer (rolling window).
    /// </summary>
    /// <param name="windowSize">Maximum number of lines to retain.</param>
    public static StreamingTextNode CreateWindowed(int windowSize = 100)
    {
        return new StreamingTextNode(new WindowedStreamBuffer(windowSize));
    }

    /// <summary>
    /// Creates a new StreamingTextNode with a custom buffer.
    /// </summary>
    /// <param name="buffer">The buffer to use.</param>
    public StreamingTextNode(IStreamingTextBuffer buffer)
    {
        _buffer = buffer;
        WidthConstraint = new SizeConstraint.Fill();
        HeightConstraint = new SizeConstraint.Fill();
    }

    /// <summary>
    /// Appends text to the buffer and triggers a redraw.
    /// Untracked - cannot be removed or replaced later.
    /// </summary>
    public void Append(string text)
    {
        lock (_contentLock)
        {
            var segment = new StyledSegment(text, TextStyle.Default);
            _content.Add(new StaticElement(segment));
            _buffer.Append(text);
        }
        NotifyChanged();
    }

    /// <summary>
    /// Appends styled text to the buffer and triggers a redraw.
    /// Untracked - cannot be removed or replaced later.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <param name="foreground">The foreground color (optional).</param>
    /// <param name="background">The background color (optional).</param>
    /// <param name="decoration">Text decorations like bold, italic, etc. (optional).</param>
    public void Append(string text, Color? foreground = null, Color? background = null,
        TextDecoration decoration = TextDecoration.None)
    {
        var style = new TextStyle(
            foreground ?? Color.Default,
            background ?? Color.Default,
            decoration);
        var segment = new StyledSegment(text, style);

        lock (_contentLock)
        {
            _content.Add(new StaticElement(segment));
            _buffer.Append(segment);
        }
        NotifyChanged();
    }

    /// <summary>
    /// Appends a styled segment to the buffer and triggers a redraw.
    /// Untracked - cannot be removed or replaced later.
    /// </summary>
    /// <param name="segment">The styled segment to append.</param>
    public void Append(StyledSegment segment)
    {
        lock (_contentLock)
        {
            _content.Add(new StaticElement(segment));
            _buffer.Append(segment);
        }
        NotifyChanged();
    }

    /// <summary>
    /// Appends a line to the buffer and triggers a redraw.
    /// Untracked - cannot be removed or replaced later.
    /// </summary>
    public void AppendLine(string line)
    {
        lock (_contentLock)
        {
            var segment = new StyledSegment(line + "\n", TextStyle.Default);
            _content.Add(new StaticElement(segment));
            _buffer.AppendLine(line);
        }
        NotifyChanged();
    }

    /// <summary>
    /// Appends a styled line to the buffer and triggers a redraw.
    /// Untracked - cannot be removed or replaced later.
    /// </summary>
    /// <param name="line">The line to append.</param>
    /// <param name="foreground">The foreground color (optional).</param>
    /// <param name="background">The background color (optional).</param>
    /// <param name="decoration">Text decorations like bold, italic, etc. (optional).</param>
    public void AppendLine(string line, Color? foreground = null, Color? background = null,
        TextDecoration decoration = TextDecoration.None)
    {
        var style = new TextStyle(
            foreground ?? Color.Default,
            background ?? Color.Default,
            decoration);
        var segment = new StyledSegment(line + "\n", style);

        lock (_contentLock)
        {
            _content.Add(new StaticElement(segment));
            _buffer.AppendLine(line, style);
        }
        NotifyChanged();
    }

    /// <summary>
    /// Appends a tracked text segment that can be removed or replaced later.
    /// The caller provides the ID to reference this segment.
    /// </summary>
    /// <param name="id">The unique identifier for this segment (provided by caller).</param>
    /// <param name="segment">The text segment to append.</param>
    /// <exception cref="ArgumentException">Thrown if the ID is already in use.</exception>
    public void AppendTracked(SegmentId id, ITextSegment segment)
    {
        lock (_contentLock)
        {
            if (id == SegmentId.None)
                throw new ArgumentException("SegmentId.None cannot be used for tracked segments", nameof(id));

            if (_segmentIndices.ContainsKey(id))
                throw new ArgumentException($"SegmentId {id.Value} is already in use", nameof(id));

            var element = new TrackedElement(id, segment);
            var index = _content.Count;

            _content.Add(element);
            _segmentIndices[id] = index;
            _buffer.Append(segment.GetCurrentSegment());

            // Subscribe to animation invalidation if this is an animated segment
            if (segment is IAnimatedTextSegment animated)
            {
                var subscription = animated.Invalidated.Subscribe(_ =>
                {
                    // On animation frame change, rebuild buffer to show new frame
                    RebuildBuffer();
                    NotifyChanged();
                });
                _subscriptions[id] = subscription;
            }

            NotifyChanged();
        }
    }

    /// <summary>
    /// Removes a tracked segment by ID and rebuilds the buffer.
    /// </summary>
    /// <param name="id">The segment ID to remove.</param>
    /// <returns>True if the segment was found and removed, false otherwise.</returns>
    public bool Remove(SegmentId id)
    {
        lock (_contentLock)
        {
            if (!_segmentIndices.TryGetValue(id, out var index))
                return false;

            // Dispose animation subscription if present
            if (_subscriptions.TryGetValue(id, out var subscription))
            {
                subscription.Dispose();
                _subscriptions.Remove(id);
            }

            // Dispose the segment itself
            if (_content[index] is TrackedElement { Segment: var segment })
            {
                segment.Dispose();
            }

            // Remove from content list
            _content.RemoveAt(index);
            _segmentIndices.Remove(id);

            // Update indices for all segments after this one
            for (var i = index; i < _content.Count; i++)
            {
                if (_content[i] is TrackedElement { Id: var otherId })
                {
                    _segmentIndices[otherId] = i;
                }
            }

            // Rebuild buffer from remaining content
            RebuildBuffer();
            NotifyChanged();
            return true;
        }
    }

    /// <summary>
    /// Replaces a tracked segment with a new segment.
    /// </summary>
    /// <param name="id">The segment ID to replace.</param>
    /// <param name="newSegment">The new segment to replace it with.</param>
    /// <param name="keepTracked">If true, keeps the segment tracked with the same ID. If false, converts to untracked static content.</param>
    /// <returns>True if the segment was found and replaced, false otherwise.</returns>
    public bool Replace(SegmentId id, ITextSegment newSegment, bool keepTracked = true)
    {
        lock (_contentLock)
        {
            if (!_segmentIndices.TryGetValue(id, out var index))
                return false;

            // Dispose old animation subscription if present
            if (_subscriptions.TryGetValue(id, out var oldSubscription))
            {
                oldSubscription.Dispose();
                _subscriptions.Remove(id);
            }

            // Dispose old segment
            if (_content[index] is TrackedElement { Segment: var oldSegment })
            {
                oldSegment.Dispose();
            }

            if (keepTracked)
            {
                // Replace with new tracked segment
                _content[index] = new TrackedElement(id, newSegment);

                // Subscribe to new animation if applicable
                if (newSegment is IAnimatedTextSegment animated)
                {
                    var subscription = animated.Invalidated.Subscribe(_ =>
                    {
                        RebuildBuffer();
                        NotifyChanged();
                    });
                    _subscriptions[id] = subscription;
                }
            }
            else
            {
                // Replace with static element (no longer tracked)
                var staticSegment = newSegment.GetCurrentSegment();
                _content[index] = new StaticElement(staticSegment);
                _segmentIndices.Remove(id);

                // Dispose the new segment since we only needed its current content
                newSegment.Dispose();

                // Update indices for all segments after this one
                for (var i = index + 1; i < _content.Count; i++)
                {
                    if (_content[i] is TrackedElement { Id: var otherId })
                    {
                        _segmentIndices[otherId] = i;
                    }
                }
            }

            // Rebuild buffer with new content
            RebuildBuffer();
            NotifyChanged();
            return true;
        }
    }

    /// <summary>
    /// Rebuilds the buffer from the content list.
    /// Called when tracked segments are added, removed, or replaced.
    /// </summary>
    private void RebuildBuffer()
    {
        _buffer.Clear();

        foreach (var element in _content)
        {
            var segment = element switch
            {
                StaticElement s => s.Segment,
                TrackedElement t => t.Segment.GetCurrentSegment(),
                _ => throw new InvalidOperationException($"Unknown content element type: {element.GetType()}")
            };

            _buffer.Append(segment);
        }
    }

    /// <summary>
    /// Clears all content from the buffer, including tracked segments.
    /// </summary>
    public void Clear()
    {
        lock (_contentLock)
        {
            // Dispose all subscriptions
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            // Dispose all tracked segments
            foreach (var element in _content)
            {
                if (element is TrackedElement { Segment: var segment })
                {
                    segment.Dispose();
                }
            }

            _content.Clear();
            _segmentIndices.Clear();
            _buffer.Clear();
        }
        NotifyChanged();
    }

    /// <summary>
    /// Scrolls up by the specified number of lines (only applies to PersistedStreamBuffer).
    /// </summary>
    public void ScrollUp(int lines = 1, int viewportWidth = 80)
    {
        if (_buffer is PersistedStreamBuffer persisted)
        {
            persisted.ScrollUp(lines, viewportWidth);
            NotifyChanged();
        }
    }

    /// <summary>
    /// Scrolls down by the specified number of lines (only applies to PersistedStreamBuffer).
    /// </summary>
    public void ScrollDown(int lines = 1)
    {
        if (_buffer is PersistedStreamBuffer persisted)
        {
            persisted.ScrollDown(lines);
            NotifyChanged();
        }
    }

    /// <summary>
    /// Scrolls to the bottom (only applies to PersistedStreamBuffer).
    /// </summary>
    public void ScrollToBottom()
    {
        if (_buffer is PersistedStreamBuffer persisted)
        {
            persisted.ScrollToBottom();
            NotifyChanged();
        }
    }

    /// <summary>
    /// Handle keyboard input for scrolling. Returns true if the input was handled.
    /// </summary>
    /// <param name="key">The key info to handle.</param>
    /// <param name="viewportHeight">The height of the visible area (for page scrolling).</param>
    /// <param name="viewportWidth">The width of the visible area (for word wrap calculations).</param>
    public bool HandleInput(ConsoleKeyInfo key, int viewportHeight, int viewportWidth)
    {
        // Only PersistedStreamBuffer supports scrolling
        if (_buffer is not PersistedStreamBuffer)
            return false;

        switch (key.Key)
        {
            case ConsoleKey.PageUp:
                ScrollUp(Math.Max(1, viewportHeight - 1), viewportWidth);
                return true;

            case ConsoleKey.PageDown:
                ScrollDown(Math.Max(1, viewportHeight - 1));
                return true;

            case ConsoleKey.Home when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                // Ctrl+Home scrolls to top
                if (_buffer is PersistedStreamBuffer persisted)
                {
                    persisted.ScrollToTop(viewportWidth);
                    NotifyChanged();
                }
                return true;

            case ConsoleKey.End when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                // Ctrl+End scrolls to bottom
                ScrollToBottom();
                return true;

            default:
                return false;
        }
    }

    private void NotifyChanged()
    {
        _invalidated.OnNext(Unit.Default);
    }

    /// <summary>
    /// Set foreground color.
    /// </summary>
    public StreamingTextNode WithForeground(Color color)
    {
        Foreground = color;
        return this;
    }

    /// <summary>
    /// Set background color.
    /// </summary>
    public StreamingTextNode WithBackground(Color color)
    {
        Background = color;
        return this;
    }

    /// <summary>
    /// Set prefix string.
    /// </summary>
    public StreamingTextNode WithPrefix(string prefix, Color? color = null)
    {
        Prefix = prefix;
        PrefixColor = color;
        return this;
    }

    /// <inheritdoc />
    public override Size Measure(Size available)
    {
        var width = WidthConstraint.Compute(available.Width, available.Width, available.Width);
        var height = HeightConstraint.Compute(available.Height, _buffer.LineCount, available.Height);
        return new Size(width, height);
    }

    /// <inheritdoc />
    public override void Render(IRenderContext context, Rect bounds)
    {
        if (!bounds.HasArea)
            return;

        var prefixLen = Prefix?.Length ?? 0;
        var contentWidth = bounds.Width - prefixLen;
        if (contentWidth <= 0)
            return;

        // Get styled lines from buffer
        var styledLines = _buffer.GetVisibleStyledLines(bounds.Height, contentWidth);

        TextStyle? lastStyle = null;

        for (var i = 0; i < bounds.Height && i < styledLines.Count; i++)
        {
            var styledLine = styledLines[i];

            // Draw prefix if any
            if (!string.IsNullOrEmpty(Prefix))
            {
                context.ResetColors();
                if (PrefixColor.HasValue)
                    context.SetForeground(PrefixColor.Value);
                context.WriteAt(0, i, Prefix);
                context.ResetColors();
                lastStyle = null;
            }

            // Draw styled segments
            var x = prefixLen;
            foreach (var segment in styledLine.Segments)
            {
                var text = segment.Text;
                var availableWidth = contentWidth - (x - prefixLen);

                if (availableWidth <= 0)
                    break;

                if (text.Length > availableWidth)
                    text = text[..availableWidth];

                // Determine effective style (segment style with node-level fallback)
                var effectiveStyle = GetEffectiveStyle(segment.Style);

                // Apply style only if changed (optimization)
                if (lastStyle == null || !lastStyle.Value.Equals(effectiveStyle))
                {
                    context.ResetColors();
                    context.ApplyStyle(effectiveStyle);
                    lastStyle = effectiveStyle;
                }

                context.WriteAt(x, i, text);
                x += text.Length;
            }
        }

        context.ResetColors();
    }

    /// <summary>
    /// Gets the effective style by combining segment style with node-level defaults.
    /// </summary>
    private TextStyle GetEffectiveStyle(TextStyle segmentStyle)
    {
        // Use segment colors if set, otherwise fall back to node-level colors
        var fg = segmentStyle.HasForeground ? segmentStyle.Foreground
            : (Foreground ?? Color.Default);
        var bg = segmentStyle.HasBackground ? segmentStyle.Background
            : (Background ?? Color.Default);

        return new TextStyle(fg, bg, segmentStyle.Decoration);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        lock (_contentLock)
        {
            // Dispose all subscriptions
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            // Dispose all tracked segments
            foreach (var element in _content)
            {
                if (element is TrackedElement { Segment: var segment })
                {
                    segment.Dispose();
                }
            }

            _content.Clear();
            _segmentIndices.Clear();
        }

        _invalidated.OnCompleted();
        _invalidated.Dispose();
        base.Dispose();
    }
}
