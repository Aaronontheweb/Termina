// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// A segment of text with associated style. Used for inline styled text rendering.
/// </summary>
/// <remarks>
/// <para>
/// StyledSegment is the fundamental unit for styled text. A line of styled text
/// is composed of multiple segments, each with potentially different styling.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var segment = new StyledSegment("Hello", new TextStyle(Color.Green));
/// var boldSegment = new StyledSegment("World", new TextStyle(Color.Red, Color.Default, TextDecoration.Bold));
/// </code>
/// </para>
/// </remarks>
public readonly record struct StyledSegment : IEquatable<StyledSegment>
{
    /// <summary>
    /// The text content of this segment.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// The style applied to this segment's text.
    /// </summary>
    public TextStyle Style { get; init; }

    /// <summary>
    /// Creates a new StyledSegment with the specified text and default style.
    /// </summary>
    /// <param name="text">The text content.</param>
    public StyledSegment(string text)
    {
        Text = text ?? string.Empty;
        Style = TextStyle.Default;
    }

    /// <summary>
    /// Creates a new StyledSegment with the specified text and style.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="style">The style to apply.</param>
    public StyledSegment(string text, TextStyle style)
    {
        Text = text ?? string.Empty;
        Style = style;
    }

    /// <summary>
    /// Creates a new StyledSegment with the specified text and foreground color.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="foreground">The foreground color.</param>
    public StyledSegment(string text, Color foreground)
    {
        Text = text ?? string.Empty;
        Style = new TextStyle(foreground);
    }

    /// <summary>
    /// Creates a new StyledSegment with the specified text, colors, and decoration.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="decoration">The text decoration.</param>
    public StyledSegment(string text, Color foreground, Color background, TextDecoration decoration = TextDecoration.None)
    {
        Text = text ?? string.Empty;
        Style = new TextStyle(foreground, background, decoration);
    }

    /// <summary>
    /// The display length of this segment (character count).
    /// </summary>
    public int Length => Text.Length;

    /// <summary>
    /// Returns true if this segment has no text content.
    /// </summary>
    public bool IsEmpty => Text.Length == 0;

    /// <summary>
    /// Creates a new segment with the same style but different text.
    /// </summary>
    /// <param name="newText">The new text content.</param>
    /// <returns>A new StyledSegment with the updated text.</returns>
    public StyledSegment WithText(string newText) => new(newText, Style);

    /// <summary>
    /// Creates a new segment with the same text but different style.
    /// </summary>
    /// <param name="newStyle">The new style.</param>
    /// <returns>A new StyledSegment with the updated style.</returns>
    public StyledSegment WithStyle(TextStyle newStyle) => new(Text, newStyle);

    /// <summary>
    /// Creates a substring of this segment, preserving the style.
    /// </summary>
    /// <param name="startIndex">The starting character index.</param>
    /// <returns>A new StyledSegment containing the substring.</returns>
    public StyledSegment Substring(int startIndex) => new(Text[startIndex..], Style);

    /// <summary>
    /// Creates a substring of this segment, preserving the style.
    /// </summary>
    /// <param name="startIndex">The starting character index.</param>
    /// <param name="length">The number of characters to include.</param>
    /// <returns>A new StyledSegment containing the substring.</returns>
    public StyledSegment Substring(int startIndex, int length) => new(Text.Substring(startIndex, length), Style);

    /// <summary>
    /// Creates an empty segment with no style.
    /// </summary>
    public static StyledSegment Empty => new(string.Empty);

    /// <inheritdoc />
    public bool Equals(StyledSegment other) => Text == other.Text && Style.Equals(other.Style);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Text, Style);

    /// <inheritdoc />
    public override string ToString() => Text;
}
