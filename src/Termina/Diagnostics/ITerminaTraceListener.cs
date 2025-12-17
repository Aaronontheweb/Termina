// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Diagnostics;

/// <summary>
/// Interface for receiving Termina diagnostic trace output.
/// </summary>
/// <remarks>
/// Implement this interface to create custom trace listeners that route
/// Termina diagnostics to your preferred logging infrastructure.
/// </remarks>
public interface ITerminaTraceListener
{
    /// <summary>
    /// Write a trace event.
    /// </summary>
    /// <param name="evt">The trace event. Call <see cref="TraceEvent.FormatMessage"/>
    /// to get the formatted message string.</param>
    void Write(in TraceEvent evt);

    /// <summary>
    /// Check if a specific category and level combination is enabled.
    /// </summary>
    /// <param name="level">The severity level to check.</param>
    /// <param name="category">The category to check.</param>
    /// <returns>True if tracing is enabled for this level and category.</returns>
    bool IsEnabled(TerminaTraceLevel level, TerminaTraceCategory category);
}
