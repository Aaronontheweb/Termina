using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// A streaming text buffer that retains only the last N lines (rolling window).
/// Suitable for ephemeral content like "thinking" indicators, progress updates.
/// </summary>
/// <remarks>
/// <para>
/// Key features:
/// </para>
/// <list type="bullet">
///   <item>Fixed window size - oldest content discarded as new arrives</item>
///   <item>No scroll - always shows the most recent content</item>
///   <item>"Ticker tape" effect for continuous updates</item>
///   <item>Memory-efficient for high-volume streaming</item>
/// </list>
/// </remarks>
public class WindowedStreamBuffer : IStreamingTextBuffer
{
    private readonly int _windowSize;
    private readonly Queue<StyledLine> _styledLines;
    private readonly object _lock = new();
    private StyledLine _currentStyledLine = new();

    /// <summary>
    /// Creates a new windowed stream buffer.
    /// </summary>
    /// <param name="windowSize">Maximum number of lines to retain (default 3).</param>
    public WindowedStreamBuffer(int windowSize = 3)
    {
        if (windowSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be positive.");

        _windowSize = windowSize;
        _styledLines = new Queue<StyledLine>(windowSize + 1);
    }

    /// <inheritdoc />
    public StreamMode Mode => StreamMode.Windowed;

    /// <summary>
    /// Gets the configured window size.
    /// </summary>
    public int WindowSize => _windowSize;

    /// <inheritdoc />
    public int LineCount
    {
        get
        {
            lock (_lock)
            {
                return _styledLines.Count + (_currentStyledLine.Length > 0 ? 1 : 0);
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
                var count = _styledLines.Sum(l => l.Length + 1);
                count += _currentStyledLine.Length;
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
                return _styledLines.Count > 0 || _currentStyledLine.Length > 0;
            }
        }
    }

    /// <summary>
    /// Gets the number of lines that have been discarded due to window overflow.
    /// Useful for knowing how much content has scrolled past.
    /// </summary>
    public long DiscardedLineCount { get; private set; }

    /// <inheritdoc />
    public void Append(string text)
    {
        Append(text, TextStyle.Default);
    }

    /// <inheritdoc />
    public void Append(string text, TextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return;

        lock (_lock)
        {
            var pendingText = new System.Text.StringBuilder();

            foreach (var c in text)
            {
                if (c == '\n')
                {
                    // Flush pending text to current line
                    if (pendingText.Length > 0)
                    {
                        _currentStyledLine.Append(new StyledSegment(pendingText.ToString(), style));
                        pendingText.Clear();
                    }

                    // Finalize current line
                    EnqueueLine(_currentStyledLine);
                    _currentStyledLine = new StyledLine();
                }
                else if (c != '\r')
                {
                    pendingText.Append(c);
                }
            }

            // Append remaining text to current line
            if (pendingText.Length > 0)
            {
                _currentStyledLine.Append(new StyledSegment(pendingText.ToString(), style));
            }
        }
    }

    /// <inheritdoc />
    public void Append(string text, Color foreground)
    {
        Append(text, new TextStyle(foreground));
    }

    /// <inheritdoc />
    public void Append(StyledSegment segment)
    {
        Append(segment.Text, segment.Style);
    }

    /// <inheritdoc />
    public void AppendLine(string line)
    {
        Append(line + "\n");
    }

    /// <inheritdoc />
    public void AppendLine(string line, TextStyle style)
    {
        Append(line + "\n", style);
    }

    /// <inheritdoc />
    public void AppendLine(string line, Color foreground)
    {
        Append(line + "\n", new TextStyle(foreground));
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _styledLines.Clear();
            _currentStyledLine = new StyledLine();
            // Note: We don't reset DiscardedLineCount - it's cumulative
        }
    }

    /// <summary>
    /// Resets the discarded line counter.
    /// </summary>
    public void ResetDiscardedCount()
    {
        DiscardedLineCount = 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetVisibleLines(int viewportHeight, int viewportWidth)
    {
        // Delegate to styled version and convert to plain text
        return GetVisibleStyledLines(viewportHeight, viewportWidth)
            .Select(l => l.ToPlainText())
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<StyledLine> GetVisibleStyledLines(int viewportHeight, int viewportWidth)
    {
        if (viewportHeight <= 0 || viewportWidth <= 0)
            return [];

        lock (_lock)
        {
            var allLines = GetAllStyledLinesInternal().ToList();
            var wrappedLines = StyledWordWrapper.WrapLines(allLines, viewportWidth);

            // For windowed mode, always show the last viewportHeight lines
            // (or fewer if we don't have that many)
            if (wrappedLines.Count <= viewportHeight)
                return wrappedLines;

            return wrappedLines.Skip(wrappedLines.Count - viewportHeight).ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllLines()
    {
        return GetAllStyledLines().Select(l => l.ToPlainText()).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<StyledLine> GetAllStyledLines()
    {
        lock (_lock)
        {
            return GetAllStyledLinesInternal().ToList();
        }
    }

    private void EnqueueLine(StyledLine line)
    {
        _styledLines.Enqueue(line);

        // Trim to window size
        while (_styledLines.Count > _windowSize)
        {
            _styledLines.Dequeue();
            DiscardedLineCount++;
        }
    }

    private IEnumerable<StyledLine> GetAllStyledLinesInternal()
    {
        foreach (var line in _styledLines)
        {
            yield return line;
        }

        if (_currentStyledLine.Length > 0)
        {
            yield return _currentStyledLine;
        }
    }
}
