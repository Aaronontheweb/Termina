// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Termina.Platform;

/// <summary>
/// Single owner of process-global console environment configuration.
/// </summary>
internal static class ConsoleEnvironment
{
    /// <summary>
    /// Ensures the console uses UTF-8 output encoding so Unicode glyphs (box-drawing
    /// characters, symbols, emoji) render instead of being replaced with U+FFFD.
    /// Invoked once by the platform console during initialization; idempotent.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="Console.OutputEncoding"/> replaces <see cref="Console.Out"/>
    /// with a new <see cref="TextWriter"/> bound to the new encoding. Callers must not
    /// cache <see cref="Console.Out"/> across this call. See issue #204.
    /// </remarks>
    public static void EnsureUtf8Output()
    {
        Console.OutputEncoding = Encoding.UTF8;
    }
}
