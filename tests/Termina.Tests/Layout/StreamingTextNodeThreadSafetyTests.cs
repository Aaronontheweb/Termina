// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Components.Streaming;
using Termina.Layout;
using Termina.Terminal;

namespace Termina.Tests.Layout;

/// <summary>
/// Thread-safety regression tests for <see cref="StreamingTextNode"/>.
/// Covers the race where animation <see cref="IAnimatedTextSegment.Invalidated"/>
/// callbacks (firing on the segment's own scheduler — e.g. an R3 timer thread)
/// would iterate <c>_content</c> and mutate <c>_buffer</c> without taking
/// <c>_contentLock</c>, racing with public mutations and with <see cref="StreamingTextNode.Dispose"/>.
/// </summary>
public class StreamingTextNodeThreadSafetyTests
{
    /// <summary>
    /// Stress test: a background thread fires animation invalidations as fast as it can
    /// while the main thread hammers the public mutation API. Without the lock fix this
    /// reliably throws <see cref="InvalidOperationException"/> ("Collection was modified")
    /// from the foreach inside RebuildBuffer, or torn-buffer assertion failures.
    /// </summary>
    [Fact]
    public async Task AnimationInvalidation_ConcurrentWithMutations_DoesNotThrow()
    {
        var node = StreamingTextNode.Create();
        var spinner = new ControllableAnimatedSegment();
        const int SpinnerId = 1;
        node.AppendTracked(new SegmentId(SpinnerId), spinner);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Exception? tickerException = null;

        var ticker = Task.Run(() =>
        {
            try
            {
                var i = 0;
                while (!cts.IsCancellationRequested)
                {
                    spinner.TriggerInvalidation("frame" + (++i));
                }
            }
            catch (Exception ex)
            {
                tickerException = ex;
            }
        });

        Exception? mainException = null;
        try
        {
            var j = SpinnerId + 1;
            while (!cts.IsCancellationRequested)
            {
                var id = new SegmentId(j++);
                node.AppendTracked(id, new StaticTextSegment("hello", TextStyle.Default));
                node.Remove(id);
            }
        }
        catch (Exception ex)
        {
            mainException = ex;
        }
        finally
        {
            cts.Cancel();
            await ticker;
            node.Dispose();
            spinner.Dispose();
        }

        Assert.Null(tickerException);
        Assert.Null(mainException);
    }

