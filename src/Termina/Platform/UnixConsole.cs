// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Channels;
using R3;
using Termina.Diagnostics;

namespace Termina.Platform;

/// <summary>
/// Unix/macOS console implementation that bypasses <see cref="Console.ReadKey(bool)"/> by reading
/// raw bytes directly from stdin after putting the terminal into raw mode via termios.
/// </summary>
/// <remarks>
/// <para>
/// .NET's internal <c>StdInReader</c> on Unix has hardcoded recognition for both CSI
/// (<c>ESC [ A</c>) and SS3 (<c>ESC O A</c>) arrow forms as <see cref="ConsoleKey.UpArrow"/>
/// regardless of the terminal's DECCKM state. That makes it impossible to distinguish a real
/// arrow keypress from a mouse-wheel tick delivered under xterm alternate-scroll mode
/// (<c>?1007h</c>), because both sequences fold into the same <see cref="ConsoleKeyInfo"/>.
/// </para>
/// <para>
/// This implementation:
/// </para>
/// <list type="bullet">
/// <item><description>Enters raw mode with <c>cfmakeraw</c> + <c>VMIN=0</c>/<c>VTIME=1</c>,
/// re-enabling <c>OPOST</c> so post-shutdown logging isn't staircased.</description></item>
/// <item><description>Spawns a dedicated background thread that calls libc <c>read()</c>
/// directly (no <see cref="Stream.ReadAsync(byte[], int, int)"/> indirection) and pushes
/// <see cref="ConsoleKeyEvent"/>s to a <see cref="Channel{T}"/>.</description></item>
/// <item><description>UTF-8 multi-byte input is reassembled via <see cref="Decoder"/>; ASCII
/// bytes (including all escape-sequence bytes) flow through one-per-event so the existing
/// <see cref="Input.EscapeSequenceParser"/> can reassemble multi-byte escape sequences.</description></item>
/// <item><description>Restores termios on <see cref="Dispose"/>, <see cref="AppDomain.ProcessExit"/>,
/// unhandled exception, and <see cref="Console.CancelKeyPress"/>.</description></item>
/// </list>
/// <para>
/// Resize detection is currently polling-based (matches <see cref="FallbackConsole"/>); SIGWINCH
/// integration is a follow-up.
/// </para>
/// </remarks>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class UnixConsole : IPlatformConsole
{
    private const int StdInFd = 0;
    private const int Tcsanow = 0;
    private const int TermiosBufferSize = 256;

    // termios byte offsets — same constants verified by Termina.Demo.RawStdinProbe.
    //   Linux:  4 × tcflag_t (4 B each) + c_line (1 B)  → c_cc base 17; VTIME=5, VMIN=6.
    //   macOS:  4 × tcflag_t (8 B each)                  → c_cc base 32; VMIN=16, VTIME=17.
    // c_oflag is the second tcflag_t; OPOST is bit 0x01 on both platforms.
    private static int CcBase => OperatingSystem.IsMacOS() ? 32 : 17;
    private static int VminIdx => OperatingSystem.IsMacOS() ? 16 : 6;
    private static int VtimeIdx => OperatingSystem.IsMacOS() ? 17 : 5;
    private static int VminOffset => CcBase + VminIdx;
    private static int VtimeOffset => CcBase + VtimeIdx;
    private static int COflagOffset => OperatingSystem.IsMacOS() ? 8 : 4;
    private const byte OpostBit = 0x01;

    private readonly Subject<ConsoleResizeEvent> _resized = new();
    private readonly byte[] _savedTermios = new byte[TermiosBufferSize];
    private readonly Channel<IConsoleInputEvent> _events =
        Channel.CreateUnbounded<IConsoleInputEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });

    private CancellationTokenSource? _stopCts;
    private Thread? _readerThread;
    private bool _rawModeEntered;
    private bool _initialized;
    private bool _disposed;
    private bool _restored;
    private int _lastWidth;
    private int _lastHeight;

    private EventHandler? _processExitHandler;
    private UnhandledExceptionEventHandler? _unhandledExceptionHandler;
    private ConsoleCancelEventHandler? _cancelKeyPressHandler;
    private PosixSignalRegistration? _sigwinchRegistration;

    /// <inheritdoc />
    public TerminalCapabilities Capabilities => new(RawInputActive: true);

    /// <summary>
    /// Returns true when stdin is an interactive TTY.
    /// </summary>
    public static bool IsInteractiveStdin()
    {
        if (Console.IsInputRedirected)
            return false;

        try
        {
            return isatty(StdInFd) == 1;
        }
        catch
        {
            return false;
        }
    }

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

        try { _lastWidth = Console.WindowWidth; _lastHeight = Console.WindowHeight; }
        catch (IOException) { _lastWidth = 80; _lastHeight = 24; }

        _processExitHandler = (_, _) => SafeRestore();
        _unhandledExceptionHandler = (_, _) => SafeRestore();
        _cancelKeyPressHandler = (_, e) =>
        {
            // With ISIG cleared by cfmakeraw, this only fires before EnterRawMode or after
            // Restore — both rare. Belt-and-suspenders restoration.
            SafeRestore();
            e.Cancel = false;
        };
        AppDomain.CurrentDomain.ProcessExit += _processExitHandler;
        AppDomain.CurrentDomain.UnhandledException += _unhandledExceptionHandler;
        Console.CancelKeyPress += _cancelKeyPressHandler;

        // Event-driven resize via SIGWINCH. The handler runs on a thread-pool thread, so it
        // safely posts a ConsoleResizeEvent into the same channel the reader thread writes to.
        try
        {
            _sigwinchRegistration = PosixSignalRegistration.Create(
                PosixSignal.SIGWINCH, _ => CheckResize());
        }
        catch (PlatformNotSupportedException)
        {
            TerminaTrace.Platform.Info(this, "SIGWINCH registration not supported — falling back to VTIME-tick resize polling.");
        }

        _stopCts = new CancellationTokenSource();
        _readerThread = new Thread(ReaderLoop)
        {
            IsBackground = true,
            Name = "Termina.UnixConsole.Reader",
        };
        _readerThread.Start();

        _initialized = true;
        TerminaTrace.Platform.Info(this, "UnixConsole initialized (raw mode entered)");
    }

    /// <inheritdoc />
    public void Restore() => SafeRestore();

    /// <inheritdoc />
    public async ValueTask<IConsoleInputEvent?> ReadInputAsync(CancellationToken cancellationToken)
    {
        if (!_initialized) throw new InvalidOperationException("Initialize() must be called first.");
        try
        {
            return await _events.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ChannelClosedException)
        {
            return null;
        }
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

        _stopCts?.Cancel();
        try { _readerThread?.Join(TimeSpan.FromMilliseconds(250)); } catch { /* ignore */ }

        if (_processExitHandler is not null)
            AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
        if (_unhandledExceptionHandler is not null)
            AppDomain.CurrentDomain.UnhandledException -= _unhandledExceptionHandler;
        if (_cancelKeyPressHandler is not null)
            Console.CancelKeyPress -= _cancelKeyPressHandler;
        _sigwinchRegistration?.Dispose();

        SafeRestore();

        _events.Writer.TryComplete();
        _resized.OnCompleted();
        _resized.Dispose();
        _stopCts?.Dispose();
    }

    private void EnterRawMode()
    {
        var savedHandle = GCHandle.Alloc(_savedTermios, GCHandleType.Pinned);
        try
        {
            if (tcgetattr(StdInFd, savedHandle.AddrOfPinnedObject()) != 0)
                throw new InvalidOperationException("tcgetattr(stdin) failed — is stdin a TTY?");
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
            // Re-enable OPOST so '\n' → '\r\n' translation still happens for our own output
            // (logs, stack traces). cfmakeraw clears it, which would staircase output.
            working[COflagOffset] = (byte)(working[COflagOffset] | OpostBit);
            // VMIN=0, VTIME=1 → read() returns after 100 ms with whatever bytes arrived (or 0).
            // The 100 ms tick doubles as our resize-polling cadence in the reader loop.
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

    private void SafeRestore()
    {
        if (_restored || !_rawModeEntered) return;
        _restored = true;
        var h = GCHandle.Alloc(_savedTermios, GCHandleType.Pinned);
        try { _ = tcsetattr(StdInFd, Tcsanow, h.AddrOfPinnedObject()); }
        catch { /* ignore */ }
        finally { h.Free(); }
        TerminaTrace.Platform.Debug(this, "UnixConsole termios restored");
    }

    private void ReaderLoop()
    {
        var stopCt = _stopCts!.Token;
        var buf = new byte[256];
        var chars = new char[2];
        var oneByte = new byte[1];
        var decoder = Encoding.UTF8.GetDecoder();
        var bufHandle = GCHandle.Alloc(buf, GCHandleType.Pinned);
        try
        {
            while (!stopCt.IsCancellationRequested)
            {
                nint n;
                try
                {
                    n = read(StdInFd, bufHandle.AddrOfPinnedObject(), (nuint)buf.Length);
                }
                catch (Exception ex)
                {
                    TerminaTrace.Platform.Error(this, "read(stdin) threw: {0}", ex.Message);
                    break;
                }

                if (n < 0)
                {
                    var err = Marshal.GetLastPInvokeError();
                    if (err == 4) continue; // EINTR — interrupted by a signal; retry.
                    TerminaTrace.Platform.Error(this, "read(stdin) failed: errno {0}", err);
                    break;
                }

                if (n == 0)
                {
                    // VTIME expired with no input — opportunistic resize poll.
                    CheckResize();
                    continue;
                }

                for (var i = 0; i < (int)n; i++)
                {
                    var b = buf[i];
                    if (b < 0x80)
                    {
                        // ASCII / control / escape-sequence bytes — emit verbatim, one event each.
                        _events.Writer.TryWrite(new ConsoleKeyEvent(RawByteKeyMapper.ByteToKeyInfo(b)));
                    }
                    else
                    {
                        // UTF-8 continuation/lead — feed decoder. Emits 0, 1, or 2 chars
                        // (the latter for surrogate-pair codepoints).
                        oneByte[0] = b;
                        var charCount = decoder.GetChars(oneByte, 0, 1, chars, 0);
                        for (var c = 0; c < charCount; c++)
                        {
                            _events.Writer.TryWrite(new ConsoleKeyEvent(
                                new ConsoleKeyInfo(chars[c], ConsoleKey.None, false, false, false)));
                        }
                    }
                }

                CheckResize();
            }
        }
        finally
        {
            bufHandle.Free();
        }
    }

    private readonly object _resizeLock = new();

    private void CheckResize()
    {
        int w, h;
        try { w = Console.WindowWidth; h = Console.WindowHeight; }
        catch (IOException) { return; }

        lock (_resizeLock)
        {
            if (w == _lastWidth && h == _lastHeight) return;
            _lastWidth = w; _lastHeight = h;
        }

        var evt = new ConsoleResizeEvent(w, h);
        try { _resized.OnNext(evt); } catch { /* ignore */ }
        _events.Writer.TryWrite(evt);
    }

    // libc resolves to libSystem.B.dylib on macOS and to libc.so.6 (glibc) / libc.musl-*.so on Linux.
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
    private static extern int isatty(int fd);
}
