// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Layout;

namespace Termina;

/// <summary>
/// Base class for all UI components in Termina.
/// Components are simple, reusable building blocks that render to the layout tree.
/// </summary>
/// <remarks>
/// <para>
/// Components in the reactive architecture are simpler than before:
/// </para>
/// <list type="bullet">
///   <item>They render UI based on their current state</item>
///   <item>State is updated by Pages that subscribe to ViewModel observables</item>
///   <item>Components don't own state - they just render it</item>
/// </list>
/// <para>
/// Components that receive asynchronous updates (e.g., streaming data) should call
/// <see cref="MarkDirty"/> to signal that a redraw is needed. ViewModels subscribe
/// to <see cref="ContentChanged"/> and forward to the application's RequestRedraw.
/// </para>
/// </remarks>
public abstract class Component
{
    private readonly Subject<Unit> _contentChanged = new();

    /// <summary>
    /// Observable that emits when the component's content changes and needs to be redrawn.
    /// ViewModels should subscribe to this and call RequestRedraw() when it emits.
    /// </summary>
    /// <remarks>
    /// This provides a marshalling mechanism for async backend operations to signal
    /// that the UI needs to be updated, similar to Control.Invoke in WinForms or
    /// Dispatcher.Invoke in WPF.
    /// </remarks>
    public Observable<Unit> ContentChanged => _contentChanged;

    /// <summary>
    /// Marks this component as needing a redraw.
    /// Call this when async operations modify the component's state.
    /// </summary>
    protected void MarkDirty()
    {
        _contentChanged.OnNext(Unit.Default);
    }

    /// <summary>
    /// Render this component as a layout node.
    /// Called after state changes to update the display.
    /// </summary>
    public abstract ILayoutNode Render();
}
