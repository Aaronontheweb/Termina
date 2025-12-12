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
    private readonly Queue<string> _lines;
    private readonly object _lock = new();
    private System.Text.StringBuilder _currentLine = new();

    /// <summary>
    /// Creates a new windowed stream buffer.
    /// </summary>
    /// <param name="windowSize">Maximum number of lines to retain (default 3).</param>
    public WindowedStreamBuffer(int windowSize = 3)
    {
        if (windowSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be positive.");

        _windowSize = windowSize;
        _lines = new Queue<string>(windowSize + 1);
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
                var count = _lines.Sum(l => l.Length + 1);
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
    /// Gets the number of lines that have been discarded due to window overflow.
    /// Useful for knowing how much content has scrolled past.
    /// </summary>
    public long DiscardedLineCount { get; private set; }

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
                    EnqueueLine(_currentLine.ToString());
                    _currentLine.Clear();
                }
                else if (c != '\r')
                {
                    _currentLine.Append(c);
                }
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
        if (viewportHeight <= 0 || viewportWidth <= 0)
            return [];

        lock (_lock)
        {
            var allLines = GetAllLinesInternal().ToList();
            var wrappedLines = WordWrapper.WrapLines(allLines, viewportWidth);

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
        lock (_lock)
        {
            return GetAllLinesInternal().ToList();
        }
    }

    private void EnqueueLine(string line)
    {
        _lines.Enqueue(line);

        // Trim to window size
        while (_lines.Count > _windowSize)
        {
            _lines.Dequeue();
            DiscardedLineCount++;
        }
    }

    private IEnumerable<string> GetAllLinesInternal()
    {
        foreach (var line in _lines)
        {
            yield return line;
        }

        if (_currentLine.Length > 0)
        {
            yield return _currentLine.ToString();
        }
    }
}
