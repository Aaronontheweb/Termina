// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Diagnostics;

/// <summary>
/// Log levels for Termina diagnostic trace output.
/// </summary>
public enum TerminaTraceLevel
{
    /// <summary>
    /// Most detailed level - high frequency events like cursor blinks, render cycles.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Debug information useful during development.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Informational messages about normal operation.
    /// </summary>
    Info = 2,

    /// <summary>
    /// Warning conditions that don't prevent operation.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error conditions that may affect operation.
    /// </summary>
    Error = 4
}
