// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Termina.Diagnostics;

/// <summary>
/// Static entry point for Termina diagnostic trace logging.
/// </summary>
/// <remarks>
/// <para>
/// <b>Overview:</b> TerminaTrace provides lightweight, optional diagnostic output for debugging
/// TUI applications. It uses a static API to avoid forcing dependency injection
/// into all components while still supporting integration with Microsoft.Extensions.Logging.
/// </para>
/// <para>
/// <b>Default Behavior:</b> Tracing is completely disabled by default. When no listener
/// is configured via <see cref="Configure"/>:
/// <list type="bullet">
/// <item>All trace calls are no-ops (single inlined boolean check)</item>
/// <item>No string formatting occurs</item>
/// <item>No memory allocations occur</item>
/// <item>No TraceEvent structs are created</item>
/// </list>
/// </para>
/// <para>
/// <b>Deferred Formatting:</b> All trace methods use template-based string formatting.
/// The message is only formatted when tracing is enabled and the listener needs the string.
/// This ensures zero allocation overhead when disabled.
/// </para>
/// <para>
/// <b>Architecture:</b> The system uses a <see cref="TraceEvent"/> struct to capture
/// timestamp, level, category, source type, and instance hash at creation time.
/// The message template and arguments are stored but not formatted until
/// <see cref="TraceEvent.FormatMessage"/> is called by the listener.
/// </para>
/// <para>
/// <b>Lock-Free Design:</b> The <see cref="FileTraceListener"/> uses a channel-based
/// design where trace calls push events to an unbounded channel (non-blocking), and
/// a background task consumes them. This ensures tracing cannot become a global lock
/// for the UI thread.
/// </para>
/// <para>
/// <b>Enabling Tracing:</b> See <see cref="TerminaTraceExtensions"/> for DI integration,
/// or use <see cref="Configure"/> for manual setup:
/// </para>
/// <code>
/// // Manual configuration
/// var listener = new FileTraceListener("trace.log");
/// TerminaTrace.Configure(listener, TerminaTraceCategory.All, TerminaTraceLevel.Debug);
///
/// // Or use DI extension methods
/// services.AddTerminaFileTracing("trace.log");
/// services.AddTerminaLoggerTracing(); // M.E.Logging integration
/// </code>
/// <para>
/// <b>Usage from Components:</b>
/// </para>
/// <code>
/// // Simple trace - no allocation if tracing disabled
/// TerminaTrace.Focus.Debug(this, "PushFocus called");
///
/// // With format args - formatting deferred until listener needs it
/// TerminaTrace.Focus.Debug(this, "PushFocus: {0}, depth={1}", focusable.GetType().Name, depth);
/// </code>
/// </remarks>
public static class TerminaTrace
{
    private static volatile ITerminaTraceListener? _listener;
    private static volatile TerminaTraceCategory _enabledCategories = TerminaTraceCategory.None;
    private static volatile TerminaTraceLevel _minimumLevel = TerminaTraceLevel.Debug;

    /// <summary>
    /// Gets the trace logger for focus management operations.
    /// </summary>
    public static CategoryTrace Focus { get; } = new(TerminaTraceCategory.Focus);

    /// <summary>
    /// Gets the trace logger for layout lifecycle operations.
    /// </summary>
    public static CategoryTrace Layout { get; } = new(TerminaTraceCategory.Layout);

    /// <summary>
    /// Gets the trace logger for input routing operations.
    /// </summary>
    public static CategoryTrace Input { get; } = new(TerminaTraceCategory.Input);

    /// <summary>
    /// Gets the trace logger for page lifecycle operations.
    /// </summary>
    public static CategoryTrace Page { get; } = new(TerminaTraceCategory.Page);

    /// <summary>
    /// Gets the trace logger for reactive subscription operations.
    /// </summary>
    public static CategoryTrace Reactive { get; } = new(TerminaTraceCategory.Reactive);

