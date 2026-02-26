// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using R3;
using Termina.Diagnostics;

namespace Termina.Platform;

/// <summary>
/// Windows-specific console implementation using native Console APIs via P/Invoke.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides:
/// </para>
/// <list type="bullet">
/// <item><description>VT100/ANSI escape sequence processing via ENABLE_VIRTUAL_TERMINAL_PROCESSING</description></item>
/// <item><description>Event-driven input via ReadConsoleInputW (no polling)</description></item>
/// <item><description>Window resize events via WINDOW_BUFFER_SIZE_EVENT</description></item>
/// </list>
/// <para>
/// See: https://docs.microsoft.com/en-us/windows/console/console-functions
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsConsole : IPlatformConsole
{
    #region P/Invoke Constants

    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;

    // Console input mode flags
    private const uint ENABLE_PROCESSED_INPUT = 0x0001;
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;
    private const uint ENABLE_WINDOW_INPUT = 0x0008;
    private const uint ENABLE_MOUSE_INPUT = 0x0010;
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

    // Console output mode flags
    private const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
    private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    // Input record event types
    private const ushort KEY_EVENT = 0x0001;
    private const ushort MOUSE_EVENT = 0x0002;
    private const ushort WINDOW_BUFFER_SIZE_EVENT = 0x0004;

    // Wait constants
    private const uint WAIT_OBJECT_0 = 0x00000000;
    private const uint WAIT_TIMEOUT = 0x00000102;
    private const uint WAIT_FAILED = 0xFFFFFFFF;
    private const uint INFINITE = 0xFFFFFFFF;

    #endregion

    #region P/Invoke Structs

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public ushort wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_RECORD
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public KEY_EVENT_RECORD KeyEvent;
        [FieldOffset(4)] public MOUSE_EVENT_RECORD MouseEvent;
        [FieldOffset(4)] public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEY_EVENT_RECORD
    {
        public int bKeyDown;
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public char UnicodeChar;
        public uint dwControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSE_EVENT_RECORD
    {
        public COORD dwMousePosition;
        public uint dwButtonState;
        public uint dwControlKeyState;
        public uint dwEventFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOW_BUFFER_SIZE_RECORD
    {
        public COORD dwSize;
    }

    #endregion

    #region P/Invoke Methods

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleScreenBufferInfo(
        IntPtr hConsoleOutput,
        out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ReadConsoleInputW(
        IntPtr hConsoleInput,
        [Out] INPUT_RECORD[] lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetNumberOfConsoleInputEvents(
        IntPtr hConsoleInput,
        out uint lpcNumberOfEvents);

    #endregion

    #region Control Key State Flags

    private const uint RIGHT_ALT_PRESSED = 0x0001;
    private const uint LEFT_ALT_PRESSED = 0x0002;
    private const uint RIGHT_CTRL_PRESSED = 0x0004;
    private const uint LEFT_CTRL_PRESSED = 0x0008;
    private const uint SHIFT_PRESSED = 0x0010;

    #endregion

    private readonly Subject<ConsoleResizeEvent> _resized = new();
    private IntPtr _inputHandle;
    private IntPtr _outputHandle;
    private uint _originalInputMode;
    private uint _originalOutputMode;
    private bool _initialized;
    private bool _disposed;
    private int _lastWidth;
    private int _lastHeight;

    /// <summary>
    /// Check if a Windows console is available (not redirected/piped).
    /// </summary>
    /// <returns>True if a real console is available.</returns>
    public static bool IsConsoleAvailable()
    {
        var inputHandle = GetStdHandle(STD_INPUT_HANDLE);
        if (inputHandle == IntPtr.Zero || inputHandle == new IntPtr(-1))
            return false;

        // Try to get console mode - this fails if handle isn't a real console
        return GetConsoleMode(inputHandle, out _);
    }

    /// <inheritdoc />
    public bool SupportsEventDrivenInput => true;

    /// <inheritdoc />
    public Observable<ConsoleResizeEvent> Resized => _resized;

    /// <inheritdoc />
    public void Initialize()
    {
        if (_initialized) return;

        TerminaTrace.Platform.Debug(this, "WindowsConsole.Initialize() starting");

        // Get console handles
        _inputHandle = GetStdHandle(STD_INPUT_HANDLE);
        _outputHandle = GetStdHandle(STD_OUTPUT_HANDLE);

        TerminaTrace.Platform.Debug(this, "Console handles: input=0x{0:X}, output=0x{1:X}",
            _inputHandle.ToInt64(), _outputHandle.ToInt64());

        if (_inputHandle == IntPtr.Zero || _inputHandle == new IntPtr(-1))
        {
            TerminaTrace.Platform.Error(this, "Invalid input handle: 0x{0:X}", _inputHandle.ToInt64());
            throw new InvalidOperationException("Failed to get console input handle");
        }

        if (_outputHandle == IntPtr.Zero || _outputHandle == new IntPtr(-1))
        {
            TerminaTrace.Platform.Error(this, "Invalid output handle: 0x{0:X}", _outputHandle.ToInt64());
            throw new InvalidOperationException("Failed to get console output handle");
        }

        // Save original modes for restoration
        if (!GetConsoleMode(_inputHandle, out _originalInputMode))
        {
            var error = Marshal.GetLastWin32Error();
            TerminaTrace.Platform.Error(this, "GetConsoleMode(input) failed: error={0}", error);
            throw new InvalidOperationException($"Failed to get console input mode: {error}");
        }

        if (!GetConsoleMode(_outputHandle, out _originalOutputMode))
        {
            var error = Marshal.GetLastWin32Error();
            TerminaTrace.Platform.Error(this, "GetConsoleMode(output) failed: error={0}", error);
            throw new InvalidOperationException($"Failed to get console output mode: {error}");
        }

        TerminaTrace.Platform.Debug(this, "Original modes: input=0x{0:X4}, output=0x{1:X4}",
            _originalInputMode, _originalOutputMode);

        // Log which flags are currently set on output
        LogOutputModeFlags("Original output flags", _originalOutputMode);

        // Configure input mode:
        // - Enable window input events (for resize)
        // - Disable line input (get each key immediately)
        // - Disable echo (we render ourselves)
        // NOTE: Do NOT use ENABLE_VIRTUAL_TERMINAL_INPUT - it converts keys to VT sequences
        // which interferes with ReadConsoleInputW that expects KEY_EVENT records
        // NOTE: Keep ENABLE_PROCESSED_INPUT so Ctrl+C works as expected
        var newInputMode = (_originalInputMode | ENABLE_WINDOW_INPUT)
                          & ~ENABLE_LINE_INPUT
                          & ~ENABLE_ECHO_INPUT;

        TerminaTrace.Platform.Debug(this, "Setting input mode: 0x{0:X4} -> 0x{1:X4}",
            _originalInputMode, newInputMode);

        if (!SetConsoleMode(_inputHandle, newInputMode))
        {
            var error = Marshal.GetLastWin32Error();
            TerminaTrace.Platform.Error(this, "SetConsoleMode(input) failed: error={0}", error);
            throw new InvalidOperationException($"Failed to set console input mode: {error}");
        }

        // Verify input mode was set correctly
        if (GetConsoleMode(_inputHandle, out var verifyInputMode))
        {
            TerminaTrace.Platform.Debug(this, "Verified input mode: 0x{0:X4} (expected: 0x{1:X4})",
                verifyInputMode, newInputMode);
        }

        // Configure output mode:
        // - Enable virtual terminal processing (VT100/ANSI sequences)
        var newOutputMode = _originalOutputMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        TerminaTrace.Platform.Debug(this, "Setting output mode: 0x{0:X4} -> 0x{1:X4}",
            _originalOutputMode, newOutputMode);
        LogOutputModeFlags("New output flags", newOutputMode);

        if (!SetConsoleMode(_outputHandle, newOutputMode))
        {
            var error = Marshal.GetLastWin32Error();
            // VT100 might not be supported on older Windows versions
            // Log error but continue - some terminals handle VT natively
            TerminaTrace.Platform.Warning(this,
                "SetConsoleMode(output) failed for VT100: error={0} - ANSI sequences may not render", error);
            System.Diagnostics.Debug.WriteLine(
                $"Failed to enable VT100 processing: {error}");
        }
        else
        {
            TerminaTrace.Platform.Info(this, "VT100 processing enabled successfully");
        }

        // Verify output mode was set correctly
        if (GetConsoleMode(_outputHandle, out var verifyOutputMode))
        {
            TerminaTrace.Platform.Debug(this, "Verified output mode: 0x{0:X4} (expected: 0x{1:X4})",
                verifyOutputMode, newOutputMode);
            LogOutputModeFlags("Verified output flags", verifyOutputMode);

            // Critical check: is VT processing actually enabled?
            if ((verifyOutputMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == 0)
            {
                TerminaTrace.Platform.Error(this,
                    "CRITICAL: VT100 processing NOT enabled after SetConsoleMode succeeded!");
            }
        }

        // Ensure UTF-8 output encoding
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        TerminaTrace.Platform.Debug(this, "Set Console.OutputEncoding to UTF-8");

        // Capture initial window size for resize event deduplication
        var initialSize = GetSize();
        _lastWidth = initialSize.Width;
        _lastHeight = initialSize.Height;
        TerminaTrace.Platform.Debug(this, "Initial window size: {0}x{1}", _lastWidth, _lastHeight);

        _initialized = true;
        TerminaTrace.Platform.Info(this, "WindowsConsole.Initialize() completed successfully");
    }

    private void LogOutputModeFlags(string prefix, uint mode)
    {
        var flags = new System.Collections.Generic.List<string>();
        if ((mode & ENABLE_PROCESSED_OUTPUT) != 0) flags.Add("PROCESSED_OUTPUT");
        if ((mode & ENABLE_WRAP_AT_EOL_OUTPUT) != 0) flags.Add("WRAP_AT_EOL");
        if ((mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) != 0) flags.Add("VT_PROCESSING");
        if ((mode & DISABLE_NEWLINE_AUTO_RETURN) != 0) flags.Add("DISABLE_NEWLINE_AUTO");

        TerminaTrace.Platform.Debug(this, "{0}: [{1}]", prefix, string.Join(", ", flags));
    }

    /// <inheritdoc />
    public void Restore()
    {
        if (!_initialized) return;

        // Restore original console modes
        if (_inputHandle != IntPtr.Zero && _inputHandle != new IntPtr(-1))
        {
            SetConsoleMode(_inputHandle, _originalInputMode);
        }

        if (_outputHandle != IntPtr.Zero && _outputHandle != new IntPtr(-1))
        {
            SetConsoleMode(_outputHandle, _originalOutputMode);
        }

        _initialized = false;
    }

    /// <inheritdoc />
    public async ValueTask<IConsoleInputEvent?> ReadInputAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            TerminaTrace.Platform.Error(this, "ReadInputAsync called but console not initialized");
            throw new InvalidOperationException("Console not initialized. Call Initialize() first.");
        }

        // Simple blocking approach using Console.ReadKey
        // This is interrupt-driven - blocks until a key is available
        // The input task is separate from the main loop via the channel architecture,
        // so blocking here is correct behavior

        while (!cancellationToken.IsCancellationRequested)
        {
            // Check if a key is available without blocking
            if (Console.KeyAvailable)
            {
                // Key is available - read it immediately (no blocking)
                var key = Console.ReadKey(intercept: true);
                TerminaTrace.Platform.Trace(this, "Key pressed: {0}", key.Key);
                return new ConsoleKeyEvent(key);
            }

            // Check for window resize while we wait
            var currentSize = GetSize();
            if (currentSize.Width != _lastWidth || currentSize.Height != _lastHeight)
            {
                _lastWidth = currentSize.Width;
                _lastHeight = currentSize.Height;
                TerminaTrace.Platform.Debug(this, "Window resized: {0}x{1}", _lastWidth, _lastHeight);
                var resizeEvent = new ConsoleResizeEvent(_lastWidth, _lastHeight);
                _resized.OnNext(resizeEvent);
                return resizeEvent;
            }

            // Brief yield to allow cancellation and other async work
            // Using 1ms delay for minimal latency while still allowing cancellation
            try
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public (int Width, int Height) GetSize()
    {
        if (_outputHandle == IntPtr.Zero || _outputHandle == new IntPtr(-1))
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }

        if (GetConsoleScreenBufferInfo(_outputHandle, out var info))
        {
            // Use the window size, not the buffer size
            var width = info.srWindow.Right - info.srWindow.Left + 1;
            var height = info.srWindow.Bottom - info.srWindow.Top + 1;
            return (width, height);
        }

        return (Console.WindowWidth, Console.WindowHeight);
    }

    private static ConsoleKeyEvent? ConvertKeyEvent(KEY_EVENT_RECORD keyEvent)
    {
        var key = (ConsoleKey)keyEvent.wVirtualKeyCode;
        var ch = keyEvent.UnicodeChar;
        var state = keyEvent.dwControlKeyState;

        // Build modifiers
        var shift = (state & SHIFT_PRESSED) != 0;
        var alt = (state & (LEFT_ALT_PRESSED | RIGHT_ALT_PRESSED)) != 0;
        var control = (state & (LEFT_CTRL_PRESSED | RIGHT_CTRL_PRESSED)) != 0;

        // Filter out modifier-only key presses and other non-character keys we don't care about
        if (key is ConsoleKey.LeftWindows or ConsoleKey.RightWindows or
            ConsoleKey.Applications or ConsoleKey.Sleep or
            ConsoleKey.NoName)
        {
            return null;
        }

        // Handle pure modifier key presses (Shift, Ctrl, Alt alone)
        if (key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow or
            ConsoleKey.UpArrow or ConsoleKey.DownArrow or
            ConsoleKey.Home or ConsoleKey.End or
            ConsoleKey.PageUp or ConsoleKey.PageDown or
            ConsoleKey.Insert or ConsoleKey.Delete or
            ConsoleKey.Enter or ConsoleKey.Tab or
            ConsoleKey.Backspace or ConsoleKey.Escape or
            ConsoleKey.Spacebar)
        {
            // Navigation and special keys - always include
        }
        else if (key >= ConsoleKey.F1 && key <= ConsoleKey.F24)
        {
            // Function keys - always include
        }
        else if (ch == '\0' && !control && !alt)
        {
            // No character and no modifiers - skip (modifier key by itself)
            // Virtual key codes: VK_SHIFT=0x10, VK_CONTROL=0x11, VK_MENU(Alt)=0x12
            var vk = keyEvent.wVirtualKeyCode;
            if (vk is 0x10 or 0x11 or 0x12 or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5)
            {
                // Shift, Control, Alt, and their left/right variants
                return null;
            }
        }

        var keyInfo = new ConsoleKeyInfo(ch, key, shift, alt, control);
        return new ConsoleKeyEvent(keyInfo);
    }

    private static ConsoleMouseEvent? ConvertMouseEvent(MOUSE_EVENT_RECORD mouseEvent)
    {
        // For now, we don't handle mouse events extensively
        // This can be expanded later if needed
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            Restore();
        }
        catch
        {
            // Ignore errors during cleanup
        }

        _resized.OnCompleted();
        _resized.Dispose();
    }
}
