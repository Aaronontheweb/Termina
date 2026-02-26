// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Terminal;

namespace Termina.Components.Streaming;

/// <summary>
/// Available spinner animation styles.
/// </summary>
public enum SpinnerStyle
{
    /// <summary>Braille dots spinner: ⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏</summary>
    Dots,

    /// <summary>Line spinner: - \ | /</summary>
    Line,

    /// <summary>Arrow spinner: ← ↖ ↑ ↗ → ↘ ↓ ↙</summary>
    Arrow,

    /// <summary>Bounce spinner: ⠁ ⠂ ⠄ ⠂</summary>
    Bounce,

    /// <summary>Box spinner: ▖ ▘ ▝ ▗</summary>
    Box,

    /// <summary>Circle spinner: ◐ ◓ ◑ ◒</summary>
    Circle
}

/// <summary>
/// Animated spinner segment that cycles through animation frames.
/// Emits invalidation events when frames change to trigger redraws.
/// </summary>
public sealed class SpinnerSegment : IAnimatedTextSegment
{
    private static readonly Dictionary<SpinnerStyle, string[]> Frames = new()
    {
        [SpinnerStyle.Dots] = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"],
        [SpinnerStyle.Line] = ["-", "\\", "|", "/"],
        [SpinnerStyle.Arrow] = ["←", "↖", "↑", "↗", "→", "↘", "↓", "↙"],
        [SpinnerStyle.Bounce] = ["⠁", "⠂", "⠄", "⠂"],
        [SpinnerStyle.Box] = ["▖", "▘", "▝", "▗"],
        [SpinnerStyle.Circle] = ["◐", "◓", "◑", "◒"]
    };

    private readonly TimeProvider _timeProvider;
    private readonly int _intervalMs;
    private readonly string[] _frames;
    private readonly Subject<Unit> _invalidated = new();
    private readonly TextStyle _style;
    private IDisposable? _timerSubscription;
    private int _currentFrame;
    private bool _disposed;

    /// <summary>
    /// Creates a new spinner segment with the specified style and appearance.
    /// </summary>
    /// <param name="style">The spinner animation style.</param>
    /// <param name="color">The color of the spinner (defaults to terminal default).</param>
    /// <param name="intervalMs">The interval between frame updates in milliseconds (default: 80ms).</param>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public SpinnerSegment(SpinnerStyle style = SpinnerStyle.Dots, Color? color = null, int intervalMs = 80,
        TimeProvider? timeProvider = null)
    {
        _frames = Frames[style];
        _style = new TextStyle(color ?? Color.Default, Color.Default, TextDecoration.None);
        _intervalMs = intervalMs;
        _timeProvider = timeProvider ?? TimeProvider.System;
        Start();
    }

    /// <inheritdoc />
    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    /// <inheritdoc />
    public bool IsAnimating => _timerSubscription != null;

    /// <inheritdoc />
    public StyledSegment GetCurrentSegment()
    {
        return new StyledSegment(_frames[_currentFrame], _style);
    }

    /// <inheritdoc />
    public void Start()
    {
        if (!_disposed)
        {
            _timerSubscription ??= Observable.Interval(TimeSpan.FromMilliseconds(_intervalMs), _timeProvider)
                .Subscribe(_ =>
                {
                    _currentFrame = (_currentFrame + 1) % _frames.Length;
                    _invalidated.OnNext(Unit.Default);
                });
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        _timerSubscription?.Dispose();
        _timerSubscription = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
    }
}
