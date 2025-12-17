// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Diagnostics;

/// <summary>
/// Categories for Termina diagnostic trace output.
/// </summary>
[Flags]
public enum TerminaTraceCategory
{
    /// <summary>
    /// No categories enabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// Focus management operations (PushFocus, PopFocus, RouteInput).
    /// </summary>
    Focus = 1 << 0,

    /// <summary>
    /// Layout node lifecycle (OnActivate, OnDeactivate, Dispose).
    /// </summary>
    Layout = 1 << 1,

    /// <summary>
    /// Input routing and keystroke handling.
    /// </summary>
    Input = 1 << 2,

    /// <summary>
    /// Page lifecycle (OnNavigatedTo, OnNavigatingFrom, BuildLayout).
    /// </summary>
    Page = 1 << 3,

    /// <summary>
    /// Reactive subscription and observable emissions.
    /// </summary>
    Reactive = 1 << 4,

    /// <summary>
    /// Rendering and terminal output.
    /// </summary>
    Render = 1 << 5,

    /// <summary>
    /// Platform console operations.
    /// </summary>
    Platform = 1 << 6,

    /// <summary>
    /// All categories enabled.
    /// </summary>
    All = Focus | Layout | Input | Page | Reactive | Render | Platform
}
