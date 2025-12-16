// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// Word wrapper that preserves styling across line breaks.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the plain <see cref="WordWrapper"/>, this class operates on <see cref="StyledLine"/>
/// objects and ensures that text styles are preserved when lines are wrapped.
/// </para>
/// <para>
/// Example:
/// <code>
/// var line = new StyledLine();
/// line.Append("Hello ", Color.Green);
/// line.Append("World", Color.Red);
///
/// var wrapped = StyledWordWrapper.WrapLine(line, 8);
/// // Result: two lines, "Hello" (green) and "World" (red)
/// </code>
/// </para>
/// </remarks>
public static class StyledWordWrapper
{
    /// <summary>
    /// Wraps a styled line to fit within the specified width, preserving styling.
    /// </summary>
    /// <param name="line">The line to wrap.</param>
    /// <param name="width">Maximum width per line.</param>
    /// <returns>List of wrapped styled lines.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when width is not positive.</exception>
    public static List<StyledLine> WrapLine(StyledLine line, int width)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");

        if (line.IsEmpty)
            return [new StyledLine()];

        // Fast path: line fits within width
        if (line.Length <= width)
            return [line.Clone()];

        var result = new List<StyledLine>();
        var currentLine = new StyledLine();
        var currentWidth = 0;

        // Split into styled words
        var words = SplitIntoStyledWords(line);

        foreach (var word in words)
        {
            // If word itself is longer than width, break it
            if (word.Length > width)
            {
                // Flush current line if it has content
                if (currentWidth > 0)
                {
                    result.Add(currentLine);
                    currentLine = new StyledLine();
                    currentWidth = 0;
                }

                // Break long word into chunks while preserving styles
                var remaining = word;
                while (remaining.Length > width)
                {
                    var chunk = remaining.Substring(0, width);
                    result.Add(chunk);
                    remaining = remaining.Substring(width);
                }

                // Remainder becomes start of new line
                if (remaining.Length > 0)
                {
                    currentLine = remaining;
                    currentWidth = remaining.Length;
                }
            }
            else if (currentWidth == 0)
            {
                // Start of line - add word directly
                foreach (var segment in word.Segments)
                {
                    currentLine.Append(segment);
                }
                currentWidth = word.Length;
            }
            else if (currentWidth + 1 + word.Length <= width)
            {
                // Word fits with space separator
                currentLine.Append(" ");
                foreach (var segment in word.Segments)
                {
                    currentLine.Append(segment);
                }
                currentWidth = currentLine.Length;
            }
            else
            {
                // Word doesn't fit, start new line
                result.Add(currentLine);
                currentLine = new StyledLine();
                foreach (var segment in word.Segments)
                {
                    currentLine.Append(segment);
                }
                currentWidth = word.Length;
            }
        }

        // Flush remaining content
        if (currentWidth > 0)
        {
            result.Add(currentLine);
        }

        return result.Count > 0 ? result : [new StyledLine()];
    }

    /// <summary>
    /// Wraps multiple styled lines.
    /// </summary>
    /// <param name="lines">Lines to wrap.</param>
    /// <param name="width">Maximum width per line.</param>
    /// <returns>All wrapped styled lines.</returns>
    public static List<StyledLine> WrapLines(IEnumerable<StyledLine> lines, int width)
    {
        var result = new List<StyledLine>();
        foreach (var line in lines)
        {
            result.AddRange(WrapLine(line, width));
        }
        return result;
    }

    /// <summary>
    /// Calculates how many wrapped lines a styled line will produce.
    /// </summary>
    /// <param name="line">Line to measure.</param>
    /// <param name="width">Width for wrapping.</param>
    /// <returns>Number of wrapped lines.</returns>
    public static int CalculateWrappedLineCount(StyledLine line, int width)
    {
        return WrapLine(line, width).Count;
    }

    /// <summary>
    /// Calculates total wrapped line count for multiple styled lines.
    /// </summary>
    public static int CalculateTotalWrappedLineCount(IEnumerable<StyledLine> lines, int width)
    {
        return lines.Sum(line => CalculateWrappedLineCount(line, width));
    }

    /// <summary>
    /// Splits a styled line into styled words, preserving styling for each character.
    /// </summary>
    /// <remarks>
    /// This handles the complex case where a word may span multiple segments with
    /// different styles. Each resulting "word" is a StyledLine that may contain
    /// multiple segments.
    /// </remarks>
    private static List<StyledLine> SplitIntoStyledWords(StyledLine line)
    {
        var words = new List<StyledLine>();
        var currentWord = new StyledLine();

        foreach (var segment in line.Segments)
        {
            var currentSegmentStart = 0;

            for (var i = 0; i < segment.Length; i++)
            {
                var c = segment.Text[i];

                if (char.IsWhiteSpace(c))
                {
                    // Flush any accumulated text from this segment to current word
                    if (i > currentSegmentStart)
                    {
                        var text = segment.Text.Substring(currentSegmentStart, i - currentSegmentStart);
                        currentWord.Append(new StyledSegment(text, segment.Style));
                    }

                    // If we have a word, add it to results
                    if (!currentWord.IsEmpty)
                    {
                        words.Add(currentWord);
                        currentWord = new StyledLine();
                    }

                    currentSegmentStart = i + 1;
                }
            }

            // Flush remaining text from this segment
            if (currentSegmentStart < segment.Length)
            {
                var text = segment.Text.Substring(currentSegmentStart);
                currentWord.Append(new StyledSegment(text, segment.Style));
            }
        }

        // Add final word if any
        if (!currentWord.IsEmpty)
        {
            words.Add(currentWord);
        }

        return words;
    }
}
