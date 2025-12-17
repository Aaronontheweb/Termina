// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Termina.Diagnostics;

namespace Termina.Benchmarks;

/// <summary>
/// Benchmarks for tracing when DISABLED - should show near-zero overhead.
/// </summary>
/// <remarks>
/// This is the critical benchmark: when tracing is disabled, there should be
/// no allocations and minimal CPU overhead (just an inlined boolean check).
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class TracingDisabledBenchmarks
{
    [GlobalSetup]
    public void Setup()
    {
        // Ensure tracing is disabled
        TerminaTrace.Disable();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("NoArgs")]
    public void Disabled_NoArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message");
    }

    [Benchmark]
    [BenchmarkCategory("OneArg")]
    public void Disabled_OneArg()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}", 42);
    }

    [Benchmark]
    [BenchmarkCategory("TwoArgs")]
    public void Disabled_TwoArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}, {1}", 42, "hello");
    }

    [Benchmark]
    [BenchmarkCategory("ThreeArgs")]
    public void Disabled_ThreeArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}, {1}, {2}", 42, "hello", true);
    }
}

/// <summary>
/// Benchmarks for tracing when ENABLED with a null listener (no I/O).
/// </summary>
/// <remarks>
/// This measures the overhead of creating TraceEvent structs without
/// any actual formatting or I/O.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class TracingEnabledNullListenerBenchmarks
{
    private NullTraceListener _listener = null!;

    [GlobalSetup]
    public void Setup()
    {
        _listener = new NullTraceListener();
        TerminaTrace.Configure(_listener, TerminaTraceCategory.All, TerminaTraceLevel.Trace);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        TerminaTrace.Disable();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("NoArgs")]
    public void NullListener_NoArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message");
    }

    [Benchmark]
    [BenchmarkCategory("OneArg")]
    public void NullListener_OneArg()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}", 42);
    }

    [Benchmark]
    [BenchmarkCategory("TwoArgs")]
    public void NullListener_TwoArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}, {1}", 42, "hello");
    }

    [Benchmark]
    [BenchmarkCategory("ThreeArgs")]
    public void NullListener_ThreeArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}, {1}, {2}", 42, "hello", true);
    }
}

/// <summary>
/// Benchmarks for tracing when ENABLED with a listener that formats messages.
/// </summary>
/// <remarks>
/// This measures the full cost of tracing including string formatting.
/// This is the "worst case" for enabled tracing.
/// </remarks>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class TracingEnabledFormattingBenchmarks
{
    private FormattingTraceListener _listener = null!;

    [GlobalSetup]
    public void Setup()
    {
        _listener = new FormattingTraceListener();
        TerminaTrace.Configure(_listener, TerminaTraceCategory.All, TerminaTraceLevel.Trace);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        TerminaTrace.Disable();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("NoArgs")]
    public void Formatting_NoArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message");
    }

    [Benchmark]
    [BenchmarkCategory("OneArg")]
    public void Formatting_OneArg()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}", 42);
    }

    [Benchmark]
    [BenchmarkCategory("TwoArgs")]
    public void Formatting_TwoArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}, {1}", 42, "hello");
    }

    [Benchmark]
    [BenchmarkCategory("ThreeArgs")]
    public void Formatting_ThreeArgs()
    {
        TerminaTrace.Focus.Debug(this, "Test message: {0}, {1}, {2}", 42, "hello", true);
    }
}

/// <summary>
/// A trace listener that does nothing - used to measure pure tracing overhead.
/// </summary>
internal sealed class NullTraceListener : ITerminaTraceListener
{
    public bool IsEnabled(TerminaTraceLevel level, TerminaTraceCategory category) => true;
    public void Write(in TraceEvent evt) { }
}

/// <summary>
/// A trace listener that formats the message - used to measure formatting cost.
/// </summary>
internal sealed class FormattingTraceListener : ITerminaTraceListener
{
    private string? _lastMessage;

    public bool IsEnabled(TerminaTraceLevel level, TerminaTraceCategory category) => true;

    public void Write(in TraceEvent evt)
    {
        // Force the message to be formatted
        _lastMessage = evt.FormatMessage();
    }
}
