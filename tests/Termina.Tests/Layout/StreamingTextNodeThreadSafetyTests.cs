// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
    /// Stress test: <see cref="StreamingTextNode.Replace"/> mutates a tracked element
    /// concurrently with animation invalidations firing on a SEPARATE, permanently-tracked
    /// spinner. The ticker thread is always firing on a live subscription, so its
    /// <c>OnAnimationInvalidated</c> race-condition target is real — it competes with
    /// Replace for <c>_contentLock</c> while Replace disposes the old element, mutates
    /// <c>_content</c>, and rebuilds the buffer.
    /// </summary>
    [Fact]
    public async Task AnimationInvalidation_ConcurrentWithReplace_DoesNotThrow()
    {
        var node = StreamingTextNode.Create();
        var tickerSpinnerId = new SegmentId(1);
        var replaceTargetId = new SegmentId(2);

        // Permanently-tracked spinner: the ticker thread always fires on this live
        // subscription, so OnAnimationInvalidated is always reachable.
        var tickerSpinner = new ControllableAnimatedSegment();
        node.AppendTracked(tickerSpinnerId, tickerSpinner);

        // Separate tracked element that Replace swaps repeatedly. The ticker doesn't
        // touch this element; it just exists to exercise Replace's mutation path.
        node.AppendTracked(replaceTargetId, new StaticTextSegment("initial", TextStyle.Default));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Exception? tickerException = null;

        var ticker = Task.Run(() =>
        {
            try
            {
                var i = 0;
                while (!cts.IsCancellationRequested)
                {
                    tickerSpinner.TriggerInvalidation("frame" + (++i));
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
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                node.Replace(
                    replaceTargetId,
                    new StaticTextSegment("replacement" + (++i), TextStyle.Default));
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
    /// After <see cref="StreamingTextNode.Dispose"/>, a late <c>OnNext</c> on the
    /// segment's <see cref="IAnimatedTextSegment.Invalidated"/> stream must not reach
    /// the node's callback at all — Dispose disposes the R3 subscription, which
    /// unhooks the observer. This covers the common case where the subscription's
    /// disposal beats the late emission.
    /// </summary>
    [Fact]
    public void AnimationInvalidation_AfterDispose_SubscriptionDisposalUnhooksCallback()
    {
        var node = StreamingTextNode.Create();
        var spinner = new ControllableAnimatedSegment();
        node.AppendTracked(new SegmentId(1), spinner);

        node.Dispose();

        // The R3 subscription was disposed inside node.Dispose(), so the node's
        // OnAnimationInvalidated is no longer on the spinner's observer list.
        // TriggerInvalidation fires OnNext on a subject with zero relevant observers.
        var ex = Record.Exception(() => spinner.TriggerInvalidation("late"));
        Assert.Null(ex);

        spinner.Dispose();
    }

    /// <summary>
    /// Belt-and-suspenders test for the <c>_disposed</c> flag inside
    /// <c>OnAnimationInvalidated</c>: simulates a callback that already passed the
    /// subject's observer-list dispatch (so subscription disposal can't unhook it)
    /// and is just about to enter the lock when <see cref="StreamingTextNode.Dispose"/>
    /// completes. Without the <c>_disposed</c> guard, the callback would call
    /// <c>_invalidated.OnNext</c> on the now-disposed Subject and throw
    /// <see cref="ObjectDisposedException"/>.
    /// Uses reflection to invoke the private callback directly because there is no
    /// public API that lets the test schedule an in-flight observer dispatch across
    /// the dispose boundary.
    /// </summary>
    [Fact]
    public void OnAnimationInvalidated_AfterDispose_BailsOutViaDisposedFlag()
    {
        var node = StreamingTextNode.Create();
        var spinner = new ControllableAnimatedSegment();
        node.AppendTracked(new SegmentId(1), spinner);

        node.Dispose();

        var callback = typeof(StreamingTextNode).GetMethod(
            "OnAnimationInvalidated",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(callback);

        var ex = Record.Exception(() => callback!.Invoke(node, null));
        // Reflection wraps target exceptions in TargetInvocationException; unwrap.
        var inner = ex is TargetInvocationException tie ? tie.InnerException : ex;
        Assert.Null(inner);

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
    /// Public mutators must not throw when called after <see cref="StreamingTextNode.Dispose"/>.
    /// Pre-fix, every mutator ended with <c>NotifyChanged()</c> → <c>_invalidated.OnNext(...)</c>
    /// with no disposed check, which threw <see cref="ObjectDisposedException"/> from R3's
    /// <c>Subject&lt;T&gt;.OnNext</c> on the disposed Subject. The race window: a mutator on
    /// thread A releases <c>_contentLock</c> before calling NotifyChanged; thread B's
    /// <c>Dispose()</c> can run the OnCompleted/Dispose on <c>_invalidated</c> in that gap.
    /// </summary>
    [Theory]
    [MemberData(nameof(PublicMutatorActions))]
    public void PublicMutator_AfterDispose_DoesNotThrow(string name, Action<StreamingTextNode> mutate)
    {
        _ = name; // theory parameter for clearer test names
        var node = StreamingTextNode.Create();
        node.Dispose();

        var ex = Record.Exception(() => mutate(node));
        Assert.Null(ex);
    }

    public static IEnumerable<object[]> PublicMutatorActions()
    {
        yield return ["Append(string)", new Action<StreamingTextNode>(n => n.Append("x"))];
        yield return ["Append(string, color)", new Action<StreamingTextNode>(n => n.Append("x", Color.Red))];
        yield return ["Append(StyledSegment)", new Action<StreamingTextNode>(n => n.Append(new StyledSegment("x", TextStyle.Default)))];
        yield return ["AppendLine(string)", new Action<StreamingTextNode>(n => n.AppendLine("x"))];
        yield return ["AppendLine(string, color)", new Action<StreamingTextNode>(n => n.AppendLine("x", Color.Red))];
        yield return ["AppendTracked", new Action<StreamingTextNode>(n => n.AppendTracked(new SegmentId(42), new StaticTextSegment("x", TextStyle.Default)))];
        yield return ["Remove", new Action<StreamingTextNode>(n => n.Remove(new SegmentId(42)))];
        yield return ["Replace", new Action<StreamingTextNode>(n => n.Replace(new SegmentId(42), new StaticTextSegment("y", TextStyle.Default)))];
        yield return ["Clear", new Action<StreamingTextNode>(n => n.Clear())];
        yield return ["ScrollUp", new Action<StreamingTextNode>(n => n.ScrollUp())];
        yield return ["ScrollDown", new Action<StreamingTextNode>(n => n.ScrollDown())];
        yield return ["ScrollToBottom", new Action<StreamingTextNode>(n => n.ScrollToBottom())];
    }

    /// <summary>
    /// Stress test: <c>StreamingTextNode.Append</c> hammered on a background thread
    /// while the main thread disposes the node. The fix in <c>NotifyChanged</c> must
    /// prevent <see cref="ObjectDisposedException"/> from leaking out of the in-flight
    /// mutator after Dispose() has completed <c>_invalidated.Dispose()</c>.
    /// </summary>
    [Fact]
    public async Task Append_ConcurrentWithDispose_DoesNotThrow()
    {
        var node = StreamingTextNode.Create();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        Exception? appenderException = null;

        var appender = Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    node.Append("x");
                }
            }
            catch (Exception ex)
            {
                appenderException = ex;
            }
        });

        // Give the appender a head start, then dispose mid-flight to exercise the race.
        await Task.Delay(50);
        node.Dispose();
        await appender;

        Assert.Null(appenderException);
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
