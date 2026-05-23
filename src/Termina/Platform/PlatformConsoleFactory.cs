// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Termina.Diagnostics;
using Termina.Hosting;

namespace Termina.Platform;

/// <summary>
/// Factory for creating platform-specific console implementations.
/// </summary>
public static class PlatformConsoleFactory
{
    // Dummy instance for trace logging (static class can't use 'this')
    private static readonly object TraceSource = new();

    /// <summary>
    /// Create the appropriate <see cref="IPlatformConsole"/> for the current platform.
    /// </summary>
    /// <returns>A platform-specific console implementation.</returns>
    /// <remarks>
    /// <para>Platform detection order:</para>
    /// <list type="number">
    /// <item><description>Windows: Uses P/Invoke for native console APIs. Raw-VT input is opt-in via <c>TERMINA_RAW_INPUT=1</c>.</description></item>
    /// <item><description>Linux/macOS: Uses termios and POSIX signals. Raw stdin is opt-in via <c>TERMINA_RAW_INPUT=1</c> (or the legacy <c>TERMINA_UNIX_RAW_INPUT</c>).</description></item>
    /// <item><description>Other: Falls back to polling-based implementation</description></item>
    /// </list>
    /// </remarks>
    public static IPlatformConsole Create(TerminaRuntimeOptions runtimeOptions)
    {
        ArgumentNullException.ThrowIfNull(runtimeOptions);

        TerminaTrace.Platform.Debug(TraceSource, "PlatformConsoleFactory.Create() called");
        TerminaTrace.Platform.Debug(TraceSource, "OS: {0}, Framework: {1}",
            RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TerminaTrace.Platform.Debug(TraceSource, "Detected Windows platform");

            // Only use WindowsConsole if we have a real console (not redirected/piped)
            var isConsoleAvailable = WindowsConsole.IsConsoleAvailable();
            TerminaTrace.Platform.Debug(TraceSource, "WindowsConsole.IsConsoleAvailable() = {0}", isConsoleAvailable);

            if (isConsoleAvailable)
            {
                var rawVt = ShouldUseRawInput(runtimeOptions);
                TerminaTrace.Platform.Info(TraceSource,
                    "Creating WindowsConsole (native P/Invoke, rawVtMode={0})", rawVt);
                return new WindowsConsole(rawVtMode: rawVt);
            }

            // Fall back to polling-based console for piped/redirected scenarios
            TerminaTrace.Platform.Info(TraceSource, "Creating FallbackConsole (console not available - piped/redirected?)");
            return new FallbackConsole();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // UnixConsole is opt-in until Phase 4 of the raw-stdin plan flips the default.
            // Set TERMINA_RAW_INPUT=1 to enable raw-byte stdin reads — required for ?1007h
            // wheel-scroll disambiguation from real arrow keys.
            if (ShouldUseRawInput(runtimeOptions))
            {
                if (UnixConsole.IsInteractiveStdin())
                {
                    TerminaTrace.Platform.Info(TraceSource, "Creating UnixConsole (raw input enabled)");
                    return new UnixConsole();
                }

                TerminaTrace.Platform.Warning(TraceSource,
                    "Raw input requested but stdin is not a TTY; falling back to FallbackConsole.");
            }
        }

        TerminaTrace.Platform.Info(TraceSource, "Creating FallbackConsole (non-Windows platform)");
        return new FallbackConsole();
    }

    /// <summary>
    /// Returns true if the caller opted into the raw-byte input pipeline via
    /// <c>TERMINA_RAW_INPUT</c> (canonical) or the legacy <c>TERMINA_UNIX_RAW_INPUT</c> alias.
    /// Setting either variable to <c>1</c> or <c>true</c> (case-insensitive) opts in.
    /// </summary>
    private static bool ShouldUseRawInput(TerminaRuntimeOptions runtimeOptions)
    {
        if (runtimeOptions.PreferRawInput)
            return true;

        var canonical = Environment.GetEnvironmentVariable("TERMINA_RAW_INPUT");
        if (IsTruthy(canonical)) return true;

        var legacy = Environment.GetEnvironmentVariable("TERMINA_UNIX_RAW_INPUT");
        if (IsTruthy(legacy))
        {
            TerminaTrace.Platform.Warning(TraceSource,
                "TERMINA_UNIX_RAW_INPUT is deprecated — use TERMINA_RAW_INPUT (cross-platform).");
            return true;
        }

        return false;
    }

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "1", StringComparison.Ordinal) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Create a fallback console implementation regardless of platform.
    /// Useful for testing or when native implementations have issues.
    /// </summary>
    /// <returns>A polling-based console implementation.</returns>
    public static IPlatformConsole CreateFallback() => new FallbackConsole();
}
