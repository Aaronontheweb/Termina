// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Threading.Channels;

namespace Termina.Diagnostics;

/// <summary>
/// Trace listener that writes to a file or TextWriter using a lock-free channel.
/// </summary>
/// <remarks>
/// <para>
/// This listener uses a channel-based design to avoid blocking the UI thread.
/// Trace calls just push events to an unbounded channel (non-blocking), and a
/// background task consumes them and writes to the output.
/// </para>
/// <para>
/// Example output:
/// </para>
/// <code>
/// 2024-01-15 10:30:45.123 [DEBUG] [Focus] FocusManager#12345678 - PushFocus: TextInputNode
/// 2024-01-15 10:30:45.125 [TRACE] [Input] TextInputNode#87654321 - HandleInput: Key=A
/// </code>
/// </remarks>
public sealed class FileTraceListener : ITerminaTraceListener, IAsyncDisposable, IDisposable
{
    private readonly Channel<TraceEvent> _channel;
    private readonly Task _consumerTask;
    private readonly TextWriter _writer;
    private readonly bool _ownsWriter;
    private readonly CancellationTokenSource _cts = new();
    private readonly TerminaTraceCategory _enabledCategories;
    private readonly TerminaTraceLevel _minimumLevel;
    private volatile bool _disposed;
    private int _disposeStarted;

    // UTF-8 encoding that does not emit a byte-order mark.
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Create a file trace listener that writes to a file.
    /// </summary>
    /// <param name="filePath">Path to the output file.</param>
    /// <param name="categories">Categories to enable (default: All).</param>
    /// <param name="minimumLevel">Minimum level to trace (default: Debug).</param>
    public FileTraceListener(
        string filePath,
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug)
    {
        // UTF-8 without a BOM: trace files are read by line-oriented tools (tail/grep/log
        // shippers) that would otherwise surface the 3-byte preamble as garbage on line 1.
        _writer = new StreamWriter(filePath, append: false, encoding: Utf8NoBom) { AutoFlush = true };
        _ownsWriter = true;
        _enabledCategories = categories;
        _minimumLevel = minimumLevel;

        _channel = Channel.CreateUnbounded<TraceEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _consumerTask = ConsumeEventsAsync(_cts.Token);
    }

    /// <summary>
    /// Create a file trace listener that writes to a TextWriter.
    /// </summary>
    /// <param name="writer">The writer to output to (e.g., Console.Error).</param>
    /// <param name="categories">Categories to enable (default: All).</param>
    /// <param name="minimumLevel">Minimum level to trace (default: Debug).</param>
    /// <param name="ownsWriter">Whether to dispose the writer when this listener is disposed.</param>
    public FileTraceListener(
        TextWriter writer,
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug,
        bool ownsWriter = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _ownsWriter = ownsWriter;
        _enabledCategories = categories;
        _minimumLevel = minimumLevel;

        _channel = Channel.CreateUnbounded<TraceEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _consumerTask = ConsumeEventsAsync(_cts.Token);
    }

    /// <summary>
    /// Create a file trace listener that writes to stderr.
    /// </summary>
    /// <param name="categories">Categories to enable (default: All).</param>
    /// <param name="minimumLevel">Minimum level to trace (default: Debug).</param>
    /// <returns>A new file trace listener writing to stderr.</returns>
    public static FileTraceListener CreateStdErr(
        TerminaTraceCategory categories = TerminaTraceCategory.All,
        TerminaTraceLevel minimumLevel = TerminaTraceLevel.Debug)
    {
        return new FileTraceListener(Console.Error, categories, minimumLevel, ownsWriter: false);
    }

    /// <inheritdoc />
    public bool IsEnabled(TerminaTraceLevel level, TerminaTraceCategory category)
    {
        return !_disposed
               && level >= _minimumLevel
               && (_enabledCategories & category) != 0;
    }

    /// <inheritdoc />
    public void Write(in TraceEvent evt)
    {
        if (_disposed)
            return;

        // Non-blocking push to channel - UI thread never waits
        _channel.Writer.TryWrite(evt);
    }

    /// <summary>
    /// Background task that consumes events from the channel and writes to output.
    /// </summary>
    private async Task ConsumeEventsAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var evt in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                if (!IsEnabled(evt.Level, evt.Category))
                    continue;

                var line = FormatEvent(evt);
                await _writer.WriteLineAsync(line).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            // Don't let trace errors crash the app - write to stderr as last resort
            try
            {
                await Console.Error.WriteLineAsync($"[TerminaTrace] Consumer error: {ex.Message}")
                    .ConfigureAwait(false);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private static string FormatEvent(in TraceEvent evt)
    {
        var timestamp = evt.Timestamp.ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var levelStr = evt.Level.ToString().ToUpperInvariant();
        var categoryStr = evt.Category.ToString();
        var hashStr = evt.SourceHash.ToString("X8");
        var message = evt.FormatMessage();

        return $"{timestamp} [{levelStr}] [{categoryStr}] {evt.SourceType}#{hashStr} - {message}";
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Single-shot guard: only the first caller proceeds. Set BEFORE doing any work so a
        // concurrent Dispose()/DisposeAsync() can't call _channel.Writer.Complete() twice
        // (which throws). This is separate from _disposed, which gates IsEnabled() and is
        // deliberately set only AFTER the drain below.
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        // Signal completion and wait for consumer to drain
        // NOTE: _disposed is set AFTER the drain so IsEnabled() still returns true
        // during draining — otherwise the consumer sees disposed=true and skips all events.
        _channel.Writer.Complete();

        try
        {
            // Give consumer time to drain remaining events
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _consumerTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Timeout - some events may be lost
        }
        catch (Exception)
        {
            // Consumer task may have faulted
        }

        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();

        if (_ownsWriter)
        {
            await _writer.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Single-shot guard (see DisposeAsync). Set before any work.
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        // Complete first, then drain. As in DisposeAsync, _disposed is set AFTER the drain so
        // the consumer keeps writing buffered events instead of skipping them via IsEnabled().
        _channel.Writer.Complete();

        // Synchronously wait for consumer with timeout
        try
        {
            _consumerTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignore - best effort
        }

        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();

        if (_ownsWriter)
        {
            _writer.Dispose();
        }
    }
}