    /// <summary>
    /// Stress test: <see cref="StreamingTextNode.Clear"/> rebuilds the world while the
    /// animation thread tries to RebuildBuffer concurrently. This exercises both the
    /// _content iteration race AND the buffer mutation race.
    /// </summary>
    [Fact]
    public async Task AnimationInvalidation_ConcurrentWithClear_DoesNotThrow()
    {
        var node = StreamingTextNode.Create();
        const int SpinnerId = 1;
        var currentSpinner = new ControllableAnimatedSegment();
        node.AppendTracked(new SegmentId(SpinnerId), currentSpinner);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Exception? tickerException = null;

        var ticker = Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    // Volatile snapshot — main thread swaps the active spinner after each Clear.
                    Volatile.Read(ref currentSpinner)?.TriggerInvalidation("frame");
                }
            }
            catch (Exception ex)
            {
                tickerException = ex;
            }
        });

        Exception? mainException = null;
        try
        {
            var j = SpinnerId + 1;
            while (!cts.IsCancellationRequested)
            {
                // Keep adding fresh content between Clears so the rebuild path has
                // _content elements to iterate over when the animation thread wins
                // the lock race.
                node.Append("noise " + j++);
                node.AppendTracked(new SegmentId(j++), new StaticTextSegment("tracked", TextStyle.Default));
                node.Clear();
                // Clear disposes the previous spinner. Swap in a fresh one before re-attaching.
                var fresh = new ControllableAnimatedSegment();
                Volatile.Write(ref currentSpinner, fresh);
                node.AppendTracked(new SegmentId(SpinnerId), fresh);
            }
        }
        catch (Exception ex)
        {
            mainException = ex;
        }
        finally
        {
            cts.Cancel();
            await ticker;
            node.Dispose();
        }

        Assert.Null(tickerException);
        Assert.Null(mainException);
    }

    /// <summary>
    /// Stress test: Replace swaps the animation subscription while the old segment
    /// might still be firing on a different thread. The lock + subscription disposal
    /// must coordinate cleanly.
    /// </summary>
    [Fact]
    public async Task AnimationInvalidation_ConcurrentWithReplace_DoesNotThrow()
    {
        var node = StreamingTextNode.Create();
        var id = new SegmentId(1);
        var initial = new ControllableAnimatedSegment();
        node.AppendTracked(id, initial);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Exception? tickerException = null;
        var current = initial;

        var ticker = Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    // Volatile-ish read: the test only needs to call a live segment's
                    // OnNext. Even if `current` is replaced mid-call, the old segment's
                    // disposed-state guard makes TriggerInvalidation safe.
                    var snapshot = Volatile.Read(ref current!);
                    snapshot.TriggerInvalidation("frame");
                }
            }
            catch (Exception ex)
            {
                tickerException = ex;
            }
        });

        Exception? mainException = null;
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var next = new ControllableAnimatedSegment();
                node.Replace(id, next);
                var old = Interlocked.Exchange(ref current!, next);
                // The old segment is now disposed by Replace; ticker reads from `current`
                // and won't touch it again.
                _ = old;
            }
        }
        catch (Exception ex)
        {
            mainException = ex;
        }
        finally
        {
            cts.Cancel();
            await ticker;
            node.Dispose();
        }

        Assert.Null(tickerException);
        Assert.Null(mainException);
    }

    /// <summary>
    /// After <see cref="StreamingTextNode.Dispose"/>, a late animation invalidation
    /// must not throw and must not call OnNext on the disposed <c>_invalidated</c>
    /// Subject. (Subscription disposal in Dispose handles the common case; the
    /// _disposed flag in the callback is the belt-and-suspenders guard for in-flight
    /// callbacks that already passed the subject's observer list.)
    /// </summary>
    [Fact]
    public void AnimationInvalidation_AfterDispose_DoesNotThrow()
    {
        var node = StreamingTextNode.Create();
        var spinner = new ControllableAnimatedSegment();
        node.AppendTracked(new SegmentId(1), spinner);

        node.Dispose();

        var ex = Record.Exception(() => spinner.TriggerInvalidation("late"));
        Assert.Null(ex);

        spinner.Dispose();
    }

    /// <summary>
    /// Calling <see cref="StreamingTextNode.Dispose"/> twice must be safe (idempotent)
    /// and must not double-dispose <c>_invalidated</c>.
    /// </summary>
    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var node = StreamingTextNode.Create();
        node.Append("anything");

        node.Dispose();
        var ex = Record.Exception(() => node.Dispose());
        Assert.Null(ex);
    }

    /// <summary>
    /// Test-only <see cref="IAnimatedTextSegment"/> whose invalidation is driven by
    /// the test (not a timer), so race tests can fire on a background thread with
    /// no scheduler dependency.
    /// </summary>
    private sealed class ControllableAnimatedSegment : IAnimatedTextSegment
    {
        private readonly Subject<Unit> _invalidated = new();
        private string _text = "frame0";
        private volatile bool _disposed;

        public Observable<Unit> Invalidated => _invalidated.AsObservable();

        public bool IsAnimating => !_disposed;

        public StyledSegment GetCurrentSegment() => new(_text, TextStyle.Default);

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void TriggerInvalidation(string newText)
        {
            if (_disposed) return;
            _text = newText;
            try
            {
                _invalidated.OnNext(Unit.Default);
            }
            catch (ObjectDisposedException)
            {
                // Race with Dispose() on another thread — expected; treat as a no-op.
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _invalidated.OnCompleted();
            _invalidated.Dispose();
        }
    }
}
