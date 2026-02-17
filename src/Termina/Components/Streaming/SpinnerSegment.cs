// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Timers;
using R3;
using Termina.Terminal;
using Timer = System.Timers.Timer;

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

    private readonly Timer _timer;
    private readonly string[] _frames;
    private readonly Subject<Unit> _invalidated = new();
    private readonly TextStyle _style;
    private int _currentFrame;
    private bool _disposed;

    /// <summary>
    /// Creates a new spinner segment with the specified style and appearance.
    /// </summary>
    /// <param name="style">The spinner animation style.</param>
    /// <param name="color">The color of the spinner (defaults to terminal default).</param>
    /// <param name="intervalMs">The interval between frame updates in milliseconds (default: 80ms).</param>
    public SpinnerSegment(SpinnerStyle style = SpinnerStyle.Dots, Color? color = null, int intervalMs = 80)
    {
        _frames = Frames[style];
        _style = new TextStyle(color ?? Color.Default, Color.Default, TextDecoration.None);
        _timer = new Timer(intervalMs);
        _timer.Elapsed += OnTimerTick;
        _timer.AutoReset = true;
        Start();
    }

    /// <inheritdoc />
    public Observable<Unit> Invalidated => _invalidated.AsObservable();

    /// <inheritdoc />
    public bool IsAnimating => _timer.Enabled;

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
            _timer.Start();
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        _timer.Stop();
    }

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        _currentFrame = (_currentFrame + 1) % _frames.Length;
        _invalidated.OnNext(Unit.Default);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _timer.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
    }
}