    /// <summary>
    /// Gets the trace logger for rendering operations.
    /// </summary>
    public static CategoryTrace Render { get; } = new(TerminaTraceCategory.Render);

    /// <summary>
    /// Gets the trace logger for platform console operations.
    /// </summary>
    public static CategoryTrace Platform { get; } = new(TerminaTraceCategory.Platform);

    /// <summary>
    /// Gets whether any tracing is currently enabled.
    /// </summary>
    public static bool IsEnabled => _listener != null && _enabledCategories != TerminaTraceCategory.None;

    /// <summary>
    /// Configure the trace listener and enabled categories.
    /// </summary>
    /// <param name="listener">The listener to receive trace output.</param>
    /// <param name="categories">The categories to enable (default: All).</param>
    /// <param name="minimumLevel">The minimum level to trace (default: Debug).</param>
    public static void Configure(
        ITerminaTraceListener listener,
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug)
    {
        _listener = listener;
        _enabledCategories = categories;
        _minimumLevel = minimumLevel;
    }

    /// <summary>
    /// Disable all tracing.
    /// </summary>
    public static void Disable()
    {
        _listener = null;
        _enabledCategories = TerminaTraceCategory.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ShouldTrace(TerminaTraceCategory category, TerminaTraceLevel level)
    {
        return _listener != null
               && (_enabledCategories & category) != 0
               && level >= _minimumLevel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteEvent(in TraceEvent evt)
    {
        _listener?.Write(in evt);
    }

    /// <summary>
    /// Category-specific trace logger with deferred formatting.
    /// </summary>
    public sealed class CategoryTrace
    {
        private readonly TerminaTraceCategory _category;

        internal CategoryTrace(TerminaTraceCategory category)
        {
            _category = category;
        }

        /// <summary>
        /// Gets whether this category is currently enabled for any level.
        /// </summary>
        public bool IsEnabled => ShouldTrace(_category, _minimumLevel);

        #region Trace Level

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace(object source, string message)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Trace)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Trace, _category,
                source.GetType().Name, source.GetHashCode(), message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace<T0>(object source, string template, T0 arg0)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Trace)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Trace, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace<T0, T1>(object source, string template, T0 arg0, T1 arg1)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Trace)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Trace, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0, arg1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trace<T0, T1, T2>(object source, string template, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Trace)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Trace, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0, arg1, arg2));
        }

        #endregion

        #region Debug Level

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(object source, string message)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Debug)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Debug, _category,
                source.GetType().Name, source.GetHashCode(), message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T0>(object source, string template, T0 arg0)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Debug)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Debug, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T0, T1>(object source, string template, T0 arg0, T1 arg1)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Debug)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Debug, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0, arg1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T0, T1, T2>(object source, string template, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Debug)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Debug, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0, arg1, arg2));
        }

        #endregion

        #region Info Level

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(object source, string message)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Info)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Info, _category,
                source.GetType().Name, source.GetHashCode(), message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info<T0>(object source, string template, T0 arg0)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Info)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Info, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info<T0, T1>(object source, string template, T0 arg0, T1 arg1)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Info)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Info, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0, arg1));
        }

        #endregion

        #region Warning Level

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(object source, string message)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Warning)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Warning, _category,
                source.GetType().Name, source.GetHashCode(), message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning<T0>(object source, string template, T0 arg0)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Warning)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Warning, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning<T0, T1>(object source, string template, T0 arg0, T1 arg1)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Warning)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Warning, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0, arg1));
        }

        #endregion

        #region Error Level

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(object source, string message)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Error)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Error, _category,
                source.GetType().Name, source.GetHashCode(), message));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T0>(object source, string template, T0 arg0)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Error)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Error, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T0, T1>(object source, string template, T0 arg0, T1 arg1)
        {
            if (!ShouldTrace(_category, TerminaTraceLevel.Error)) return;
            WriteEvent(new TraceEvent(TerminaTraceLevel.Error, _category,
                source.GetType().Name, source.GetHashCode(), template, arg0, arg1));
        }

        #endregion
    }
}
