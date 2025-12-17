// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Termina.Diagnostics;

/// <summary>
/// Extension methods for configuring Termina diagnostic tracing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Default Behavior:</b> Tracing is completely disabled by default. When no listener
/// is configured, all trace calls are no-ops with minimal overhead (a single boolean check).
/// </para>
/// <para>
/// <b>Enabling Tracing:</b> Use one of the extension methods below to enable tracing
/// to a file, Microsoft.Extensions.Logging, or a custom listener.
/// </para>
/// <para>
/// <b>Categories:</b> Filter output by category (Focus, Layout, Input, Page, Reactive, Render, Platform)
/// using the <see cref="TerminaTraceCategory"/> flags enum. Default is <c>All</c>.
/// </para>
/// <para>
/// <b>Levels:</b> Filter output by severity (Trace, Debug, Info, Warning, Error) using
/// <see cref="TerminaTraceLevel"/>. Default is <c>Debug</c>.
/// </para>
/// <para>
/// <b>Usage Examples:</b>
/// </para>
/// <code>
/// // 1. File tracing - writes to a file with a lock-free channel design
/// services.AddTerminaFileTracing("termina-trace.log");
///
/// // 2. File tracing with category and level filtering
/// services.AddTerminaFileTracing(
///     "termina-trace.log",
///     TerminaTraceCategory.Focus | TerminaTraceCategory.Input,
///     TerminaTraceLevel.Debug);
///
/// // 3. M.E.Logging integration - routes to your configured logging providers
/// services.AddLogging(builder => builder.AddConsole())
///         .AddTerminaLoggerTracing();
///
/// // 4. Stderr output (useful for debugging without file I/O)
/// TerminaTrace.Configure(
///     FileTraceListener.CreateStdErr(),
///     TerminaTraceCategory.All,
///     TerminaTraceLevel.Debug);
///
/// // 5. Manual configuration without DI
/// var listener = new FileTraceListener("trace.log");
/// TerminaTrace.Configure(listener, TerminaTraceCategory.All, TerminaTraceLevel.Debug);
/// // ... when done:
/// await listener.DisposeAsync(); // Drains pending events before closing
/// </code>
/// <para>
/// <b>Output Format (FileTraceListener):</b>
/// </para>
/// <code>
/// 2024-01-15 10:30:45.123 [DEBUG] [Focus] FocusManager#12345678 - PushFocus: TextInputNode, stack depth=1
/// 2024-01-15 10:30:45.125 [TRACE] [Input] TextInputNode#87654321 - HandleInput: key=A, char='A'
/// </code>
/// </remarks>
public static class TerminaTraceExtensions
{
    /// <summary>
    /// Enable Termina tracing to a file.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="filePath">Path to the trace output file.</param>
    /// <param name="categories">Categories to enable (default: All).</param>
    /// <param name="minimumLevel">Minimum level to trace (default: Debug).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTerminaFileTracing(
        this IServiceCollection services,
        string filePath,
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug)
    {
        var listener = new FileTraceListener(filePath, categories, minimumLevel);
        TerminaTrace.Configure(listener, categories, minimumLevel);

        // Register for disposal
        services.AddSingleton<ITerminaTraceListener>(listener);

        return services;
    }

    /// <summary>
    /// Enable Termina tracing to Microsoft.Extensions.Logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="categories">Categories to enable (default: All).</param>
    /// <param name="minimumLevel">Minimum level to trace (default: Debug).</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This must be called after logging is configured. The listener will be
    /// created when the service provider is built.
    /// </remarks>
    public static IServiceCollection AddTerminaLoggerTracing(
        this IServiceCollection services,
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug)
    {
        services.AddSingleton<ITerminaTraceListener>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var listener = new LoggerTraceListener(loggerFactory, categories, minimumLevel);
            TerminaTrace.Configure(listener, categories, minimumLevel);
            return listener;
        });

        return services;
    }

    /// <summary>
    /// Enable Termina tracing to a custom listener.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="listener">The trace listener.</param>
    /// <param name="categories">Categories to enable (default: All).</param>
    /// <param name="minimumLevel">Minimum level to trace (default: Debug).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTerminaTracing(
        this IServiceCollection services,
        ITerminaTraceListener listener,
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug)
    {
        TerminaTrace.Configure(listener, categories, minimumLevel);
        services.AddSingleton(listener);
        return services;
    }
}
