// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Termina.Terminal;

/// <summary>
/// Utility for measuring and manipulating text in terminal display columns.
///
/// Terminal cells have variable-width characters: CJK and other East Asian
/// characters occupy 2 columns, while ASCII/Latin occupy 1. This class
/// provides methods that work with display columns rather than raw char count,
/// which is critical for correct text rendering, truncation, and cursor positioning.
///
/// Reference: Unicode East Asian Width (UAX #11).
/// </summary>
public static class DisplayWidth
{
    /// <summary>
    /// Returns the number of terminal display columns occupied by a string.
    /// CJK, Hiragana, Katakana, Hangul, and fullwidth forms count as 2 columns.
    /// Everything else (ASCII, Latin, Cyrillic, etc.) counts as 1.
    /// </summary>
    public static int GetColumnCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var columns = 0;
        foreach (var c in text)
        {
            columns += IsFullWidth(c) ? 2 : 1;
        }

        return columns;
    }

    /// <summary>
    /// Returns the leftmost N display columns of a string, properly truncating
    /// so we never split a fullwidth character in the middle.
    /// </summary>
    public static string TruncateToColumns(string text, int maxColumns)
    {
        if (string.IsNullOrEmpty(text) || maxColumns <= 0)
            return string.Empty;

        var sb = new StringBuilder();
        var columns = 0;

        foreach (var c in text)
        {
            var charWidth = IsFullWidth(c) ? 2 : 1;
            if (columns + charWidth > maxColumns)
                break; // Don't include this character — we'd exceed the limit

            sb.Append(c);
            columns += charWidth;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns the display column position after placing N grapheme clusters
    /// in the string (accounting for fullwidth width).
    /// </summary>
    public static int CursorPositionToColumn(string text, int charIndex)
    {
        if (string.IsNullOrEmpty(text) || charIndex <= 0)
            return 0;

        var columns = 0;
        var count = Math.Min(charIndex, text.Length);

        for (var i = 0; i < count; i++)
        {
            columns += IsFullWidth(text[i]) ? 2 : 1;
        }

        return columns;
    }

    /// <summary>
    /// Determines whether a character is fullwidth (occupies 2 terminal columns).
    ///
    /// Covers the major East Asian Unicode ranges defined in UAX #11:
    /// - CJK Unified Ideographs + extensions
    /// - CJK Radicals, Symbols, Punctuation
    /// - Hiragana, Katakana
    /// - Hangul Syllables and Jamo
    /// - Halfwidth/Fullwidth Forms (U+FF00–U+FF5E are fullwidth)
    /// </summary>
    private static bool IsFullWidth(char c)
    {
        var code = (int)c;

        // CJK Unified Ideographs (U+4E00–U+9FFF)
        if (code >= 0x4E00 && code <= 0x9FFF) return true;

        // CJK Radicals Supplement (U+2E80–U+2EFF)
        if (code >= 0x2E80 && code <= 0x2EFF) return true;

        // CJK Strokes (U+2E00–U+2E7F)
        if (code >= 0x2E00 && code <= 0x2E7F) return true;

        // CJK Compatibility Ideographs (U+F900–U+FAFF)
        if (code >= 0xF900 && code <= 0xFAFF) return true;

        // CJK Compatibility Ideographs Supplement (U+2FF0–U+2FFF)
        if (code >= 0x2FF0 && code <= 0x2FFF) return true;

        // CJK Symbols and Punctuation (U+3000–U+303F)
        if (code >= 0x3000 && code <= 0x303F) return true;

        // Ideographic Description Characters (U+2FF0–U+2FFD) — overlap above, safe
        if (code >= 0x2FF0 && code <= 0x2FFD) return true;

        // Hiragana (U+3040–U+309F)
        if (code >= 0x3040 && code <= 0x309F) return true;

        // Hiragana Extended Supplement (U+1B001–U+1B0FF)
        if (code >= 0x1B001 && code <= 0x1B0FF) return true;

        // Katakana (U+30A0–U+30FF)
        if (code >= 0x30A0 && code <= 0x30FF) return true;

        // Katakana Phonetic Extensions (U+31F0–U+31FF)
        if (code >= 0x31F0 && code <= 0x31FF) return true;

        // Small Kana Extension (U+1B150–U+1B16F)
        if (code >= 0x1B150 && code <= 0x1B16F) return true;

        // Bopomofo (U+3100–U+312F)
        if (code >= 0x3100 && code <= 0x312F) return true;

        // Bopomofo Extended (U+31A0–U+31BF)
        if (code >= 0x31A0 && code <= 0x31BF) return true;

        // CJK Strokes (U+2E80–U+2EFF) — overlap, safe

        // Hangul Jamo (U+1100–U+11FF)
        if (code >= 0x1100 && code <= 0x11FF) return true;

        // Hangul Jamo Extended-A (U+A960–U+A97F)
        if (code >= 0xA960 && code <= 0xA97F) return true;

        // Hangul Jamo Extended-B (U+D7B0–U+D7FF)
        if (code >= 0xD7B0 && code <= 0xD7FF) return true;

        // Hangul Syllables (U+AC00–U+D7AF)
        if (code >= 0xAC00 && code <= 0xD7AF) return true;

        // Hangul Compatibility Jamo (U+3130–U+318F)
        if (code >= 0x3130 && code <= 0x318F) return true;

        // Halfwidth Katakana (U+FF61–U+FF9F) — some are fullwidth
        if (code >= 0xFF61 && code <= 0xFF9F) return true;

        // Halfwidth/Fullwidth Forms (U+FF01–U+FF60): fullwidth Latin, digits, punctuation
        // Note: U+FF61 is 1-col, U+FF62–U+FF65 are fullwidth. We handle the fullwidth block.
        if (code >= 0xFF01 && code <= 0xFF60) return true;

        // CJK Unified Ideographs Extension A (U+3400–U+4DBF)
        if (code >= 0x3400 && code <= 0x4DBF) return true;

        // CJK Unified Ideographs Extension B (U+20000–U+2A6DF) — surrogates
        if (code >= 0x20000 && code <= 0x2A6DF) return true;

        // CJK Unified Ideographs Extension C–F are beyond BMP (surrogate pairs)
        // Extension G (U+30000–U+3134F) also beyond BMP

        // CJK Compatibility Ideographs (U+F900–U+FAFF) — covered above
        // CJK Compatibility Forms (U+FE30–U+FE4F) — not fullwidth, skip

        // CJK Radicals Supplement Supplement (U+2E80–U+2EFF) — covered above

        // General Punctuation — not fullwidth except as listed

        // Emoji range: U+1F300–U+1FAFF — typically fullwidth
        if (code >= 0x1F300 && code <= 0x1FAFF) return true;

        // Misc. Symbols and Pictographs (U+1F300–U+1F5FF)
        // Emoticons (U+1F600–U+1F64F)
        if (code >= 0x1F600 && code <= 0x1F64F) return true;

        // Supplemental Symbols and Pictographs (U+1F900–U+1F9FF)
        if (code >= 0x1F900 && code <= 0x1F9FF) return true;

        // Miscellaneous Symbols (U+2600–U+26FF) — some may be fullwidth
        if (code >= 0x2600 && code <= 0x26FF) return true;

        // Dingbats (U+2700–U+27BF)
        if (code >= 0x2700 && code <= 0x27BF) return true;

        return false;
    }
}
