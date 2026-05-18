// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

namespace Termina.Demo.RawStdinProbe;

/// <summary>
/// Standalone raw-stdin probe for the future <c>UnixConsole</c> implementation. No dependency on
/// Termina itself — intentionally a bare console app so termios behavior and raw byte flow can be
/// verified on the user's actual terminal before any framework code changes.
/// </summary>
/// <remarks>
/// What it does:
/// <list type="number">
///   <item>Prints platform, runtime-computed termios offsets, and current VMIN/VTIME so the user
///   can confirm the constants are right on their box.</item>
///   <item>Sends <c>?1007h</c> (alternate scroll) + <c>?1h</c> (DECCKM application cursor keys).</item>
///   <item>Enters raw mode via <c>cfmakeraw</c>, preserves <c>OPOST</c> for sane log output,
///   sets <c>VMIN=0</c>/<c>VTIME=1</c>.</item>
///   <item>Loops on direct <c>libc.read(0, ...)</c>, printing every byte as
///   <c>0xXX (char|.)</c> plus annotations for recognized wheel/arrow sequences.</item>
///   <item>Quits cleanly on <c>q</c> or <c>Ctrl+C</c> (byte 0x03, since <c>cfmakeraw</c> clears
///   <c>ISIG</c>). Restores termios on exit, <see cref="AppDomain.ProcessExit"/>, and
///   <see cref="Console.CancelKeyPress"/>.</item>
/// </list>
/// </remarks>
internal static class Program
{
    private const int StdInFd = 0;
    private const int StdOutFd = 1;
    private const int Tcsanow = 0;
    private const int TermiosBufferSize = 256;

    // termios layout — same constants Phase 2 UnixConsole will use.
    //   Linux:  4 × tcflag_t (4 B each) + c_line (1 B)  → c_cc base 17; VTIME=5, VMIN=6.
    //   macOS:  4 × tcflag_t (8 B each)                  → c_cc base 32; VMIN=16, VTIME=17.
    private static int CcBase => OperatingSystem.IsMacOS() ? 32 : 17;
    private static int VminIdx => OperatingSystem.IsMacOS() ? 16 : 6;
    private static int VtimeIdx => OperatingSystem.IsMacOS() ? 17 : 5;
    private static int VminOffset => CcBase + VminIdx;
    private static int VtimeOffset => CcBase + VtimeIdx;

    // c_oflag is the second tcflag_t in the struct. OPOST is the low bit on both platforms.
    private static int COflagOffset => OperatingSystem.IsMacOS() ? 8 : 4;
    private const byte OpostBit = 0x01;

    private static readonly byte[] SavedTermios = new byte[TermiosBufferSize];
    private static bool _rawModeEntered;
    private static bool _restored;
    private static int _kittyFlags;
    private static bool _kittyPushed;

