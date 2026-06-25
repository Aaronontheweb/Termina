// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Termina.Terminal;

/// <summary>
/// A Unicode text element plus the number of terminal columns it occupies.
/// </summary>
/// <param name="Text">The original text element.</param>
/// <param name="StartIndex">The UTF-16 start index in the source string.</param>
/// <param name="Length">The UTF-16 length in the source string.</param>
/// <param name="ColumnWidth">The number of terminal columns occupied by this element.</param>
public readonly record struct DisplayCell(string Text, int StartIndex, int Length, int ColumnWidth);

/// <summary>
/// Utility for measuring and manipulating text in terminal display columns.
/// </summary>
/// <remarks>
/// Terminal cells are not UTF-16 characters. CJK, Hangul, kana, fullwidth forms,
/// and emoji typically occupy two terminal columns; combining marks and variation
/// selectors occupy zero. All layout, wrapping, truncation, and cursor math should
/// go through this class instead of using <see cref="string.Length"/>.
/// </remarks>
public static class DisplayWidth
{
    /// <summary>
    /// Enumerates display cells using Unicode text elements so surrogate pairs and
    /// combining sequences are never split by column-based operations.
    /// </summary>
    public static IEnumerable<DisplayCell> EnumerateCells(string text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        var indexes = StringInfo.ParseCombiningCharacters(text);
        for (var i = 0; i < indexes.Length; i++)
        {
            var start = indexes[i];
            var end = i + 1 < indexes.Length ? indexes[i + 1] : text.Length;
            var element = text[start..end];
            yield return new DisplayCell(element, start, end - start, GetTextElementWidth(element));
        }
    }

