using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Components.Streaming;

/// <summary>
/// A component that efficiently renders streaming text content.
/// Renders ALL content - uses terminal's native scrollback for scrolling.
/// Use <see cref="StreamMode.Windowed"/> for rolling windows (e.g., thinking indicators).
/// </summary>
/// <remarks>
/// <para>
/// This component follows composition over inheritance - it handles text buffering and rendering.
/// For fixed-height scrolling viewports, wrap in a ScrollableViewport component.
/// </para>
/// <para>
/// Usage examples:
/// </para>
/// <code>
/// // Default: renders all content (uses terminal scrollback)
/// var chat = StreamingText.Create();
/// chat.Append("Hello ");
/// chat.Append("world!\n");
///
/// // Windowed mode for thinking indicators (rolling window)
/// var thinking = StreamingText.CreateWindowed(windowSize: 3);
/// thinking.AppendLine("Step 1...");
/// thinking.AppendLine("Step 2...");
/// thinking.AppendLine("Step 3..."); // Step 1 is discarded
/// </code>
/// <para>
/// Subscribe to <see cref="Component.ContentChanged"/> to be notified when
/// streaming content updates and the UI needs to be redrawn.
/// </para>
/// </remarks>
public class StreamingText : Component
{
    private readonly IStreamingTextBuffer _buffer;
    private int _maxWidth = 80;
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
    /// Gets or sets the maximum width for word wrapping.
    /// Set to 0 to disable word wrapping.
    /// </summary>
    public int MaxWidth
    {
        get => _maxWidth;
        set => _maxWidth = Math.Max(0, value);
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

    /// <inheritdoc />
    public override IRenderable Render()
    {
        // Get all lines from buffer
        var lines = _buffer.GetAllLines();

        if (lines.Count == 0)
        {
            return new Text("");
        }

        // Apply word wrapping if MaxWidth is set
        IEnumerable<string> displayLines = lines;
        if (_maxWidth > 0)
        {
            var effectiveWidth = _maxWidth;
            if (!string.IsNullOrEmpty(_prefix))
            {
                effectiveWidth -= _prefix.Length;
            }
            displayLines = WordWrapper.WrapLines(lines, effectiveWidth);
        }

        var rows = new List<IRenderable>();

        foreach (var line in displayLines)
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
    /// Creates a streaming text component. This is the default - renders all content.
    /// </summary>
    public static StreamingText Create() => new(StreamMode.Persisted);

    /// <summary>
    /// Creates a windowed streaming text component (rolling window).
    /// Only retains the last N lines - useful for thinking indicators, progress, etc.
    /// </summary>
    /// <param name="windowSize">Number of lines to retain.</param>
    public static StreamingText CreateWindowed(int windowSize = 3) =>
        new(StreamMode.Windowed, windowSize);
}
