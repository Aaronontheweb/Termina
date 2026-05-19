// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Diagnostics;

namespace Termina.Platform;

/// <summary>
/// Cross-platform helper for pushing and popping the kitty keyboard protocol enhancement bits
/// (<c>CSI &gt; N u</c> / <c>CSI &lt; u</c>) on the terminal's per-screen stack.
/// </summary>
/// <remarks>
/// Driven by the <c>TERMINA_KITTY_KEYBOARD</c> environment variable. Common value is <c>8</c>
/// (<c>report_all_keys</c>), which forces all keyboard input through <c>CSI ... u</c> /
/// <c>CSI 1;mods X</c> — unambiguously separating real arrow keys from <c>?1007h</c> wheel
/// events that continue to arrive as legacy <c>CSI A/B</c> or <c>SS3 OA/OB</c>.
/// <para>
/// Output bytes are written via <see cref="Console.Out"/> and require the host terminal to
/// be in a VT-processing output mode. On Windows this means
/// <c>ENABLE_VIRTUAL_TERMINAL_PROCESSING</c> must already be set on the output handle by
/// the platform console.
/// </para>
/// </remarks>
internal static class KittyKeyboardEnhancement
{
    /// <summary>
    /// Reads <c>TERMINA_KITTY_KEYBOARD</c> and, if it is a positive integer, writes
    /// <c>CSI &gt; flags u</c> to push the kitty enhancement onto the terminal's per-screen stack.
    /// </summary>
    /// <param name="traceSource">Object used for trace-source attribution in logs.</param>
    /// <returns>True if the enhancement was pushed; false if the env var was unset/invalid or the write failed.</returns>
    public static bool TryEnter(object traceSource)
    {
        var raw = Environment.GetEnvironmentVariable("TERMINA_KITTY_KEYBOARD");
        if (string.IsNullOrEmpty(raw)) return false;
        if (!int.TryParse(raw, out var flags) || flags <= 0) return false;

        try
        {
            Console.Out.Write($"\x1b[>{flags}u");
            Console.Out.Flush();
            TerminaTrace.Platform.Info(traceSource, "Pushed kitty keyboard flags={0}", flags);
            return true;
        }
        catch (Exception ex)
        {
            TerminaTrace.Platform.Error(traceSource, "Failed to push kitty keyboard flags: {0}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Pops the kitty enhancement from the terminal's per-screen stack by writing
    /// <c>CSI &lt; u</c>. Safe to call on shutdown / restore paths; swallows failures.
    /// </summary>
    public static void TryLeave()
    {
        try
        {
            Console.Out.Write("\x1b[<u");
            Console.Out.Flush();
        }
        catch
        {
            // Tearing down — nothing useful we can do here.
        }
    }
}
