// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Terminal;

/// <summary>
/// Immutable style specification for text rendering, combining foreground color,
/// background color, and text decorations.
/// </summary>
/// <remarks>
/// <para>
/// TextStyle is a value type designed for efficient comparison and storage.
/// Use it to specify inline styling for text segments in streaming text nodes.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var style = new TextStyle
/// {
///     Foreground = Color.Yellow,
///     Background = Color.Default,
///     Decoration = TextDecoration.Bold | TextDecoration.Underline
/// };
/// </code>
/// </para>
/// </remarks>
public readonly record struct TextStyle : IEquatable<TextStyle>
{
    /// <summary>
    /// The foreground (text) color.
    /// </summary>
    public Color Foreground { get; init; }

    /// <summary>
    /// The background color.
    /// </summary>
    public Color Background { get; init; }

    /// <summary>
    /// Text decorations (bold, italic, underline, etc.).
    /// </summary>
    public TextDecoration Decoration { get; init; }

    /// <summary>
    /// Creates a new TextStyle with default values (no colors, no decorations).
    /// </summary>
    public TextStyle()
    {
        Foreground = Color.Default;
        Background = Color.Default;
        Decoration = TextDecoration.None;
    }

    /// <summary>
    /// Creates a new TextStyle with the specified foreground color.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    public TextStyle(Color foreground) : this()
    {
        Foreground = foreground;
    }

    /// <summary>
    /// Creates a new TextStyle with the specified colors.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    public TextStyle(Color foreground, Color background) : this()
    {
        Foreground = foreground;
        Background = background;
    }

    /// <summary>
    /// Creates a new TextStyle with the specified colors and decorations.
    /// </summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="decoration">The text decorations.</param>
    public TextStyle(Color foreground, Color background, TextDecoration decoration)
    {
        Foreground = foreground;
        Background = background;
        Decoration = decoration;
    }

    /// <summary>
    /// Default style with no colors or decorations.
    /// </summary>
    public static TextStyle Default => new();

    /// <summary>
    /// Returns true if this style has no colors or decorations set (all defaults).
    /// </summary>
    public bool IsDefault =>
        Foreground == Color.Default &&
        Background == Color.Default &&
        Decoration == TextDecoration.None;

    /// <summary>
    /// Returns true if this style has any decoration flags set.
    /// </summary>
    public bool HasDecoration => Decoration != TextDecoration.None;

    /// <summary>
    /// Returns true if this style has a non-default foreground color.
    /// </summary>
    public bool HasForeground => Foreground != Color.Default;

    /// <summary>
    /// Returns true if this style has a non-default background color.
    /// </summary>
    public bool HasBackground => Background != Color.Default;

    /// <summary>
    /// Creates a new style with the specified foreground color, preserving other properties.
    /// </summary>
    public TextStyle WithForeground(Color color) => this with { Foreground = color };

    /// <summary>
    /// Creates a new style with the specified background color, preserving other properties.
    /// </summary>
    public TextStyle WithBackground(Color color) => this with { Background = color };

    /// <summary>
    /// Creates a new style with the specified decoration, preserving colors.
    /// </summary>
    public TextStyle WithDecoration(TextDecoration decoration) => this with { Decoration = decoration };

    /// <summary>
    /// Creates a new style with the decoration added to existing decorations.
    /// </summary>
    public TextStyle AddDecoration(TextDecoration decoration) => this with { Decoration = Decoration | decoration };

    /// <summary>
    /// Creates a new style with the decoration removed from existing decorations.
    /// </summary>
    public TextStyle RemoveDecoration(TextDecoration decoration) => this with { Decoration = Decoration & ~decoration };

    /// <summary>
    /// Returns true if this style has the specified decoration flag.
    /// </summary>
    public bool HasFlag(TextDecoration decoration) => Decoration.HasFlag(decoration);

    /// <inheritdoc />
    public bool Equals(TextStyle other) =>
        Foreground == other.Foreground &&
        Background == other.Background &&
        Decoration == other.Decoration;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Foreground, Background, Decoration);
}