    private static int Main()
    {
        if (OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("This probe only runs on Linux/macOS. Windows uses WindowsConsole.");
            return 1;
        }

        Console.OutputEncoding = Encoding.UTF8;

        // Register restore hooks before we touch termios.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => SafeRestore();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => SafeRestore();
        Console.CancelKeyPress += (_, e) =>
        {
            // With ISIG cleared by cfmakeraw, this handler shouldn't fire while in raw mode —
            // but cover the window before EnterRawMode() and after Restore().
            SafeRestore();
            e.Cancel = false;
        };

        try
        {
            PrintBanner();
            EnterRawMode();
            // ?1049h: switch to alternate screen buffer. ?1007h's wheel-as-arrow emission only
            // applies on the alt screen per xterm.ctlseqs. ?1h: DECCKM application cursor keys.
            Write("\x1b[?1049h\x1b[?1007h\x1b[?1h");
            PrintInfo("Sent ?1049h (alt screen) + ?1007h (alternate scroll) + ?1h (DECCKM)");

            // Optional: kitty keyboard progressive enhancement push. TERMINA_KITTY_FLAGS env var
            // selects which flag bits to set. Common values:
            //   0  → disabled (legacy behavior; default if env unset/0)
            //   1  → disambiguate escape codes only
            //   8  → report all keys as escape codes (implies disambiguate)
            //   9  → 1 | 8
            //   11 → 1 | 2 | 8 (also report event types)
            // We use CSI > <flags> u (PUSH onto the kitty stack) so we can cleanly pop on exit
            // without clobbering whatever the parent shell had configured.
            var kittyEnv = Environment.GetEnvironmentVariable("TERMINA_KITTY_FLAGS");
            if (int.TryParse(kittyEnv, out var flags) && flags > 0)
            {
                _kittyFlags = flags;
                Write($"\x1b[>{flags}u");
                _kittyPushed = true;
                PrintInfo($"Sent CSI > {flags} u   (kitty keyboard PUSH; bits: {DescribeKittyFlags(flags)})");
                PrintInfo("Now press the same keys: arrows should arrive as CSI ... u (or CSI 1;<mods>A); wheel should still arrive as legacy ESC OA/B or ESC [A/B.");
            }
            else
            {
                PrintInfo("(no kitty keyboard enhancement; set TERMINA_KITTY_FLAGS=8 to enable)");
            }

            PrintInfo("Try: scroll wheel | press arrow keys | type some text | hold Shift+arrow | Ctrl+C | press 'q' to quit");
            PrintInfo("--------------------------------------------------------------------------------");

            RunLoop();
        }
        catch (Exception ex)
        {
            SafeRestore();
            Console.Error.WriteLine($"FATAL: {ex}");
            return 2;
        }
        finally
        {
            // Disable mouse modes and leave the alt screen before restoring termios so the
            // terminal doesn't keep sending CSI sequences on the user's shell prompt after exit.
            try
            {
                if (_kittyPushed)
                {
                    // POP one entry off the kitty keyboard stack.
                    Write("\x1b[<u");
                }
                Write("\x1b[?1l\x1b[?1007l\x1b[?1049l");
            }
            catch { /* ignore */ }
            SafeRestore();
        }

        return 0;
    }

    private static string DescribeKittyFlags(int flags)
    {
        var parts = new List<string>();
        if ((flags & 1) != 0) parts.Add("disambiguate");
        if ((flags & 2) != 0) parts.Add("report-event-types");
        if ((flags & 4) != 0) parts.Add("report-alternate-keys");
        if ((flags & 8) != 0) parts.Add("report-all-keys-as-escape-codes");
        if ((flags & 16) != 0) parts.Add("report-associated-text");
        return parts.Count == 0 ? "(none)" : string.Join("|", parts);
    }

