// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Text;
using R3;
using Termina.Clipboard;
using Termina.Components.Streaming;
using Termina.Input;
using Termina.Notifications;
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
public sealed class StreamingTextNode : LayoutNode, IInvalidatingNode, IScrollable, IMouseHandler
{
    private readonly IStreamingTextBuffer _buffer;
    private readonly Subject<Unit> _invalidated = new();

    // Scrollbar
    private ScrollbarOptions? _scrollbarOptions;

    // Cached viewport dimensions, updated during Render() and used by IScrollable
    private int _lastViewportWidth = 80;
    private int _lastViewportHeight = 24;

    // Tracked segment infrastructure
    private readonly List<ContentElement> _content = new();  // Ordered list of all content
    private readonly Dictionary<SegmentId, int> _segmentIndices = new();  // ID -> index in _content
    private readonly Dictionary<SegmentId, IDisposable> _subscriptions = new();  // Animation subscriptions
    private readonly object _contentLock = new();  // Thread safety for content mutations

    // Text selection support
    private TextSelectionState _selectionState = TextSelectionState.None;
    private bool _selectionEnabled = false;
    private IClipboardService? _clipboardService;
    private IToastService? _toastService;
    private bool _mouseTrackingEnabled = false;

    // Content element types for tracking
    private abstract record ContentElement;
    private record StaticElement(StyledSegment Segment) : ContentElement;  // Untracked text
    private record TrackedElement(SegmentId Id, ITextSegment Segment) : ContentElement;  // Tracked segment

    /// <inheritdoc />
    public Observable<Unit> Invalidated => _invalidated;

    /// <summary>
    /// Observable that emits when content changes. Alias for Invalidated.
    /// </summary>
    public Observable<Unit> ContentChanged => _invalidated;

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
    /// Enables text selection with copy support. Requires mouse tracking to be enabled in the terminal.
    /// </summary>
    /// <param name="clipboardService">The clipboard service for copying text.</param>
    /// <param name="toastService">Optional toast service for copy confirmation notifications.</param>
    /// <param name="terminal">Optional terminal instance to enable mouse tracking. If null, mouse tracking must be enabled manually.</param>
    public void EnableSelection(IClipboardService clipboardService, IToastService? toastService = null, IAnsiTerminal? terminal = null)
    {
        _selectionEnabled = true;
        _clipboardService = clipboardService;
        _toastService = toastService;

        // Enable mouse tracking in the terminal if terminal is provided
        if (terminal != null && !_mouseTrackingEnabled)
        {
            terminal.SendRaw("\x1b[?1006h"); // Enable SGR mouse tracking
            _mouseTrackingEnabled = true;
        }
    }

    /// <summary>
    /// Disables text selection and clears any active selection.
    /// </summary>
    public void DisableSelection()
    {
        _selectionEnabled = false;
        _selectionState = TextSelectionState.None;
        
        // Disable mouse tracking if we enabled it
        if (_mouseTrackingEnabled)
        {
            // Note: This would need access to the terminal instance
            // For now, we'll leave mouse tracking enabled to avoid flickering
        }
    }

