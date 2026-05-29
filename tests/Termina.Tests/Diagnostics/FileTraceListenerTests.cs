// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license info.

using System.IO;
using System.Text;
using Termina.Diagnostics;

namespace Termina.Tests.Diagnostics;

/// <summary>
/// Regression tests for <see cref="FileTraceListener"/> encoding and dispose-drain behavior.
/// </summary>
public sealed class FileTraceListenerTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private string NewTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"trace_{Guid.NewGuid():N}.log");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }

    private static TraceEvent Event(int seq, string message) => new(
        TerminaTraceLevel.Debug,
        TerminaTraceCategory.Render,
        "TestComponent",
        seq,
        message);

    [Fact]
    public async Task DisposeAsync_flushes_unicode_content_with_utf8()
    {
        // Regression: StreamWriter used Encoding.Default, throwing ArgumentException on chars
        // like ➭ (U+27AD) — that faulted the consumer and flooded the unbounded channel until
        // the pod was OOM-killed. Also exercises the drain order: DisposeAsync must flush
        // buffered events (it sets _disposed AFTER draining, not before).
        var tempFile = NewTempFile();
        var unicodeMessage = "➭➭➭➭➭ Special chars: ñ, é, ü, 中文, 🚀";

        await using (var listener = new FileTraceListener(tempFile))
        {
            listener.Write(Event(42, unicodeMessage));
        }

        var content = File.ReadAllText(tempFile, Encoding.UTF8);
        Assert.Contains(unicodeMessage, content);
    }

    [Fact]
    public async Task Consumer_survives_unicode_under_stress()
    {
        // 100 Unicode events must all reach the file — proves the consumer never faults and
        // every buffered event is drained on dispose.
        var tempFile = NewTempFile();
        var unicodeChars = "➭ñéü中文🚀🔥💯🎯";

        await using (var listener = new FileTraceListener(tempFile))
        {
            for (var i = 0; i < 100; i++)
                listener.Write(Event(i, $"{unicodeChars} iteration {i}"));
        }

        var lines = File.ReadAllLines(tempFile, Encoding.UTF8);
        Assert.Equal(100, lines.Length);
    }

    [Fact]
    public void Sync_Dispose_flushes_buffered_unicode_content()
    {
        // The synchronous Dispose() path must also drain: previously it set _disposed = true and
        // cancelled the consumer token BEFORE the drain, so buffered events were skipped via
        // IsEnabled() and the file came out empty. Content must now reach the file.
        var tempFile = NewTempFile();
        var unicodeOnly = "➭🚀中文ñéü";

        using (var listener = new FileTraceListener(tempFile))
        {
            listener.Write(Event(0, unicodeOnly));
        }

        var content = File.ReadAllText(tempFile, Encoding.UTF8);
        Assert.Contains(unicodeOnly, content);
    }

    [Fact]
    public async Task File_is_written_without_a_utf8_bom()
    {
        // A BOM would surface as a stray 3-byte (EF BB BF) prefix to line-oriented log tooling
        // (tail/grep/log shippers), so the writer must use UTF-8 without a preamble.
        var tempFile = NewTempFile();

        await using (var listener = new FileTraceListener(tempFile))
        {
            listener.Write(Event(0, "plain ascii line"));
        }

        var bytes = await File.ReadAllBytesAsync(tempFile);
        var bom = Encoding.UTF8.GetPreamble();
        var startsWithBom = bytes.Length >= bom.Length
            && bytes[0] == bom[0] && bytes[1] == bom[1] && bytes[2] == bom[2];
        Assert.False(startsWithBom);
    }

    [Fact]
    public async Task Redundant_disposal_is_safe()
    {
        // The single-shot guard makes repeated/mixed disposal a no-op rather than calling
        // _channel.Writer.Complete() a second time (which throws InvalidOperationException).
        var tempFile = NewTempFile();
        var listener = new FileTraceListener(tempFile);
        listener.Write(Event(0, "line"));

        await listener.DisposeAsync();
        listener.Dispose();
        await listener.DisposeAsync();
    }
}