    private static void PrintBanner()
    {
        var os = OperatingSystem.IsMacOS() ? "macOS"
               : OperatingSystem.IsLinux() ? "Linux"
               : RuntimeInformation.OSDescription;
        Console.WriteLine("==== Termina raw-stdin probe ====");
        Console.WriteLine($"OS:               {os}");
        Console.WriteLine($"Arch:             {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($"Runtime:          {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"TERM:             {Environment.GetEnvironmentVariable("TERM") ?? "(unset)"}");
        Console.WriteLine($"c_oflag offset:   {COflagOffset}  (OPOST bit 0x{OpostBit:X2})");
        Console.WriteLine($"c_cc base:        {CcBase}");
        Console.WriteLine($"VMIN offset:      {VminOffset}  (c_cc base {CcBase} + idx {VminIdx})");
        Console.WriteLine($"VTIME offset:     {VtimeOffset}  (c_cc base {CcBase} + idx {VtimeIdx})");
        Console.WriteLine();
    }

    private static void EnterRawMode()
    {
        var savedHandle = GCHandle.Alloc(SavedTermios, GCHandleType.Pinned);
        try
        {
            if (tcgetattr(StdInFd, savedHandle.AddrOfPinnedObject()) != 0)
                throw new InvalidOperationException("tcgetattr(stdin) failed — is stdin a TTY?");
        }
        finally
        {
            savedHandle.Free();
        }

        var initialVmin = SavedTermios[VminOffset];
        var initialVtime = SavedTermios[VtimeOffset];
        var initialOflag = SavedTermios[COflagOffset];
        PrintInfo($"Initial termios: c_oflag[byte0]=0x{initialOflag:X2}, VMIN={initialVmin}, VTIME={initialVtime}");

        var working = (byte[])SavedTermios.Clone();
        var workHandle = GCHandle.Alloc(working, GCHandleType.Pinned);
        try
        {
            var ptr = workHandle.AddrOfPinnedObject();
            cfmakeraw(ptr);
            // Re-enable OPOST so '\n' → '\r\n' translation still happens for our own probe output.
            working[COflagOffset] = (byte)(working[COflagOffset] | OpostBit);
            working[VminOffset] = 0;
            working[VtimeOffset] = 1;
            if (tcsetattr(StdInFd, Tcsanow, ptr) != 0)
                throw new InvalidOperationException("tcsetattr(stdin) failed.");

            // Read back what we set, to confirm offsets actually landed where we expected.
            var verify = new byte[TermiosBufferSize];
            var verifyHandle = GCHandle.Alloc(verify, GCHandleType.Pinned);
            try
            {
                tcgetattr(StdInFd, verifyHandle.AddrOfPinnedObject());
                var readbackOflag = verify[COflagOffset];
                var readbackVmin = verify[VminOffset];
                var readbackVtime = verify[VtimeOffset];
                PrintInfo($"After tcsetattr: c_oflag[byte0]=0x{readbackOflag:X2} (OPOST {(((readbackOflag & OpostBit) != 0) ? "ON" : "OFF")}), VMIN={readbackVmin}, VTIME={readbackVtime}");
                if (readbackVmin != 0 || readbackVtime != 1)
                {
                    PrintInfo("!! VMIN/VTIME readback does not match what was set — offsets are likely WRONG on this platform. Phase 2 should not ship until this matches.");
                }
                if ((readbackOflag & OpostBit) == 0)
                {
                    PrintInfo("!! OPOST is OFF after our set — c_oflag offset is likely WRONG.");
                }
            }
            finally
            {
                verifyHandle.Free();
            }
        }
        finally
        {
            workHandle.Free();
        }
        _rawModeEntered = true;
    }

    private static void RunLoop()
    {
        var buf = new byte[64];
        var bufHandle = GCHandle.Alloc(buf, GCHandleType.Pinned);
        try
        {
            while (true)
            {
                var n = read(StdInFd, bufHandle.AddrOfPinnedObject(), (nuint)buf.Length);
                if (n < 0)
                {
                    var err = Marshal.GetLastPInvokeError();
                    // EINTR (4) is fine — the read got interrupted by a signal; loop and try again.
                    if (err == 4) continue;
                    throw new InvalidOperationException($"read(stdin) failed: errno {err}");
                }
                if (n == 0)
                {
                    // VTIME expired with no input. Loop.
                    continue;
                }

                var bytes = new byte[n];
                Array.Copy(buf, bytes, n);
                PrintReadResult(bytes);

                // Quit shortcuts.
                if (bytes.Length == 1)
                {
                    if (bytes[0] == (byte)'q') { PrintInfo("'q' received — exiting."); return; }
                    if (bytes[0] == 0x03)      { PrintInfo("Ctrl+C (0x03) received — exiting."); return; }
                }
            }
        }
        finally
        {
            bufHandle.Free();
        }
    }

    private static void PrintReadResult(byte[] bytes)
    {
        var sb = new StringBuilder();
        sb.Append($"read() → {bytes.Length,2} byte(s): [");
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            var b = bytes[i];
            var ch = (b >= 0x20 && b < 0x7F) ? (char)b : '.';
            sb.Append($"0x{b:X2}({ch})");
        }
        sb.Append(']');

        var annotation = AnnotateSequence(bytes);
        if (annotation is not null) sb.Append("   → ").Append(annotation);
        Console.WriteLine(sb.ToString());
    }

