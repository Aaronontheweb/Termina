// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Termina.Diagnostics;

/// <summary>
/// Trace listener that routes to Microsoft.Extensions.Logging.
/// </summary>
/// <remarks>
/// <para>
/// This adapter allows Termina diagnostic output to be captured by any
/// logging provider configured in your application (Serilog, NLog, file, etc.).
/// </para>
/// <para>
/// Log messages are written with the category "Termina.{Category}" and include
/// structured properties for component type and instance hash.
/// </para>
/// </remarks>
public sealed class LoggerTraceListener : ITerminaTraceListener
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<TerminaTraceCategory, ILogger> _loggers = new();
    private readonly TerminaTraceCategory _enabledCategories;
    private readonly TerminaTraceLevel _minimumLevel;

    /// <summary>
    /// Create a logger trace listener.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to create loggers from.</param>
    /// <param name="categories">Categories to enable (default: All).</param>
    /// <param name="minimumLevel">Minimum level to trace (default: Debug).</param>
    public LoggerTraceListener(
        ILoggerFactory loggerFactory,
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _enabledCategories = categories;
        _minimumLevel = minimumLevel;

        // Pre-create loggers for each category
        foreach (TerminaTraceCategory category in Enum.GetValues(typeof(TerminaTraceCategory)))
        {
            if (category != TerminaTraceCategory.None && category != TerminaTraceCategory.All)
            {
                _loggers[category] = loggerFactory.CreateLogger($"Termina.{category}");
            }
        }
    }

    /// <inheritdoc />
    public bool IsEnabled(TerminaTraceLevel level, TerminaTraceCategory category)
    {
        return level >= _minimumLevel
               && (_enabledCategories & category) != 0
               && _loggers.TryGetValue(category, out var logger)
               && logger.IsEnabled(ToLogLevel(level));
    }

    /// <inheritdoc />
    public void Write(in TraceEvent evt)
    {
        if (!IsEnabled(evt.Level, evt.Category))
            return;

        if (_loggers.TryGetValue(evt.Category, out var logger))
        {
            var logLevel = ToLogLevel(evt.Level);
            var message = evt.FormatMessage(); // Only format when actually logging

            logger.Log(logLevel, "[{SourceType}#{SourceHash:X8}] {Message}",
                evt.SourceType, evt.SourceHash, message);
        }
    }

    private static LogLevel ToLogLevel(TerminaTraceLevel level) => level switch
    {
        TerminaTraceLevel.Trace => LogLevel.Trace,
        TerminaTraceLevel.Debug => LogLevel.Debug,
        TerminaTraceLevel.Info => LogLevel.Information,
        TerminaTraceLevel.Warning => LogLevel.Warning,
        TerminaTraceLevel.Error => LogLevel.Error,
        _ => LogLevel.Debug
    };
}
