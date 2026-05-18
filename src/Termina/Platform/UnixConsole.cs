// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using R3;
using Termina.Diagnostics;

namespace Termina.Platform;

/// <summary>
/// Unix/macOS console implementation that reads raw bytes from stdin after putting the
/// terminal into raw mode via termios.
/// </summary>
/// <remarks>
/// <para>
/// The native <see cref="Console.ReadKey(bool)"/> path on Unix uses an internal
/// <c>StdInReader</c> that recognizes both CSI (<c>ESC [ A</c>) and SS3 (<c>ESC O A</c>) arrow
/// forms as <see cref="ConsoleKey.UpArrow"/> regardless of the terminal's DECCKM state. That
/// makes it impossible to distinguish a real arrow keypress from a mouse-wheel tick delivered
/// under xterm alternate-scroll mode (<c>?1007h</c>), because both sequences are folded into the
/// same <see cref="ConsoleKeyInfo"/>.
/// </para>
/// <para>
/// This implementation bypasses <c>Console.ReadKey</c> entirely: it enters raw mode with
/// <c>cfmakeraw</c> + <c>VMIN=0</c> / <c>VTIME=1</c>, reads bytes from <c>Console.OpenStandardInput</c>,
/// and emits one <see cref="ConsoleKeyEvent"/> per byte. The existing
/// <see cref="Input.EscapeSequenceParser"/> handles multi-byte escape sequences (CSI mouse,
/// bracketed paste, CSI/SS3 arrow keys).
/// </para>
/// </remarks>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class UnixConsole : IPlatformConsole
{
    private const int StdInFd = 0;
    private const int Tcsanow = 0;
    // termios struct differs in size between Linux (~60 B) and macOS (~72 B). Use a generous
    // opaque buffer; cfmakeraw / tcsetattr only touch / read the bytes they need.
    private const int TermiosBufferSize = 256;
    // Byte offsets of c_cc[VMIN] / c_cc[VTIME] inside the termios struct.
    //   Linux:   4*tcflag_t (4 B each) + c_line (1 B) = 17 → c_cc base 17; VTIME=5, VMIN=6.
    //   macOS:   4*tcflag_t (8 B each)                = 32 → c_cc base 32; VMIN=16, VTIME=17.
    // See sys/termios.h on each platform.
    private static int VminOffset => OperatingSystem.IsMacOS() ? 32 + 16 : 17 + 6;
    private static int VtimeOffset => OperatingSystem.IsMacOS() ? 32 + 17 : 17 + 5;
    // c_oflag is the second tcflag_t in the struct (4 B on Linux, 8 B on macOS). OPOST is the
    // low bit on both platforms; we re-enable it after cfmakeraw so '\n' → '\r\n' translation
    // still works for normal logging / stack traces. Without this, post-shutdown logs print
    // as staircase output.
    private static int COflagOffset => OperatingSystem.IsMacOS() ? 8 : 4;
    private const byte OpostBit = 0x01;

    private readonly Subject<ConsoleResizeEvent> _resized = new();
    private readonly byte[] _savedTermios = new byte[TermiosBufferSize];
    private readonly byte[] _readBuf = new byte[256];
    private int _bufPos;
    private int _bufLen;
    private Stream? _stdin;
    private bool _rawModeEntered;
    private bool _initialized;
    private bool _disposed;
    private int _lastWidth;
    private int _lastHeight;
    private EventHandler? _processExitHandler;

    /// <inheritdoc />
    public bool SupportsEventDrivenInput => true;

    /// <inheritdoc />
    public Observable<ConsoleResizeEvent> Resized => _resized;

    /// <inheritdoc />
    public void Initialize()
    {
        if (_initialized) return;

        ConsoleEnvironment.EnsureUtf8Output();

        EnterRawMode();
        _stdin = Console.OpenStandardInput();

        try { _lastWidth = Console.WindowWidth; _lastHeight = Console.WindowHeight; }
        catch (IOException) { _lastWidth = 80; _lastHeight = 24; }

        // Best-effort restore on abnormal exit. We can't guarantee this runs for SIGKILL or
        // for crashes in native code, but it covers the common normal-shutdown / unhandled
        // exception cases so we don't leave the user's terminal in raw mode.
        _processExitHandler = (_, _) =>
        {
            try { Restore(); } catch { /* ignore */ }
        };
        AppDomain.CurrentDomain.ProcessExit += _processExitHandler;

        _initialized = true;
        TerminaTrace.Platform.Info(this, "UnixConsole initialized (raw mode entered)");
    }

    /// <inheritdoc />
    public void Restore()
    {
        if (!_rawModeEntered) return;
        var handle = GCHandle.Alloc(_savedTermios, GCHandleType.Pinned);
        try
        {
            _ = tcsetattr(StdInFd, Tcsanow, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
        _rawModeEntered = false;
        TerminaTrace.Platform.Debug(this, "UnixConsole termios restored");
    }

    /// <inheritdoc />
    public async ValueTask<IConsoleInputEvent?> ReadInputAsync(CancellationToken cancellationToken)
    {
        if (_stdin is null) throw new InvalidOperationException("Initialize() must be called first.");

        while (!cancellationToken.IsCancellationRequested)
        {
            // Drain anything already buffered from a previous read syscall — one byte per event.
            if (_bufPos < _bufLen)
            {
                var b = _readBuf[_bufPos++];
                return new ConsoleKeyEvent(ByteToKeyInfo(b));
            }

            // Polling-based resize detection. SIGWINCH support is deferred (issue #80 follow-up).
            var resize = CheckForResize();
            if (resize.HasValue) return resize.Value;

            int n;
            try
            {
                // VTIME=1 (100 ms) lets the syscall return so we can check cancellation/resize
                // even when the user is idle.
                n = await _stdin.ReadAsync(_readBuf.AsMemory(0, _readBuf.Length), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            _bufPos = 0;
            _bufLen = n;
            // n == 0 means VTIME expired with no input — loop and re-check cancellation/resize.
        }

        return null;
    }

    /// <inheritdoc />
    public (int Width, int Height) GetSize()
    {
        try { return (Console.WindowWidth, Console.WindowHeight); }
        catch (IOException) { return (_lastWidth, _lastHeight); }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_processExitHandler is not null)
        {
            AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
            _processExitHandler = null;
        }

        try { Restore(); } catch { /* ignore */ }

        _resized.OnCompleted();
        _resized.Dispose();
    }

    private ConsoleResizeEvent? CheckForResize()
    {
        try
        {
            var w = Console.WindowWidth;
            var h = Console.WindowHeight;
            if (w == _lastWidth && h == _lastHeight) return null;
            _lastWidth = w; _lastHeight = h;
            var evt = new ConsoleResizeEvent(w, h);
            _resized.OnNext(evt);
            return evt;
        }
        catch (IOException) { return null; }
    }

    private void EnterRawMode()
    {
        var savedHandle = GCHandle.Alloc(_savedTermios, GCHandleType.Pinned);
        try
        {
            if (tcgetattr(StdInFd, savedHandle.AddrOfPinnedObject()) != 0)
                throw new InvalidOperationException("tcgetattr(stdin) failed; not a TTY?");
        }
        finally
        {
            savedHandle.Free();
        }

        var working = (byte[])_savedTermios.Clone();
        var workHandle = GCHandle.Alloc(working, GCHandleType.Pinned);
        try
        {
            var ptr = workHandle.AddrOfPinnedObject();
            cfmakeraw(ptr);
            // Preserve OPOST so '\n' → '\r\n' translation still happens on output. cfmakeraw
            // clears it, which would turn any post-raw logging into staircase text.
            working[COflagOffset] = (byte)(working[COflagOffset] | OpostBit);
            // VMIN=0, VTIME=1 → read() returns after 100 ms with whatever bytes arrived (possibly 0).
            working[VminOffset] = 0;
            working[VtimeOffset] = 1;
            if (tcsetattr(StdInFd, Tcsanow, ptr) != 0)
                throw new InvalidOperationException("tcsetattr(stdin) failed.");
        }
        finally
        {
            workHandle.Free();
        }
        _rawModeEntered = true;
    }

    /// <summary>
    /// Maps a single raw byte from stdin into a <see cref="ConsoleKeyInfo"/> shaped the same way
    /// <see cref="Console.ReadKey(bool)"/> would shape it for that byte. The
    /// <see cref="Input.EscapeSequenceParser"/> reassembles multi-byte escape sequences from the
    /// resulting <see cref="ConsoleKeyEvent"/> stream.
    /// </summary>
    internal static ConsoleKeyInfo ByteToKeyInfo(byte b) => b switch
    {
        0x08 => new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false),
        0x09 => new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false),
        0x0A => new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false),
        0x0D => new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false),
        0x1B => new ConsoleKeyInfo('\x1B', ConsoleKey.Escape, false, false, false),
        0x20 => new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false),
        0x7F => new ConsoleKeyInfo('\x7F', ConsoleKey.Backspace, false, false, false),
        // Ctrl+letter (0x01..0x1A excluding the special cases above) → ConsoleKey.A..Z + Control modifier.
        >= 0x01 and <= 0x1A => new ConsoleKeyInfo(
            (char)b, ConsoleKey.A + (b - 1), false, false, true),
        // Printable ASCII.
        >= 0x21 and <= 0x7E => new ConsoleKeyInfo(
            (char)b, MapPrintableToConsoleKey((char)b),
            shift: b >= 'A' && b <= 'Z', alt: false, control: false),
        // Anything else (incl. high-bit UTF-8 continuation bytes) → pass through; downstream
        // text-input handling treats KeyChar verbatim. Full multi-byte UTF-8 decoding is a
        // future enhancement.
        _ => new ConsoleKeyInfo((char)b, ConsoleKey.None, false, false, false)
    };

    private static ConsoleKey MapPrintableToConsoleKey(char c) => c switch
    {
        >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
        >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
        >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
        _ => ConsoleKey.None
    };

    // libc is available as "libc" on both glibc/musl Linux and macOS (resolves to libSystem.B.dylib).
    private const string Libc = "libc";

    [DllImport(Libc, SetLastError = true)]
    private static extern int tcgetattr(int fd, IntPtr termios_p);

    [DllImport(Libc, SetLastError = true)]
    private static extern int tcsetattr(int fd, int optional_actions, IntPtr termios_p);

    [DllImport(Libc)]
    private static extern void cfmakeraw(IntPtr termios_p);
}
