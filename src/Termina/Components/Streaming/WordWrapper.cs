using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// Utility class for word-wrapping text to fit within a specified width.
/// </summary>
public static class WordWrapper
{
    /// <summary>
    /// Wraps a single line of text to fit within the specified width.
    /// </summary>
    /// <param name="text">Text to wrap.</param>
    /// <param name="width">Maximum width per line.</param>
    /// <returns>List of wrapped lines.</returns>
    public static List<string> WrapLine(string text, int width)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");

        if (string.IsNullOrEmpty(text))
            return [""];

        var result = new List<string>();

        // Handle text that's shorter than width
        if (DisplayWidth.GetColumnCount(text) <= width)
        {
            result.Add(text);
            return result;
        }

        var currentLine = new System.Text.StringBuilder();
        var words = SplitIntoWords(text);

        foreach (var word in words)
        {
            var wordWidth = DisplayWidth.GetColumnCount(word);

            // If word itself is longer than width, break it
            if (wordWidth > width)
            {
                // Flush current line if it has content
                if (currentLine.Length > 0)
                {
                    result.Add(currentLine.ToString());
                    currentLine.Clear();
                }

                // Break long word into chunks
                var remaining = word;
                while (DisplayWidth.GetColumnCount(remaining) > width)
                {
                    var chunk = DisplayWidth.TruncateToColumns(remaining, width);
                    if (chunk.Length == 0)
                        break;

                    result.Add(chunk);
                    remaining = remaining[chunk.Length..];
                }

                if (remaining.Length > 0)
                {
                    currentLine.Append(remaining);
                }
            }
            else if (currentLine.Length == 0)
            {
                // Start of line
                currentLine.Append(word);
            }
            else if (DisplayWidth.GetColumnCount(currentLine.ToString()) + 1 + wordWidth <= width)
            {
                // Word fits with space
                currentLine.Append(' ');
                currentLine.Append(word);
            }
            else
            {
                // Word doesn't fit, start new line
                result.Add(currentLine.ToString());
                currentLine.Clear();
                currentLine.Append(word);
            }
        }

        // Flush remaining content
        if (currentLine.Length > 0)
        {
            result.Add(currentLine.ToString());
        }

        return result;
    }

    /// <summary>
    /// Wraps multiple lines of text.
    /// </summary>
    /// <param name="lines">Lines to wrap.</param>
    /// <param name="width">Maximum width per line.</param>
    /// <returns>All wrapped lines.</returns>
    public static List<string> WrapLines(IEnumerable<string> lines, int width)
    {
        var result = new List<string>();
        foreach (var line in lines)
        {
            result.AddRange(WrapLine(line, width));
        }
        return result;
    }

    /// <summary>
    /// Calculates how many wrapped lines a piece of text will produce.
    /// </summary>
    /// <param name="text">Text to measure.</param>
    /// <param name="width">Width for wrapping.</param>
    /// <returns>Number of wrapped lines.</returns>
    public static int CalculateWrappedLineCount(string text, int width)
    {
        return WrapLine(text, width).Count;
    }

    /// <summary>
    /// Calculates total wrapped line count for multiple lines.
    /// </summary>
    public static int CalculateTotalWrappedLineCount(IEnumerable<string> lines, int width)
    {
        return lines.Sum(line => CalculateWrappedLineCount(line, width));
    }

    private static List<string> SplitIntoWords(string text)
    {
        var words = new List<string>();
        var currentWord = new System.Text.StringBuilder();

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                // Preserve multiple spaces as empty words? Or collapse?
                // For now, collapse whitespace
            }
            else
            {
                currentWord.Append(c);
            }
        }

        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return words;
    }
}
