// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Diagnostics;

/// <summary>
/// A deferred trace event that captures all context at creation time
/// but only formats the message string when actually needed.
/// </summary>
/// <remarks>
/// This struct-based approach ensures zero string allocations when tracing
/// is disabled. The message is only formatted when a listener calls
/// <see cref="FormatMessage"/>.
/// </remarks>
public readonly struct TraceEvent
{
    /// <summary>
    /// UTC timestamp when the event was created (as ticks for efficiency).
    /// </summary>
    public readonly long TimestampTicks;

    /// <summary>
    /// The severity level of this trace event.
    /// </summary>
    public readonly TerminaTraceLevel Level;

    /// <summary>
    /// The category this event belongs to.
    /// </summary>
    public readonly TerminaTraceCategory Category;

    /// <summary>
    /// The type name of the component that generated this event.
    /// </summary>
    public readonly string SourceType;

    /// <summary>
    /// Hash code of the source instance for tracking specific objects.
    /// </summary>
    public readonly int SourceHash;

    /// <summary>
    /// The message format template.
    /// </summary>
    public readonly string Template;

    private readonly object? _arg0;
    private readonly object? _arg1;
    private readonly object? _arg2;
    private readonly int _argCount;

    /// <summary>
    /// Create a trace event with no format arguments.
    /// </summary>
    public TraceEvent(
        TerminaTraceLevel level,
        TerminaTraceCategory category,
        string sourceType,
        int sourceHash,
        string template)
    {
        TimestampTicks = DateTime.UtcNow.Ticks;
        Level = level;
        Category = category;
        SourceType = sourceType;
        SourceHash = sourceHash;
        Template = template;
        _arg0 = null;
        _arg1 = null;
        _arg2 = null;
        _argCount = 0;
    }

    /// <summary>
    /// Create a trace event with one format argument.
    /// </summary>
    public TraceEvent(
        TerminaTraceLevel level,
        TerminaTraceCategory category,
        string sourceType,
        int sourceHash,
        string template,
        object? arg0)
    {
        TimestampTicks = DateTime.UtcNow.Ticks;
        Level = level;
        Category = category;
        SourceType = sourceType;
        SourceHash = sourceHash;
        Template = template;
        _arg0 = arg0;
        _arg1 = null;
        _arg2 = null;
        _argCount = 1;
    }

    /// <summary>
    /// Create a trace event with two format arguments.
    /// </summary>
    public TraceEvent(
        TerminaTraceLevel level,
        TerminaTraceCategory category,
        string sourceType,
        int sourceHash,
        string template,
        object? arg0,
        object? arg1)
    {
        TimestampTicks = DateTime.UtcNow.Ticks;
        Level = level;
        Category = category;
        SourceType = sourceType;
        SourceHash = sourceHash;
        Template = template;
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = null;
        _argCount = 2;
    }

    /// <summary>
    /// Create a trace event with three format arguments.
    /// </summary>
    public TraceEvent(
        TerminaTraceLevel level,
        TerminaTraceCategory category,
        string sourceType,
        int sourceHash,
        string template,
        object? arg0,
        object? arg1,
        object? arg2)
    {
        TimestampTicks = DateTime.UtcNow.Ticks;
        Level = level;
        Category = category;
        SourceType = sourceType;
        SourceHash = sourceHash;
        Template = template;
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = arg2;
        _argCount = 3;
    }

    /// <summary>
    /// Gets the timestamp as a DateTime.
    /// </summary>
    public DateTime Timestamp => new(TimestampTicks, DateTimeKind.Utc);

    /// <summary>
    /// Format the message by applying arguments to the template.
    /// Only call this when you actually need the formatted string.
    /// </summary>
    public string FormatMessage() => _argCount switch
    {
        0 => Template,
        1 => string.Format(Template, _arg0),
        2 => string.Format(Template, _arg0, _arg1),
        3 => string.Format(Template, _arg0, _arg1, _arg2),
        _ => Template
    };
}
