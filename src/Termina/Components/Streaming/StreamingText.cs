using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Components.Streaming;

/// <summary>
/// A component that efficiently renders streaming text content.
/// Supports both persisted (full history) and windowed (rolling) modes.
/// </summary>
/// <remarks>
/// <para>
/// Usage examples:
/// </para>
/// <code>
/// // Persisted mode for chat/logs (default)
/// var chat = new StreamingText();
/// chat.Append("Hello ");
/// chat.Append("world!\n");
///
/// // Windowed mode for thinking indicators
/// var thinking = new StreamingText(StreamMode.Windowed, windowSize: 2);
/// thinking.Append("Processing step 1...\n");
/// thinking.Append("Processing step 2...\n");
/// thinking.Append("Processing step 3...\n"); // Step 1 is now discarded
/// </code>
/// <para>
/// Subscribe to <see cref="Component.ContentChanged"/> to be notified when
/// streaming content updates and the UI needs to be redrawn.
/// </para>
/// </remarks>
public class StreamingText : Component
{
    private readonly IStreamingTextBuffer _buffer;
    private int _viewportHeight = 10;
    private int _viewportWidth = 80;
    private Style _textStyle = Style.Plain;
    private string? _prefix;
    private Style _prefixStyle = new(Color.Grey);

    /// <summary>
    /// Creates a new streaming text component with the specified mode.
    /// </summary>
    /// <param name="mode">Stream mode (Persisted or Windowed).</param>
    /// <param name="windowSize">For Windowed mode, the number of lines to retain.</param>
    public StreamingText(StreamMode mode = StreamMode.Persisted, int windowSize = 3)
    {
        _buffer = mode switch
        {
            StreamMode.Persisted => new PersistedStreamBuffer(),
            StreamMode.Windowed => new WindowedStreamBuffer(windowSize),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    /// <summary>
    /// Creates a new streaming text component with a custom buffer.
    /// </summary>
    /// <param name="buffer">Custom buffer implementation.</param>
    public StreamingText(IStreamingTextBuffer buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    /// <summary>
    /// Gets the underlying buffer.
    /// </summary>
    public IStreamingTextBuffer Buffer => _buffer;

    /// <summary>
    /// Gets or sets the viewport height (visible lines).
    /// </summary>
    public int ViewportHeight
    {
        get => _viewportHeight;
        set => _viewportHeight = Math.Max(1, value);
    }

    /// <summary>
    /// Gets or sets the viewport width (for word wrapping).
    /// </summary>
    public int ViewportWidth
    {
        get => _viewportWidth;
        set => _viewportWidth = Math.Max(10, value);
    }

    /// <summary>
    /// Gets or sets the text style.
    /// </summary>
    public Style TextStyle
    {
        get => _textStyle;
        set => _textStyle = value;
    }

    /// <summary>
    /// Gets or sets an optional prefix shown before each line (e.g., "> " for chat).
    /// </summary>
    public string? Prefix
    {
        get => _prefix;
        set => _prefix = value;
    }

    /// <summary>
    /// Gets or sets the prefix style.
    /// </summary>
    public Style PrefixStyle
    {
        get => _prefixStyle;
        set => _prefixStyle = value;
    }

    /// <summary>
    /// Appends text to the buffer and marks the component as needing redraw.
    /// </summary>
    public void Append(string text)
    {
        _buffer.Append(text);
        MarkDirty();
    }

    /// <summary>
    /// Consumes an async stream of text chunks, appending each to the buffer.
    /// When the stream completes or is cancelled, consumption stops gracefully.
    /// </summary>
    /// <param name="stream">The async stream of text chunks to consume.</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption.</param>
    /// <returns>A task that completes when the stream is exhausted or cancelled.</returns>
    public async Task ConsumeAsync(IAsyncEnumerable<string> stream, CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Append(chunk);
        }
    }

    /// <summary>
    /// Consumes an async stream of text lines, appending each as a new line.
    /// When the stream completes or is cancelled, consumption stops gracefully.
    /// </summary>
    /// <param name="stream">The async stream of lines to consume.</param>
    /// <param name="cancellationToken">Cancellation token to stop consumption.</param>
    /// <returns>A task that completes when the stream is exhausted or cancelled.</returns>
    public async Task ConsumeLinesAsync(IAsyncEnumerable<string> stream, CancellationToken cancellationToken = default)
    {
        await foreach (var line in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            AppendLine(line);
        }
    }

    /// <summary>
    /// Appends a line to the buffer and marks the component as needing redraw.
    /// </summary>
    public void AppendLine(string line)
    {
        _buffer.AppendLine(line);
        MarkDirty();
    }

    /// <summary>
    /// Clears all content and marks the component as needing redraw.
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
        MarkDirty();
    }

    /// <summary>
    /// Scrolls up (for persisted mode).
    /// </summary>
    public void ScrollUp(int lines = 1)
    {
        if (_buffer is PersistedStreamBuffer persisted)
        {
            persisted.ScrollUp(lines, _viewportWidth);
        }
    }

    /// <summary>
    /// Scrolls down (for persisted mode).
    /// </summary>
    public void ScrollDown(int lines = 1)
    {
        if (_buffer is PersistedStreamBuffer persisted)
        {
            persisted.ScrollDown(lines);
        }
    }

    /// <summary>
    /// Scrolls to the bottom (for persisted mode).
    /// </summary>
    public void ScrollToBottom()
    {
        if (_buffer is PersistedStreamBuffer persisted)
        {
            persisted.ScrollToBottom();
        }
    }

    /// <summary>
    /// Gets whether the user has scrolled away from the bottom (persisted mode only).
    /// </summary>
    public bool IsScrolledUp =>
        _buffer is PersistedStreamBuffer persisted && persisted.IsScrolledUp;

    /// <inheritdoc />
    public override IRenderable Render()
    {
        var effectiveWidth = _viewportWidth;
        if (!string.IsNullOrEmpty(_prefix))
        {
            effectiveWidth -= _prefix.Length;
        }

        var lines = _buffer.GetVisibleLines(_viewportHeight, effectiveWidth);

        if (lines.Count == 0)
        {
            return new Text("");
        }

        var rows = new List<IRenderable>();

        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(_prefix))
            {
                var prefixText = new Text(_prefix, _prefixStyle);
                var lineText = new Text(line, _textStyle);
                rows.Add(new Columns(prefixText, lineText) { Expand = false });
            }
            else
            {
                rows.Add(new Text(line, _textStyle));
            }
        }

        return new Rows(rows);
    }

    /// <summary>
    /// Creates a persisted streaming text component (full history with scroll).
    /// </summary>
    public static StreamingText CreatePersisted() => new(StreamMode.Persisted);

    /// <summary>
    /// Creates a windowed streaming text component (rolling window).
    /// </summary>
    /// <param name="windowSize">Number of lines to retain.</param>
    public static StreamingText CreateWindowed(int windowSize = 3) =>
        new(StreamMode.Windowed, windowSize);
}
