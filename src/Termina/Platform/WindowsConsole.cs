// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
    public IObservable<ConsoleResizeEvent> Resized => _resized;

    /// <inheritdoc />
    public void Initialize()
    {
        if (_initialized) return;

        // Get console handles
        _inputHandle = GetStdHandle(STD_INPUT_HANDLE);
        _outputHandle = GetStdHandle(STD_OUTPUT_HANDLE);

        if (_inputHandle == IntPtr.Zero || _inputHandle == new IntPtr(-1))
        {
            throw new InvalidOperationException("Failed to get console input handle");
        }

        if (_outputHandle == IntPtr.Zero || _outputHandle == new IntPtr(-1))
        {
            throw new InvalidOperationException("Failed to get console output handle");
        }

        // Save original modes for restoration
        if (!GetConsoleMode(_inputHandle, out _originalInputMode))
        {
            throw new InvalidOperationException($"Failed to get console input mode: {Marshal.GetLastWin32Error()}");
        }

        if (!GetConsoleMode(_outputHandle, out _originalOutputMode))
        {
            throw new InvalidOperationException($"Failed to get console output mode: {Marshal.GetLastWin32Error()}");
        }

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

        if (!SetConsoleMode(_inputHandle, newInputMode))
        {
            throw new InvalidOperationException($"Failed to set console input mode: {Marshal.GetLastWin32Error()}");
        }

        // Configure output mode:
        // - Enable virtual terminal processing (VT100/ANSI sequences)
        var newOutputMode = _originalOutputMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        if (!SetConsoleMode(_outputHandle, newOutputMode))
        {
            // VT100 might not be supported on older Windows versions
            // Log error but continue - some terminals handle VT natively
            System.Diagnostics.Debug.WriteLine(
                $"Failed to enable VT100 processing: {Marshal.GetLastWin32Error()}");
        }

        // Ensure UTF-8 output encoding
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        _initialized = true;
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
            throw new InvalidOperationException("Console not initialized. Call Initialize() first.");
        }

        var inputRecords = new INPUT_RECORD[1];

        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait for input with timeout to allow cancellation checks
            var waitResult = WaitForSingleObject(_inputHandle, 50); // 50ms timeout

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (waitResult == WAIT_TIMEOUT)
            {
                // No input yet, check cancellation and continue waiting
                continue;
            }

            if (waitResult == WAIT_FAILED)
            {
                throw new InvalidOperationException($"WaitForSingleObject failed: {Marshal.GetLastWin32Error()}");
            }

            // Read the input record
            if (!ReadConsoleInputW(_inputHandle, inputRecords, 1, out var eventsRead) || eventsRead == 0)
            {
                continue;
            }

            var record = inputRecords[0];

            switch (record.EventType)
            {
                case KEY_EVENT:
                    // Only process key down events (ignore key up)
                    if (record.KeyEvent.bKeyDown != 0)
                    {
                        var keyEvent = ConvertKeyEvent(record.KeyEvent);
                        if (keyEvent.HasValue)
                        {
                            return keyEvent.Value;
                        }
                    }
                    break;

                case WINDOW_BUFFER_SIZE_EVENT:
                    var size = record.WindowBufferSizeEvent.dwSize;
                    var resizeEvent = new ConsoleResizeEvent(size.X, size.Y);
                    _resized.OnNext(resizeEvent);
                    return resizeEvent;

                case MOUSE_EVENT:
                    var mouseEvent = ConvertMouseEvent(record.MouseEvent);
                    if (mouseEvent.HasValue)
                    {
                        return mouseEvent.Value;
                    }
                    break;
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
