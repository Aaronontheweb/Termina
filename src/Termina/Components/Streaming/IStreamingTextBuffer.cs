using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// Interface for streaming text buffers that efficiently handle append-only text content.
/// </summary>
public interface IStreamingTextBuffer
{
    /// <summary>
    /// Gets the stream mode (Persisted or Windowed).
    /// </summary>
    StreamMode Mode { get; }

    /// <summary>
    /// Gets the total number of lines in the buffer.
    /// For Windowed mode, this is capped at the window size.
    /// </summary>
    int LineCount { get; }

    /// <summary>
    /// Gets the total character count in the buffer.
    /// </summary>
    int CharacterCount { get; }

    /// <summary>
    /// Gets whether the buffer has content.
    /// </summary>
    bool HasContent { get; }

    /// <summary>
    /// Appends text to the buffer. Handles newlines appropriately.
    /// </summary>
    /// <param name="text">Text to append (may contain newlines).</param>
    void Append(string text);

    /// <summary>
    /// Appends a complete line to the buffer.
    /// </summary>
    /// <param name="line">Line to append (newline added automatically).</param>
    void AppendLine(string line);

    /// <summary>
    /// Clears all content from the buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets visible lines for rendering, given a viewport height and width.
    /// Handles word wrapping internally.
    /// </summary>
    /// <param name="viewportHeight">Number of lines visible.</param>
    /// <param name="viewportWidth">Width for word wrapping.</param>
    /// <returns>Lines to render, already wrapped to fit viewport.</returns>
    IReadOnlyList<string> GetVisibleLines(int viewportHeight, int viewportWidth);

    /// <summary>
    /// Gets all raw lines (unwrapped) in the buffer.
    /// </summary>
    IReadOnlyList<string> GetAllLines();

    // Styled text support

    /// <summary>
    /// Appends a styled segment to the buffer.
    /// </summary>
    /// <param name="segment">The styled segment to append.</param>
    void Append(StyledSegment segment);

    /// <summary>
    /// Appends text with the specified style to the buffer.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <param name="style">The style to apply.</param>
    void Append(string text, TextStyle style);

    /// <summary>
    /// Appends text with the specified foreground color.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <param name="foreground">The foreground color.</param>
    void Append(string text, Color foreground);

    /// <summary>
    /// Appends a complete line with the specified style to the buffer.
    /// </summary>
    /// <param name="line">Line to append (newline added automatically).</param>
    /// <param name="style">The style to apply.</param>
    void AppendLine(string line, TextStyle style);

    /// <summary>
    /// Appends a complete line with the specified foreground color.
    /// </summary>
    /// <param name="line">Line to append (newline added automatically).</param>
    /// <param name="foreground">The foreground color.</param>
    void AppendLine(string line, Color foreground);

    /// <summary>
    /// Gets visible styled lines for rendering, given a viewport height and width.
    /// Handles word wrapping internally while preserving styles.
    /// </summary>
    /// <param name="viewportHeight">Number of lines visible.</param>
    /// <param name="viewportWidth">Width for word wrapping.</param>
    /// <returns>Styled lines to render, already wrapped to fit viewport.</returns>
    IReadOnlyList<StyledLine> GetVisibleStyledLines(int viewportHeight, int viewportWidth);

    /// <summary>
    /// Gets all raw styled lines (unwrapped) in the buffer.
    /// </summary>
    IReadOnlyList<StyledLine> GetAllStyledLines();
}