    /// <summary>
    /// Returns the number of terminal display columns occupied by a string.
    /// </summary>
    public static int GetColumnCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var columns = 0;
        foreach (var cell in EnumerateCells(text))
            columns += cell.ColumnWidth;
        return columns;
    }

    /// <summary>
    /// Returns the number of terminal display columns occupied by a single UTF-16 character.
    /// </summary>
    public static int GetColumnCount(char c) => GetColumnCount(c.ToString());

    /// <summary>
    /// Returns the leftmost text that fits within <paramref name="maxColumns"/> display columns.
    /// </summary>
    public static string TruncateToColumns(string text, int maxColumns)
    {
        if (string.IsNullOrEmpty(text) || maxColumns <= 0)
            return string.Empty;

        var sb = new StringBuilder();
        var columns = 0;

        foreach (var cell in EnumerateCells(text))
        {
            if (columns + cell.ColumnWidth > maxColumns)
                break;

            sb.Append(cell.Text);
            columns += cell.ColumnWidth;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the rightmost text that fits within <paramref name="maxColumns"/> display columns.
    /// </summary>
    public static string TruncateStartToColumns(string text, int maxColumns)
    {
        if (string.IsNullOrEmpty(text) || maxColumns <= 0)
            return string.Empty;

        var cells = EnumerateCells(text).ToArray();
        var columns = 0;
        var start = cells.Length;

        for (var i = cells.Length - 1; i >= 0; i--)
        {
            if (columns + cells[i].ColumnWidth > maxColumns)
                break;

            columns += cells[i].ColumnWidth;
            start = i;
        }

        return start >= cells.Length ? string.Empty : text[cells[start].StartIndex..];
    }

    /// <summary>
    /// Slices text by terminal columns. If the start falls inside a wide character,
    /// that character is skipped because terminals cannot draw half a cell pair.
    /// </summary>
    public static string SliceByColumns(string text, int startColumn, int maxColumns)
    {
        if (string.IsNullOrEmpty(text) || maxColumns <= 0)
            return string.Empty;

        startColumn = Math.Max(0, startColumn);
        var sb = new StringBuilder();
        var columns = 0;
        var taken = 0;

        foreach (var cell in EnumerateCells(text))
        {
            var nextColumns = columns + cell.ColumnWidth;
            if (nextColumns <= startColumn)
            {
                columns = nextColumns;
                continue;
            }

            if (columns < startColumn)
            {
                columns = nextColumns;
                continue;
            }

            if (taken + cell.ColumnWidth > maxColumns)
                break;

            sb.Append(cell.Text);
            taken += cell.ColumnWidth;
            columns = nextColumns;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the UTF-16 index after the longest prefix that fits within the column limit.
    /// </summary>
    public static int GetStringIndexForColumnCount(string text, int maxColumns)
    {
        if (string.IsNullOrEmpty(text) || maxColumns <= 0)
            return 0;

        var columns = 0;
        var index = 0;

        foreach (var cell in EnumerateCells(text))
        {
            if (columns + cell.ColumnWidth > maxColumns)
                break;

            columns += cell.ColumnWidth;
            index = cell.StartIndex + cell.Length;
        }

        return index;
    }

    /// <summary>
    /// Returns the display column position for a UTF-16 index in the string.
    /// </summary>
    public static int CursorPositionToColumn(string text, int charIndex)
    {
        if (string.IsNullOrEmpty(text) || charIndex <= 0)
            return 0;

        var columns = 0;
        var count = Math.Min(charIndex, text.Length);

        foreach (var cell in EnumerateCells(text))
        {
            if (cell.StartIndex + cell.Length > count)
                break;
            columns += cell.ColumnWidth;
        }

        return columns;
    }

    /// <summary>
    /// Returns the complete text element at a UTF-16 index, or a space at the end of the string.
    /// </summary>
    public static string GetTextElementAt(string text, int charIndex)
    {
        if (string.IsNullOrEmpty(text) || charIndex >= text.Length)
            return " ";

        charIndex = Math.Max(0, charIndex);
        foreach (var cell in EnumerateCells(text))
        {
            if (charIndex >= cell.StartIndex && charIndex < cell.StartIndex + cell.Length)
                return cell.Text;
        }

        return " ";
    }

    private static int GetTextElementWidth(string element)
    {
        var width = 0;
        var sawWideOrEmoji = false;

        foreach (var rune in element.EnumerateRunes())
        {
            var runeWidth = GetRuneWidth(rune);
            if (runeWidth >= 2)
                sawWideOrEmoji = true;
            width += runeWidth;
        }

        return sawWideOrEmoji ? 2 : width;
    }

    private static int GetRuneWidth(Rune rune)
    {
        var category = Rune.GetUnicodeCategory(rune);
        if (category is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.Format
            or UnicodeCategory.Control
            or UnicodeCategory.Surrogate)
            return 0;

        return IsWideOrFullwidth(rune) ? 2 : 1;
    }

    private static bool IsWideOrFullwidth(Rune rune)
    {
        var code = rune.Value;

        // East Asian Wide / Fullwidth ranges, based on wcwidth-style rules.
        if (code >= 0x1100 && code <= 0x115F) return true; // Hangul Jamo init. consonants
        if (code is 0x2329 or 0x232A) return true;
        if (code >= 0x2E80 && code <= 0xA4CF) return true; // CJK, kana, bopomofo, yi
        if (code >= 0xAC00 && code <= 0xD7A3) return true; // Hangul syllables
        if (code >= 0xF900 && code <= 0xFAFF) return true; // CJK compatibility ideographs
        if (code >= 0xFE10 && code <= 0xFE19) return true;
        if (code >= 0xFE30 && code <= 0xFE6F) return true;
        if (code >= 0xFF00 && code <= 0xFF60) return true; // Fullwidth forms, not halfwidth katakana
        if (code >= 0xFFE0 && code <= 0xFFE6) return true;
        if (code >= 0x20000 && code <= 0x3FFFD) return true; // CJK extensions

        // Emoji and pictographic symbols are rendered as two terminal columns by common terminals.
        if (code >= 0x1F000 && code <= 0x1FAFF) return true;
        if (code >= 0x2600 && code <= 0x27BF) return true;

        return false;
    }
}