    /// <summary>
    /// Recognizes the specific sequences this whole exercise is about, so the user can read
    /// success/failure at a glance without staring at hex.
    /// </summary>
    private static string? AnnotateSequence(byte[] b)
    {
        // CSI sequence: ESC [ ... <final byte in 0x40-0x7E>. Try to parse params + final.
        if (b.Length >= 3 && b[0] == 0x1B && b[1] == (byte)'[')
        {
            var final = b[^1];
            if (final >= 0x40 && final <= 0x7E)
            {
                var paramBytes = b.AsSpan(2, b.Length - 3);
                var paramStr = Encoding.ASCII.GetString(paramBytes);

                // CSI <num>;<mods>[;...] u  — kitty key event.
                if (final == (byte)'u')
                {
                    return $"CSI {paramStr} u   → KITTY KEY EVENT ({DecodeKittyKey(paramStr)})";
                }
                // CSI <num>;<mods> [ABCDEFHPQRS]  — kitty "second form" arrows / function keys
                // when at least one modifier is present (so the params appear).
                if (final is >= (byte)'A' and <= (byte)'H' or >= (byte)'P' and <= (byte)'S' && paramStr.Length > 0)
                {
                    return $"CSI {paramStr}{(char)final}   → KITTY FUNCTIONAL ({DecodeFunctional((char)final, paramStr)})";
                }
                // No-param CSI A/B/C/D etc. — either wheel under ?1007h OR plain arrow without kitty.
                if (paramStr.Length == 0)
                {
                    return (char)final switch
                    {
                        'A' => "CSI [A   (WHEEL UP under ?1007h, OR plain Up if no kitty/DECCKM off)",
                        'B' => "CSI [B   (WHEEL DOWN under ?1007h, OR plain Down if no kitty/DECCKM off)",
                        'C' => "CSI [C   (right arrow, DECCKM off, no kitty)",
                        'D' => "CSI [D   (left arrow, DECCKM off, no kitty)",
                        _ => $"CSI [{(char)final}   (unrecognized)",
                    };
                }
                if (b.Length >= 4 && b[2] == (byte)'M')
                    return "CSI [M ...  (X10 mouse report — ?1000h mouse tracking)";
                return $"CSI {paramStr}{(char)final}   (unrecognized CSI)";
            }
        }
        if (b.Length == 3 && b[0] == 0x1B && b[1] == (byte)'O')
        {
            return b[2] switch
            {
                (byte)'A' => "SS3 OA   (REAL UP ARROW under DECCKM, OR WHEEL UP under ?1007h+DECCKM — INDISTINGUISHABLE)",
                (byte)'B' => "SS3 OB   (REAL DOWN ARROW under DECCKM, OR WHEEL DOWN under ?1007h+DECCKM — INDISTINGUISHABLE)",
                (byte)'C' => "SS3 OC   (right arrow, DECCKM on)",
                (byte)'D' => "SS3 OD   (left arrow, DECCKM on)",
                _ => null,
            };
        }
        if (b.Length == 1 && b[0] == 0x1B)
            return "bare ESC";
        if (b.Length == 1 && b[0] == 0x03)
            return "Ctrl+C";
        if (b.Length == 1 && b[0] == 0x0D)
            return "Enter (CR)";
        if (b.Length == 1 && b[0] == 0x7F)
            return "Backspace (DEL)";
        if (b.Length == 1 && b[0] >= 0x20 && b[0] < 0x7F)
            return $"typed '{(char)b[0]}'";
        return null;
    }

    /// <summary>Decodes the params of a kitty CSI u event into a short human description.</summary>
    private static string DecodeKittyKey(string paramStr)
    {
        var fields = paramStr.Split(';');
        var keyField = fields.Length > 0 ? fields[0] : "";
        var modField = fields.Length > 1 ? fields[1] : "";
        var keycode = keyField.Split(':')[0];
        var modValue = modField.Split(':')[0];
        var keyName = KittyKeyName(keycode);
        var modName = ModName(modValue);
        return $"key={keycode}({keyName}) mods={modName}";
    }

