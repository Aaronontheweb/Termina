// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Terminal;

/// <summary>
/// Text decoration flags that can be combined for rich text styling.
/// </summary>
/// <remarks>
/// These decorations map directly to ANSI escape codes and can be combined
/// using bitwise OR operations. For example:
/// <code>
/// var style = TextDecoration.Bold | TextDecoration.Underline;
/// </code>
/// </remarks>
[Flags]
public enum TextDecoration
{
    /// <summary>
    /// No text decoration.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bold text (ANSI SGR 1).
    /// </summary>
    Bold = 1 << 0,

    /// <summary>
    /// Dim/faint text (ANSI SGR 2).
    /// </summary>
    Dim = 1 << 1,

    /// <summary>
    /// Italic text (ANSI SGR 3).
    /// </summary>
    Italic = 1 << 2,

    /// <summary>
    /// Underlined text (ANSI SGR 4).
    /// </summary>
    Underline = 1 << 3,

    /// <summary>
    /// Strikethrough text (ANSI SGR 9).
    /// </summary>
    Strikethrough = 1 << 4
}
