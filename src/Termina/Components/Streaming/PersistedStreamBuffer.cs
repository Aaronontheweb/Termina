namespace Termina.Components.Streaming;

/// <summary>
/// A streaming text buffer that retains full history with scroll support.
/// Suitable for chat messages, command output, logs that need review.
/// </summary>
/// <remarks>
/// <para>
/// Key features:
/// </para>
/// <list type="bullet">
///   <item>Full content retained and scrollable</item>
///   <item>Auto-scroll when at bottom, maintains position when scrolled up</item>
///   <item>Thread-safe for concurrent append operations</item>
///   <item>Efficient append - doesn't re-process entire buffer</item>
/// </list>
/// </remarks>
public class PersistedStreamBuffer : IStreamingTextBuffer
{
    private readonly List<string> _lines = [];
    private readonly object _lock = new();
    private System.Text.StringBuilder _currentLine = new();

    // Scroll offset: 0 = bottom (newest), positive = scrolled up
    private int _scrollOffset;

    // Track if user has manually scrolled
    private bool _userScrolled;

    // Cache for wrapped line count (invalidated on content change)
    private int _cachedWrappedLineCount;
    private int _cachedWidth;
    private bool _wrappedCountDirty = true;

    /// <inheritdoc />
    public StreamMode Mode => StreamMode.Persisted;

    /// <inheritdoc />
    public int LineCount
    {
        get
        {
            lock (_lock)
            {
                // Include current partial line if non-empty
                return _lines.Count + (_currentLine.Length > 0 ? 1 : 0);
            }
        }
    }

    /// <inheritdoc />
    public int CharacterCount
    {
        get
        {
            lock (_lock)
            {
                var count = _lines.Sum(l => l.Length + 1); // +1 for newlines
                count += _currentLine.Length;
                return count;
            }
        }
    }

    /// <inheritdoc />
    public bool HasContent
    {
        get
        {
            lock (_lock)
            {
                return _lines.Count > 0 || _currentLine.Length > 0;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether auto-scroll is enabled.
    /// When true (default), view scrolls to bottom on new content if not manually scrolled.
    /// </summary>
    public bool AutoScroll { get; set; } = true;

    /// <summary>
    /// Gets the current scroll offset (0 = bottom).
    /// </summary>
    public int ScrollOffset
    {
        get
        {
            lock (_lock)
            {
                return _scrollOffset;
            }
        }
    }

    /// <summary>
    /// Gets whether the user has manually scrolled away from the bottom.
    /// </summary>
    public bool IsScrolledUp
    {
        get
        {
            lock (_lock)
            {
                return _userScrolled && _scrollOffset > 0;
            }
        }
    }

    /// <inheritdoc />
    public void Append(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        lock (_lock)
        {
            foreach (var c in text)
            {
                if (c == '\n')
                {
                    _lines.Add(_currentLine.ToString());
                    _currentLine.Clear();
                }
                else if (c != '\r') // Ignore carriage returns
                {
                    _currentLine.Append(c);
                }
            }

            _wrappedCountDirty = true;

            // Auto-scroll to bottom if enabled and user hasn't scrolled
            if (AutoScroll && !_userScrolled)
            {
                _scrollOffset = 0;
            }
        }
    }

    /// <inheritdoc />
    public void AppendLine(string line)
    {
        Append(line + "\n");
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _lines.Clear();
            _currentLine.Clear();
            _scrollOffset = 0;
            _userScrolled = false;
            _wrappedCountDirty = true;
        }
    }

    /// <summary>
    /// Scrolls up by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll (default 1).</param>
    /// <param name="viewportWidth">Width for word wrapping calculation.</param>
    public void ScrollUp(int lines = 1, int viewportWidth = 80)
    {
        lock (_lock)
        {
            _userScrolled = true;
            var maxScroll = GetMaxScrollOffset(viewportWidth);
            _scrollOffset = Math.Min(_scrollOffset + lines, maxScroll);
        }
    }

    /// <summary>
    /// Scrolls down by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll (default 1).</param>
    public void ScrollDown(int lines = 1)
    {
        lock (_lock)
        {
            _scrollOffset = Math.Max(0, _scrollOffset - lines);

            // If scrolled back to bottom, re-enable auto-scroll
            if (_scrollOffset == 0)
            {
                _userScrolled = false;
            }
        }
    }

    /// <summary>
    /// Scrolls to the bottom (newest content).
    /// </summary>
    public void ScrollToBottom()
    {
        lock (_lock)
        {
            _scrollOffset = 0;
            _userScrolled = false;
        }
    }

    /// <summary>
    /// Scrolls to the top (oldest content).
    /// </summary>
    /// <param name="viewportWidth">Width for word wrapping calculation.</param>
    public void ScrollToTop(int viewportWidth = 80)
    {
        lock (_lock)
        {
            _userScrolled = true;
            _scrollOffset = GetMaxScrollOffset(viewportWidth);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetVisibleLines(int viewportHeight, int viewportWidth)
    {
        if (viewportHeight <= 0 || viewportWidth <= 0)
            return [];

        lock (_lock)
        {
            var allLines = GetAllLinesInternal();
            var wrappedLines = WordWrapper.WrapLines(allLines, viewportWidth);

            if (wrappedLines.Count == 0)
                return [];

            // Calculate visible range from bottom with scroll offset
            var totalWrapped = wrappedLines.Count;
            var bottomIndex = totalWrapped - _scrollOffset;
            var topIndex = Math.Max(0, bottomIndex - viewportHeight);
            bottomIndex = Math.Min(totalWrapped, topIndex + viewportHeight);

            return wrappedLines.Skip(topIndex).Take(bottomIndex - topIndex).ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllLines()
    {
        lock (_lock)
        {
            return GetAllLinesInternal().ToList();
        }
    }

    /// <summary>
    /// Gets the total number of wrapped lines for the given width.
    /// </summary>
    public int GetWrappedLineCount(int viewportWidth)
    {
        lock (_lock)
        {
            if (_wrappedCountDirty || _cachedWidth != viewportWidth)
            {
                var allLines = GetAllLinesInternal();
                _cachedWrappedLineCount = WordWrapper.CalculateTotalWrappedLineCount(allLines, viewportWidth);
                _cachedWidth = viewportWidth;
                _wrappedCountDirty = false;
            }
            return _cachedWrappedLineCount;
        }
    }

    private int GetMaxScrollOffset(int viewportWidth)
    {
        var totalWrapped = GetWrappedLineCount(viewportWidth);
        return Math.Max(0, totalWrapped - 1); // Can scroll up to see first line at bottom
    }

    private IEnumerable<string> GetAllLinesInternal()
    {
        foreach (var line in _lines)
        {
            yield return line;
        }

        // Include current partial line if non-empty
        if (_currentLine.Length > 0)
        {
            yield return _currentLine.ToString();
        }
    }
}
