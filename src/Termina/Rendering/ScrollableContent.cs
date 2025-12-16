// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Rendering;

/// <summary>
/// A container that provides vertical scrolling for content that exceeds the viewport height.
/// </summary>
public sealed class ScrollableContent : IRenderable
{
    private string[] _lines = Array.Empty<string>();
    private int _viewportHeight;

    /// <summary>
    /// Gets or sets the renderable content.
    /// When set, overrides line-based content.
    /// </summary>
    public IRenderable? Content { get; set; }

    /// <summary>
    /// Gets or sets the current scroll offset (number of lines scrolled from top).
    /// </summary>
    public int ScrollOffset { get; set; }

    /// <summary>
    /// Gets the total height of the content in lines.
    /// </summary>
    public int ContentHeight => Content != null ? _viewportHeight : _lines.Length;

    /// <summary>
    /// Gets or sets whether to show a scrollbar on the right side.
    /// </summary>
    public bool ShowScrollbar { get; set; }

    /// <summary>
    /// Gets or sets the foreground color for the content.
    /// </summary>
    public Color Foreground { get; set; } = Color.Default;

    /// <summary>
    /// Gets or sets the background color for the content.
    /// </summary>
    public Color Background { get; set; } = Color.Default;

    /// <summary>
    /// Gets or sets the color for the scrollbar track.
    /// </summary>
    public Color ScrollbarTrackColor { get; set; } = Color.BrightBlack;

    /// <summary>
    /// Gets or sets the color for the scrollbar thumb.
    /// </summary>
    public Color ScrollbarThumbColor { get; set; } = Color.White;

    /// <summary>
    /// Gets the maximum scroll offset based on content and viewport size.
    /// </summary>
    public int MaxScroll => Math.Max(0, _lines.Length - _viewportHeight);

    /// <summary>
    /// Gets whether scrolling down is possible.
    /// </summary>
    public bool CanScrollDown => ScrollOffset < MaxScroll;

    /// <summary>
    /// Gets whether scrolling up is possible.
    /// </summary>
    public bool CanScrollUp => ScrollOffset > 0;

    /// <summary>
    /// Event raised when the component needs to be re-rendered.
    /// </summary>
    public event Action? OnDirty;

    /// <summary>
    /// Set the content as an array of text lines.
    /// </summary>
    public void SetContent(string[] lines)
    {
        _lines = lines ?? Array.Empty<string>();
        Content = null;
        // Clamp scroll offset to valid range
        ScrollOffset = Math.Min(ScrollOffset, MaxScroll);
        MarkDirty();
    }

    /// <summary>
    /// Set the viewport height for scroll calculations.
    /// </summary>
    public void SetViewportHeight(int height)
    {
        _viewportHeight = Math.Max(1, height);
        // Clamp scroll offset to valid range
        ScrollOffset = Math.Min(ScrollOffset, MaxScroll);
    }

    /// <summary>
    /// Scroll down by one line.
    /// </summary>
    public void ScrollDown()
    {
        if (CanScrollDown)
        {
            ScrollOffset++;
            MarkDirty();
        }
    }

    /// <summary>
    /// Scroll up by one line.
    /// </summary>
    public void ScrollUp()
    {
        if (CanScrollUp)
        {
            ScrollOffset--;
            MarkDirty();
        }
    }

    /// <summary>
    /// Scroll down by a full page.
    /// </summary>
    public void PageDown()
    {
        var newOffset = Math.Min(ScrollOffset + _viewportHeight, MaxScroll);
        if (newOffset != ScrollOffset)
        {
            ScrollOffset = newOffset;
            MarkDirty();
        }
    }

    /// <summary>
    /// Scroll up by a full page.
    /// </summary>
    public void PageUp()
    {
        var newOffset = Math.Max(ScrollOffset - _viewportHeight, 0);
        if (newOffset != ScrollOffset)
        {
            ScrollOffset = newOffset;
            MarkDirty();
        }
    }

    /// <summary>
    /// Scroll to a specific offset.
    /// </summary>
    public void ScrollTo(int offset)
    {
        var newOffset = Math.Max(0, Math.Min(offset, MaxScroll));
        if (newOffset != ScrollOffset)
        {
            ScrollOffset = newOffset;
            MarkDirty();
        }
    }

    /// <summary>
    /// Scroll to the top of the content.
    /// </summary>
    public void ScrollToTop()
    {
        ScrollTo(0);
    }

    /// <summary>
    /// Scroll to the bottom of the content.
    /// </summary>
    public void ScrollToBottom()
    {
        ScrollTo(MaxScroll);
    }

    /// <inheritdoc />
    public void Render(IRenderContext context)
    {
        // Update viewport height from context
        _viewportHeight = context.Height;

        var contentWidth = ShowScrollbar && context.Width > 1 ? context.Width - 1 : context.Width;

        context.SetForeground(Foreground);
        context.SetBackground(Background);

        if (Content != null)
        {
            // Render the IRenderable content
            var contentContext = new ScrolledRenderContext(context, 0, -ScrollOffset, contentWidth, context.Height + ScrollOffset);
            Content.Render(contentContext);
        }
        else
        {
            // Render line-based content
            for (var y = 0; y < context.Height && y + ScrollOffset < _lines.Length; y++)
            {
                var lineIndex = y + ScrollOffset;
                var line = _lines[lineIndex];

                // Truncate if needed
                if (line.Length > contentWidth)
                    line = line[..contentWidth];

                context.WriteAt(0, y, line);
            }
        }

        context.ResetColors();

        // Draw scrollbar if enabled and needed
        if (ShowScrollbar && _lines.Length > _viewportHeight)
        {
            DrawScrollbar(context);
        }
    }

