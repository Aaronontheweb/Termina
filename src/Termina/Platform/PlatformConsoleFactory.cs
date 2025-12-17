// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Termina.Platform;

/// <summary>
/// Factory for creating platform-specific console implementations.
/// </summary>
public static class PlatformConsoleFactory
{
    /// <summary>
    /// Create the appropriate <see cref="IPlatformConsole"/> for the current platform.
    /// </summary>
    /// <returns>A platform-specific console implementation.</returns>
    /// <remarks>
    /// <para>Platform detection order:</para>
    /// <list type="number">
    /// <item><description>Windows: Uses P/Invoke for native console APIs</description></item>
    /// <item><description>Linux/macOS: Uses termios and POSIX signals</description></item>
    /// <item><description>Other: Falls back to polling-based implementation</description></item>
    /// </list>
    /// </remarks>
    public static IPlatformConsole Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Only use WindowsConsole if we have a real console (not redirected/piped)
            if (WindowsConsole.IsConsoleAvailable())
            {
                return new WindowsConsole();
            }
            // Fall back to polling-based console for piped/redirected scenarios
            return new FallbackConsole();
        }

        // Unix implementation deferred (issue #80) - current polling works fine on Linux
        // if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
        //     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        // {
        //     return new UnixConsole();
        // }

        return new FallbackConsole();
    }

    /// <summary>
    /// Create a fallback console implementation regardless of platform.
    /// Useful for testing or when native implementations have issues.
    /// </summary>
    /// <returns>A polling-based console implementation.</returns>
    public static IPlatformConsole CreateFallback() => new FallbackConsole();
}
