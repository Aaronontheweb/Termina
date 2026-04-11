// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Layout;

/// <summary>
/// Overlay node that renders a "SELECTION MODE" banner at the top-center of the screen
/// whenever selection mode is active on the application.
/// </summary>
/// <remarks>
/// Designed to overlay the current page via <see cref="StackLayout"/> — does not consume
/// space, only paints a single row at the top of its bounds.
/// </remarks>
internal sealed class SelectionModeIndicatorNode : LayoutNode, IInvalidatingNode
{
    private const string Message = " SELECTION MODE — drag to select, Esc to exit ";

    private readonly Subject<Unit> _invalidated = new();
    private readonly IDisposable _subscription;
    private bool _active;
    private bool _disposed;

    public SelectionModeIndicatorNode(Observable<bool> activeState)
    {
        WidthConstraint = new SizeConstraint.Fill();
        HeightConstraint = new SizeConstraint.Fill();

        _subscription = activeState.Subscribe(active =>
        {
            _active = active;
            _invalidated.OnNext(Unit.Default);
        });
    }

    public Observable<Unit> Invalidated => _invalidated;

    public override Size Measure(Size available) => available;

    public override void Render(IRenderContext context, Rect bounds)
    {
        if (_active && bounds.HasArea && bounds.Height >= 1)
        {
            var width = Math.Min(bounds.Width, Message.Length);
            var x = Math.Max(0, (bounds.Width - width) / 2);
            var text = width < Message.Length ? Message[..width] : Message;

            context.SetBackground(Color.Yellow);
            context.SetForeground(Color.Black);
            context.SetDecoration(TextDecoration.Bold);
            context.WriteAt(x, 0, text);
            context.ResetColors();
        }
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _subscription.Dispose();
        _invalidated.OnCompleted();
        _invalidated.Dispose();
        base.Dispose();
    }
}
