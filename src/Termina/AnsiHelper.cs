using System.Text.RegularExpressions;

namespace Termina;

/// <summary>
/// Utilities for working with ANSI escape sequences
/// </summary>
public static partial class AnsiHelper
{
    // Regex to match ANSI escape sequences (e.g., \x1b[32m, \x1b[0m)
    [GeneratedRegex(@"\x1b\[[0-9;]*m")]
    private static partial Regex AnsiCodeRegex();

    /// <summary>
    /// Strip all ANSI escape codes from a string
    /// </summary>
    public static string StripAnsiCodes(string text)
    {
        return AnsiCodeRegex().Replace(text, string.Empty);
    }

    /// <summary>
    /// Get the visual width of a string (excluding ANSI codes)
    /// </summary>
    public static int GetVisualWidth(string text)
    {
        return StripAnsiCodes(text).Length;
    }

    /// <summary>
    /// Pad a string to the specified visual width (accounting for ANSI codes)
    /// </summary>
    public static string PadRightVisual(string text, int totalWidth)
    {
        var visualWidth = GetVisualWidth(text);
        if (visualWidth >= totalWidth)
            return text;

        var paddingNeeded = totalWidth - visualWidth;
        return text + new string(' ', paddingNeeded);
    }

    /// <summary>
    /// Truncate a string to the specified visual width (accounting for ANSI codes)
    /// Preserves ANSI codes and adds "..." if truncated
    /// </summary>
    public static string TruncateVisual(string text, int maxWidth)
    {
        var visualWidth = GetVisualWidth(text);
        if (visualWidth <= maxWidth)
            return text;

        // Strip ANSI codes, truncate, then we lose the formatting
        // For now, simple approach: strip, truncate, add ellipsis
        var stripped = StripAnsiCodes(text);
        if (maxWidth <= 3)
            return stripped.Substring(0, maxWidth);

        return stripped.Substring(0, maxWidth - 3) + "...";
    }
}
