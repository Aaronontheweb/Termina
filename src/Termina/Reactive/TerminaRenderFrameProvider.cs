// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;

namespace Termina.Reactive;

/// <summary>
/// R3 frame provider bound to a <see cref="TerminaApplication" /> render loop.
/// </summary>
/// <remarks>
/// Registered frame work items are executed on the Termina event loop thread, immediately
/// before the render pass for the frame event that woke the loop.
/// </remarks>
public sealed class TerminaRenderFrameProvider : FrameProvider, IDisposable
{
    private readonly object _gate = new();
    private readonly Action _requestFrame;
    private readonly TimeSpan _frameInterval;
    private readonly ITimer _timer;
    private readonly List<IFrameRunnerWorkItem> _items = [];

    private long _frameCount;
    private bool _frameQueued;
    private bool _timerScheduled;
    private bool _disposed;

    internal TerminaRenderFrameProvider(
        Action requestFrame,
        TimeProvider timeProvider,
        TimeSpan frameInterval)
    {
        ArgumentNullException.ThrowIfNull(requestFrame);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _requestFrame = requestFrame;
        _frameInterval = frameInterval <= TimeSpan.Zero
            ? TimeSpan.FromMilliseconds(16)
            : frameInterval;
        _timer = timeProvider.CreateTimer(
            static state => ((TerminaRenderFrameProvider)state!).RequestFrameFromTimer(),
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc />
    public override long GetFrameCount()
    {
        lock (_gate)
        {
            return _frameCount;
        }
    }

    /// <inheritdoc />
    public override void Register(IFrameRunnerWorkItem callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var requestNow = false;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _items.Add(callback);

            if (!_frameQueued)
            {
                _frameQueued = true;
                requestNow = true;

                if (_timerScheduled)
                {
                    _timerScheduled = false;
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }
        }

        if (requestNow)
            _requestFrame();
    }

    internal void AdvanceFrame()
    {
        IFrameRunnerWorkItem[] snapshot;
        long frameCount;

        lock (_gate)
        {
            if (_disposed)
                return;

            _frameQueued = false;
            snapshot = _items.ToArray();
            frameCount = _frameCount;
        }

        foreach (var item in snapshot)
        {
            try
            {
                if (!item.MoveNext(frameCount))
                    Remove(item);
            }
            catch (Exception ex)
            {
                Remove(item);
                ReportUnhandledException(ex);
            }
        }

        var scheduleNext = false;
        lock (_gate)
        {
            if (_disposed)
                return;

            _frameCount++;
            if (_items.Count > 0 && !_frameQueued && !_timerScheduled)
            {
                _timerScheduled = true;
                scheduleNext = true;
            }
        }

        if (scheduleNext)
            _timer.Change(_frameInterval, Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _items.Clear();
            _frameQueued = false;
            _timerScheduled = false;
        }

        _timer.Dispose();
    }

    private void RequestFrameFromTimer()
    {
        var requestNow = false;
        lock (_gate)
        {
            _timerScheduled = false;
            if (_disposed || _frameQueued || _items.Count == 0)
                return;

            _frameQueued = true;
            requestNow = true;
        }

        if (requestNow)
            _requestFrame();
    }

    private void Remove(IFrameRunnerWorkItem item)
    {
        lock (_gate)
        {
            _items.Remove(item);
        }
    }

    private static void ReportUnhandledException(Exception ex)
    {
        try
        {
            ObservableSystem.GetUnhandledExceptionHandler().Invoke(ex);
        }
        catch
        {
        }
    }
}
