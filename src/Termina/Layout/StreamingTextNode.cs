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
    private readonly Subject<Unit> _contentChanged = new();

    /// <inheritdoc />
    public event Action? Invalidated;

    /// <summary>
    /// Observable that emits when content changes.
    /// </summary>
    public IObservable<Unit> ContentChanged => _contentChanged;

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
    /// </summary>
    public void Append(string text)
    {
        _buffer.Append(text);
        NotifyChanged();
    }

    /// <summary>
    /// Appends a line to the buffer and triggers a redraw.
    /// </summary>
    public void AppendLine(string line)
    {
        _buffer.AppendLine(line);
        NotifyChanged();
    }

    /// <summary>
    /// Clears all content from the buffer.
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
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

    private void NotifyChanged()
    {
        _contentChanged.OnNext(Unit.Default);
        Invalidated?.Invoke();
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

        var visibleLines = _buffer.GetVisibleLines(bounds.Height, contentWidth);

        for (var i = 0; i < bounds.Height && i < visibleLines.Count; i++)
        {
            var line = visibleLines[i];

            // Draw prefix if any
            if (!string.IsNullOrEmpty(Prefix))
            {
                if (PrefixColor.HasValue)
                    context.SetForeground(PrefixColor.Value);
                context.WriteAt(0, i, Prefix);
                context.ResetColors();
            }

            // Draw content
            if (Foreground.HasValue)
                context.SetForeground(Foreground.Value);
            if (Background.HasValue)
                context.SetBackground(Background.Value);

            var truncatedLine = line.Length > contentWidth ? line[..contentWidth] : line;
            context.WriteAt(prefixLen, i, truncatedLine);
            context.ResetColors();
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _contentChanged.OnCompleted();
        _contentChanged.Dispose();
        base.Dispose();
    }
}