    /// <summary>
    /// Handles mouse events for text selection.
    /// </summary>
    /// <param name="mouseEvent">The mouse event to handle.</param>
    /// <param name="bounds">The bounds of this node.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    public bool HandleMouseEvent(MouseEvent mouseEvent, Rect bounds)
    {
        if (!_selectionEnabled || mouseEvent.Button != MouseButton.Left)
            return false;

        var prefixLen = Prefix?.Length ?? 0;
        var contentX = prefixLen;
        var contentY = 0;

        // Calculate the character position under the mouse
        var charX = mouseEvent.X - contentX;
        var charY = mouseEvent.Y - contentY;

        // Check if mouse is within content area
        if (charX < 0 || charY < 0)
            return false;

        switch (mouseEvent.EventType)
        {
            case MouseEventType.Press:
                // Start selection
                _selectionState = new TextSelectionState(charY, charX, charY, charX);
                return true;

            case MouseEventType.Drag:
                // Update selection end
                if (_selectionState.IsActive)
                {
                    _selectionState = new TextSelectionState(
                        _selectionState.StartRow,
                        _selectionState.StartCol,
                        charY,
                        charX
                    );
                }
                return true;

            case MouseEventType.Release:
                // Copy selection to clipboard and clear
                if (_selectionState.HasContent)
                {
                    var selectedText = GetSelectedText();
                    if (!string.IsNullOrEmpty(selectedText))
                    {
                        _clipboardService?.Copy(selectedText);
                        _toastService?.Show("Copied to clipboard", new ToastOptions { Duration = TimeSpan.FromSeconds(1) });
                    }
                }
                _selectionState = TextSelectionState.None;
                return true;
        }

        return false;
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
    /// If the segment is a <see cref="BlockSegment"/>, it will start on a new line.
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

            // If this is a block segment, ensure it starts on a new line
            EnsureBlockNewLine(segment);

            // Unwrap BlockSegment to get the inner segment
            var innerSegment = UnwrapBlock(segment);

            var element = new TrackedElement(id, segment);
            var index = _content.Count;

            _content.Add(element);
            _segmentIndices[id] = index;
            _buffer.Append(innerSegment.GetCurrentSegment());

            // Subscribe to animation invalidation if this is an animated segment
            if (innerSegment is IAnimatedTextSegment animated)
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
            switch (element)
            {
                case StaticElement s:
                    _buffer.Append(s.Segment);
                    break;
                case TrackedElement t:
                    // Handle block segments in rebuild
                    EnsureBlockNewLine(t.Segment);
                    var inner = UnwrapBlock(t.Segment);
                    _buffer.Append(inner.GetCurrentSegment());
                    break;
                default:
                    throw new InvalidOperationException($"Unknown content element type: {element.GetType()}");
            }
        }
    }

    /// <summary>
    /// If the segment is a BlockSegment and the current line has content,
    /// ensures we start on a new line.
    /// </summary>
    private void EnsureBlockNewLine(ITextSegment segment)
    {
        if (segment is BlockSegment && _buffer.HasContentOnCurrentLine)
        {
            _buffer.AppendLine(string.Empty);
        }
    }

    /// <summary>
    /// Unwraps a BlockSegment to get the inner segment, or returns the segment as-is.
    /// </summary>
    private static ITextSegment UnwrapBlock(ITextSegment segment)
    {
        return segment is BlockSegment block ? block.Inner : segment;
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

    /// <summary>
    /// Enable the visual scrollbar with default options.
    /// The scrollbar occupies the rightmost column of the node's bounds.
    /// It is hidden automatically when all content fits within the viewport.
    /// </summary>
    public StreamingTextNode WithScrollbar() => WithScrollbar(new ScrollbarOptions());

    /// <summary>
    /// Enable the visual scrollbar with the specified options.
    /// The scrollbar occupies the rightmost column of the node's bounds.
    /// </summary>
    /// <param name="options">Scrollbar appearance and behavior options.</param>
    public StreamingTextNode WithScrollbar(ScrollbarOptions options)
    {
        _scrollbarOptions = options;
        return this;
    }

    /// <inheritdoc cref="IScrollable.CanScrollUp"/>
    /// <remarks>
    /// Depends on cached viewport dimensions updated during <see cref="Render"/>.
    /// Returns <see langword="false"/> before the first render.
    /// </remarks>
    public bool CanScrollUp => _buffer is PersistedStreamBuffer p &&
        p.ScrollOffset < p.GetMaxScrollOffset(_lastViewportWidth);

    /// <inheritdoc cref="IScrollable.CanScrollDown"/>
    /// <remarks>
    /// Depends on cached viewport dimensions updated during <see cref="Render"/>.
    /// Returns <see langword="false"/> before the first render.
    /// </remarks>
    public bool CanScrollDown => _buffer is PersistedStreamBuffer p && p.ScrollOffset > 0;

    void IScrollable.ScrollUp(int lines) => ScrollUp(lines, _lastViewportWidth);

    void IScrollable.ScrollDown(int lines) => ScrollDown(lines);

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

        // Create a sub-context so coordinates are relative to this node's bounds
        var streamContext = context.CreateSubContext(bounds);

        var prefixLen = Prefix?.Length ?? 0;
        var scrollbarWidth = ShouldDrawScrollbar(bounds) ? 1 : 0;
        var contentWidth = bounds.Width - prefixLen - scrollbarWidth;
        if (contentWidth <= 0)
            return;

        // Update cached viewport dimensions for IScrollable
        _lastViewportWidth = contentWidth;
        _lastViewportHeight = bounds.Height;

        // Get styled lines from buffer
        var styledLines = _buffer.GetVisibleStyledLines(bounds.Height, contentWidth);

        TextStyle? lastStyle = null;

        for (var i = 0; i < bounds.Height && i < styledLines.Count; i++)
        {
            var styledLine = styledLines[i];

            // Draw prefix if any
            if (!string.IsNullOrEmpty(Prefix))
            {
                streamContext.ResetColors();
                if (PrefixColor.HasValue)
                    streamContext.SetForeground(PrefixColor.Value);
                streamContext.WriteAt(0, i, Prefix);
                streamContext.ResetColors();
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

                // Determine effective style (segment style with node-level fallback and selection highlighting)
                var effectiveStyle = GetEffectiveStyle(segment.Style, i, x);

                // Apply style only if changed (optimization)
                if (lastStyle == null || !lastStyle.Value.Equals(effectiveStyle))
                {
                    streamContext.ResetColors();
                    streamContext.ApplyStyle(effectiveStyle);
                    lastStyle = effectiveStyle;
                }

                streamContext.WriteAt(x, i, text);
                x += text.Length;
            }
        }

        streamContext.ResetColors();

        if (scrollbarWidth > 0)
            DrawScrollbar(streamContext, bounds, contentWidth);
    }

    private bool ShouldDrawScrollbar(Rect bounds)
    {
        if (_scrollbarOptions == null || _buffer is not PersistedStreamBuffer persisted)
            return false;

        var prefixLen = Prefix?.Length ?? 0;
        var contentWidthWithScrollbar = bounds.Width - prefixLen - 1;
        if (contentWidthWithScrollbar <= 0)
            return false;

        if (!_scrollbarOptions.AutoHide)
            return true;

        // Show scrollbar only when wrapped line count exceeds the visible viewport height
        return persisted.GetWrappedLineCount(contentWidthWithScrollbar) > bounds.Height;
    }

    private void DrawScrollbar(IRenderContext context, Rect bounds, int contentWidth)
    {
        if (_buffer is not PersistedStreamBuffer persisted) return;

        var scrollOffset = persisted.ScrollOffset;
        var maxScroll = persisted.GetMaxScrollOffset(contentWidth);
        if (maxScroll <= 0) return;

        var x = bounds.Width - 1;
        var trackHeight = bounds.Height;
        var totalLines = trackHeight + maxScroll;
        var thumbHeight = Math.Max(1, (int)((float)trackHeight / totalLines * trackHeight));
        var maxThumbTop = trackHeight - thumbHeight;

        // PersistedStreamBuffer uses 0 = bottom convention, so invert the thumb position:
        // scrollOffset=0       → bottom → thumbTop = maxThumbTop
        // scrollOffset=maxScroll → top  → thumbTop = 0
        var thumbTop = maxScroll > 0
            ? (int)((1f - (float)scrollOffset / maxScroll) * maxThumbTop)
            : maxThumbTop;

        var opts = _scrollbarOptions!;
        var trackColor = opts.TrackColor ?? Color.BrightBlack;
        var thumbColor = opts.ThumbColor ?? Color.White;

        for (var y = 0; y < trackHeight; y++)
        {
            var isThumb = y >= thumbTop && y < thumbTop + thumbHeight;
            context.SetForeground(isThumb ? thumbColor : trackColor);
            context.WriteAt(x, y, isThumb ? opts.ThumbChar : opts.TrackChar);
        }

        context.ResetColors();
    }

    /// <summary>
    /// Gets the effective style by combining segment style with node-level defaults.
    /// </summary>
    private TextStyle GetEffectiveStyle(TextStyle segmentStyle, int row, int col)
    {
        // Use segment colors if set, otherwise fall back to node-level colors
        var fg = segmentStyle.HasForeground ? segmentStyle.Foreground
            : (Foreground ?? Color.Default);
        var bg = segmentStyle.HasBackground ? segmentStyle.Background
            : (Background ?? Color.Default);

        // Check if this position is within the selection
        if (_selectionState.IsActive)
        {
            var normalized = _selectionState.Normalized;
            var isSelected = (row > normalized.StartRow 
                || (row == normalized.StartRow && col >= normalized.StartCol))
                && (row < normalized.EndRow 
                    || (row == normalized.EndRow && col <= normalized.EndCol));
            
            if (isSelected)
            {
                // Highlight selected text with reverse colors
                return new TextStyle(bg, fg, segmentStyle.Decoration | TextDecoration.Reverse);
            }
        }

        return new TextStyle(fg, bg, segmentStyle.Decoration);
    }

    /// <summary>
    /// Extracts the selected text from the buffer based on the current selection state.
    /// </summary>
    /// <returns>The selected text, or empty string if no valid selection.</returns>
    private string GetSelectedText()
    {
        if (!_selectionState.HasContent)
            return string.Empty;

        var normalized = _selectionState.Normalized;
        var prefixLen = Prefix?.Length ?? 0;
        
        // Get all visible lines
        var styledLines = _buffer.GetVisibleStyledLines(int.MaxValue, int.MaxValue);
        
        var result = new StringBuilder();
        
        for (var row = normalized.StartRow; row <= normalized.EndRow && row < styledLines.Count; row++)
        {
            var styledLine = styledLines[row];
            var lineText = new StringBuilder();
            
            foreach (var segment in styledLine.Segments)
            {
                lineText.Append(segment.Text);
            }
            
            var fullLine = lineText.ToString();
            
            if (row == normalized.StartRow && row == normalized.EndRow)
            {
                // Single line selection
                var start = Math.Max(0, normalized.StartCol - prefixLen);
                var end = Math.Min(fullLine.Length, normalized.EndCol - prefixLen);
                result.Append(fullLine.Substring(start, end - start));
            }
            else if (row == normalized.StartRow)
            {
                // Start of multi-line selection
                var start = Math.Max(0, normalized.StartCol - prefixLen);
                result.Append(fullLine.Substring(start));
            }
            else if (row == normalized.EndRow)
            {
                // End of multi-line selection
                var end = Math.Min(fullLine.Length, normalized.EndCol - prefixLen);
                result.Append(fullLine.Substring(0, end));
            }
            else
            {
                // Middle lines of multi-line selection
                result.AppendLine(fullLine);
            }
        }
        
        return result.ToString();
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
