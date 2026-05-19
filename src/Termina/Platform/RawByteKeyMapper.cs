// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Platform;

/// <summary>
/// Maps a single raw byte from a terminal's input stream into a <see cref="ConsoleKeyInfo"/>
/// shaped the same way <see cref="Console.ReadKey(bool)"/> would shape it.
/// </summary>
/// <remarks>
/// Used by both the Unix raw-stdin reader and the Windows raw-VT reader. The
/// <see cref="Input.EscapeSequenceParser"/> reassembles multi-byte escape sequences from the
/// resulting <see cref="ConsoleKeyEvent"/> stream — this mapper is only responsible for the
/// per-byte ASCII/control framing.
/// </remarks>
internal static class RawByteKeyMapper
{
    public static ConsoleKeyInfo ByteToKeyInfo(byte b) => b switch
    {
        0x08 => new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false),
        0x09 => new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false),
        0x0A => new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false),
        0x0D => new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false),
        0x1B => new ConsoleKeyInfo('\x1B', ConsoleKey.Escape, false, false, false),
        0x20 => new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false),
        0x7F => new ConsoleKeyInfo('\x7F', ConsoleKey.Backspace, false, false, false),
        // Ctrl+letter (0x01..0x1A excluding the special cases above) → A..Z + Control modifier.
        >= 0x01 and <= 0x1A => new ConsoleKeyInfo(
            (char)b, ConsoleKey.A + (b - 1), false, false, true),
        // Printable ASCII.
        >= 0x21 and <= 0x7E => new ConsoleKeyInfo(
            (char)b, MapPrintableToConsoleKey((char)b),
            shift: b >= 'A' && b <= 'Z', alt: false, control: false),
        _ => new ConsoleKeyInfo((char)b, ConsoleKey.None, false, false, false),
    };

    private static ConsoleKey MapPrintableToConsoleKey(char c) => c switch
    {
        >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
        >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
        >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
        _ => ConsoleKey.None,
    };
}