    private static string DecodeFunctional(char final, string paramStr)
    {
        // CSI 1;<mods> A  — arrows + Home/End. Real-arrow encoding when kitty is on AND mods present.
        var fields = paramStr.Split(';');
        var modValue = fields.Length > 1 ? fields[1].Split(':')[0] : "1";
        var name = final switch
        {
            'A' => "Up arrow", 'B' => "Down arrow", 'C' => "Right arrow", 'D' => "Left arrow",
            'H' => "Home", 'F' => "End",
            'P' => "F1", 'Q' => "F2", 'R' => "F3", 'S' => "F4",
            _ => $"final={final}",
        };
        return $"{name} mods={ModName(modValue)}";
    }

    private static string KittyKeyName(string keycode)
    {
        if (!int.TryParse(keycode, out var n)) return "?";
        return n switch
        {
            // ASCII / common
            9 => "Tab", 13 => "Enter", 27 => "Esc", 32 => "Space", 127 => "Backspace",
            // Kitty functional PUA codepoints (subset)
            57344 => "Esc", 57345 => "Enter", 57346 => "Tab", 57347 => "Backspace",
            57352 => "Up", 57353 => "Down", 57354 => "Left", 57355 => "Right",
            57356 => "PageUp", 57357 => "PageDown", 57358 => "Home", 57359 => "End",
            // Modifier-alone keys (commonly seen with report_all_keys)
            57441 => "L-Shift", 57442 => "L-Ctrl", 57443 => "L-Alt", 57444 => "L-Super",
            57447 => "R-Shift", 57448 => "R-Ctrl", 57449 => "R-Alt", 57450 => "R-Super",
            // ASCII printable
            >= 32 and <= 126 => $"'{(char)n}'",
            _ => $"U+{n:X4}",
        };
    }

    private static string ModName(string modValue)
    {
        if (!int.TryParse(modValue, out var v) || v < 1) return "(none)";
        var bits = v - 1;
        if (bits == 0) return "none";
        var parts = new List<string>();
        if ((bits & 1) != 0) parts.Add("Shift");
        if ((bits & 2) != 0) parts.Add("Alt");
        if ((bits & 4) != 0) parts.Add("Ctrl");
        if ((bits & 8) != 0) parts.Add("Super");
        if ((bits & 16) != 0) parts.Add("Hyper");
        if ((bits & 32) != 0) parts.Add("Meta");
        if ((bits & 64) != 0) parts.Add("CapsLock");
        if ((bits & 128) != 0) parts.Add("NumLock");
        return string.Join("+", parts);
    }

    private static void PrintInfo(string msg) => Console.WriteLine($"[probe] {msg}");

    private static void Write(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        var h = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try { write(StdOutFd, h.AddrOfPinnedObject(), (nuint)bytes.Length); }
        finally { h.Free(); }
    }

    private static void SafeRestore()
    {
        if (_restored || !_rawModeEntered) return;
        _restored = true;
        var h = GCHandle.Alloc(SavedTermios, GCHandleType.Pinned);
        try { _ = tcsetattr(StdInFd, Tcsanow, h.AddrOfPinnedObject()); }
        catch { /* ignore */ }
        finally { h.Free(); }
    }

    private const string Libc = "libc";

    [DllImport(Libc, SetLastError = true)]
    private static extern int tcgetattr(int fd, IntPtr termios_p);

    [DllImport(Libc, SetLastError = true)]
    private static extern int tcsetattr(int fd, int optional_actions, IntPtr termios_p);

    [DllImport(Libc)]
    private static extern void cfmakeraw(IntPtr termios_p);

    [DllImport(Libc, SetLastError = true)]
    private static extern nint read(int fd, IntPtr buf, nuint count);

    [DllImport(Libc, SetLastError = true)]
    private static extern nint write(int fd, IntPtr buf, nuint count);
}