    /// <inheritdoc />
    public (int Width, int Height) Measure(int availableWidth, int availableHeight)
    {
        if (Content != null)
        {
            var (w, h) = Content.Measure(availableWidth, availableHeight);
            if (ShowScrollbar)
                w = Math.Min(w + 1, availableWidth);
            return (w, h);
        }

        var maxWidth = 0;
        foreach (var line in _lines)
        {
            if (line.Length > maxWidth)
                maxWidth = line.Length;
        }

        if (ShowScrollbar)
            maxWidth++;

        return (Math.Min(maxWidth, availableWidth), Math.Min(_lines.Length, availableHeight));
    }

    private void DrawScrollbar(IRenderContext context)
    {
        var x = context.Width - 1;
        var trackHeight = context.Height;

        if (trackHeight <= 0 || _lines.Length <= _viewportHeight)
            return;

        // Calculate thumb size and position
        var thumbHeight = Math.Max(1, (int)((float)_viewportHeight / _lines.Length * trackHeight));
        var maxThumbTop = trackHeight - thumbHeight;
        var thumbTop = MaxScroll > 0
            ? (int)((float)ScrollOffset / MaxScroll * maxThumbTop)
            : 0;

        // Draw track and thumb
        for (var y = 0; y < trackHeight; y++)
        {
            if (y >= thumbTop && y < thumbTop + thumbHeight)
            {
                // Thumb
                context.SetForeground(ScrollbarThumbColor);
                context.WriteAt(x, y, '█');
            }
            else
            {
                // Track
                context.SetForeground(ScrollbarTrackColor);
                context.WriteAt(x, y, '░');
            }
        }

        context.ResetColors();
    }

    private void MarkDirty()
    {
        OnDirty?.Invoke();
    }

    /// <summary>
    /// A render context that handles vertical scrolling offset.
    /// </summary>
    private sealed class ScrolledRenderContext : IRenderContext
    {
        private readonly IRenderContext _parent;
        private readonly int _offsetX;
        private readonly int _offsetY;
        private readonly int _clipHeight;

        public ScrolledRenderContext(IRenderContext parent, int offsetX, int offsetY, int width, int clipHeight)
        {
            _parent = parent;
            _offsetX = offsetX;
            _offsetY = offsetY;
            Width = width;
            _clipHeight = clipHeight;
        }

        public int Width { get; }
        public int Height => _clipHeight;

        public void WriteAt(int x, int y, string text)
        {
            var actualY = y + _offsetY;
            if (actualY < 0 || actualY >= _parent.Height)
                return;
            if (x < 0)
            {
                text = text[Math.Min(-x, text.Length)..];
                x = 0;
            }
            if (x >= Width)
                return;
            if (x + text.Length > Width)
                text = text[..(Width - x)];
            _parent.WriteAt(x + _offsetX, actualY, text);
        }

        public void WriteAt(int x, int y, char c)
        {
            var actualY = y + _offsetY;
            if (x < 0 || x >= Width || actualY < 0 || actualY >= _parent.Height)
                return;
            _parent.WriteAt(x + _offsetX, actualY, c);
        }

        public void SetForeground(Color color) => _parent.SetForeground(color);
        public void SetBackground(Color color) => _parent.SetBackground(color);
        public void ResetColors() => _parent.ResetColors();
        public void SetDecoration(Terminal.TextDecoration decoration) => _parent.SetDecoration(decoration);
        public void ApplyStyle(Terminal.TextStyle style) => _parent.ApplyStyle(style);

        public void Fill(int x, int y, int width, int height, char c = ' ')
        {
            var actualY = y + _offsetY;
            if (actualY < 0)
            {
                height += actualY;
                actualY = 0;
            }
            if (actualY + height > _parent.Height)
                height = _parent.Height - actualY;
            if (height <= 0)
                return;
            _parent.Fill(x + _offsetX, actualY, width, height, c);
        }

        public void Clear() => Fill(0, 0, Width, Height, ' ');

        public IRenderContext CreateSubContext(Layout.Rect bounds)
        {
            var clippedX = Math.Max(0, bounds.X);
            var clippedY = Math.Max(0, bounds.Y);
            var clippedRight = Math.Min(Width, bounds.Right);
            var clippedBottom = Math.Min(_clipHeight, bounds.Bottom);
            var clippedWidth = Math.Max(0, clippedRight - clippedX);
            var clippedHeight = Math.Max(0, clippedBottom - clippedY);

            return new ScrolledRenderContext(
                _parent,
                _offsetX + clippedX,
                _offsetY + clippedY,
                clippedWidth,
                clippedHeight);
        }
    }
}
