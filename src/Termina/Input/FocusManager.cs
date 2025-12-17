// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Termina.Diagnostics;
using Termina.Layout;

namespace Termina.Input;

/// <summary>
/// Manages keyboard focus for interactive components using a stack-based model.
/// </summary>
/// <remarks>
/// The focus manager supports nested modals through its stack-based design:
/// when a modal opens, it pushes itself onto the stack and captures all input.
/// When it closes, it pops itself off and focus returns to the previous component.
/// </remarks>
public sealed class FocusManager : IFocusManager, IDisposable
{
    private readonly Stack<IFocusable> _focusStack = new();
    private readonly BehaviorSubject<IFocusable?> _focusChanged = new(null);
    private bool _disposed;

    /// <inheritdoc />
    public IObservable<IFocusable?> FocusChanged => _focusChanged.AsObservable();

    /// <inheritdoc />
    public IFocusable? CurrentFocus => _focusStack.Count > 0 ? _focusStack.Peek() : null;

    /// <inheritdoc />
    public void PushFocus(IFocusable focusable)
    {
        ArgumentNullException.ThrowIfNull(focusable);

        if (!focusable.CanFocus)
        {
            TerminaTrace.Focus.Debug(this, "PushFocus rejected: {0} CanFocus=false", focusable.GetType().Name);
            return;
        }

        // Blur the current focus if any
        if (_focusStack.Count > 0)
        {
            var current = _focusStack.Peek();
            TerminaTrace.Focus.Debug(this, "Blurring current: {0}", current.GetType().Name);
            current.OnBlurred();
        }

        // Push and focus the new component
        _focusStack.Push(focusable);
        focusable.OnFocused();
        _focusChanged.OnNext(focusable);
        TerminaTrace.Focus.Debug(this, "PushFocus: {0}, stack depth={1}", focusable.GetType().Name, _focusStack.Count);
    }

    /// <inheritdoc />
    public void PopFocus()
    {
        if (_focusStack.Count == 0)
        {
            TerminaTrace.Focus.Debug(this, "PopFocus: stack empty, nothing to pop");
            return;
        }

        // Blur and remove the current focus
        var current = _focusStack.Pop();
        TerminaTrace.Focus.Debug(this, "PopFocus: popped {0}, stack depth={1}", current.GetType().Name, _focusStack.Count);
        current.OnBlurred();

        // Focus the previous component if any
        if (_focusStack.Count > 0)
        {
            var previous = _focusStack.Peek();
            TerminaTrace.Focus.Debug(this, "PopFocus: restoring focus to {0}", previous.GetType().Name);
            previous.OnFocused();
            _focusChanged.OnNext(previous);
        }
        else
        {
            TerminaTrace.Focus.Debug(this, "PopFocus: no previous focus, stack now empty");
            _focusChanged.OnNext(null);
        }
    }

    /// <inheritdoc />
    public void SetFocus(IFocusable focusable)
    {
        ArgumentNullException.ThrowIfNull(focusable);

        if (!focusable.CanFocus)
            return;

        // If the stack is empty, just push
        if (_focusStack.Count == 0)
        {
            PushFocus(focusable);
            return;
        }

        // Replace the top of the stack
        var current = _focusStack.Pop();
        current.OnBlurred();

        _focusStack.Push(focusable);
        focusable.OnFocused();
        _focusChanged.OnNext(focusable);
    }

    /// <inheritdoc />
    public void ClearFocus()
    {
        // Blur all focused components
        while (_focusStack.Count > 0)
        {
            var current = _focusStack.Pop();
            current.OnBlurred();
        }

        _focusChanged.OnNext(null);
    }

    /// <inheritdoc />
    public bool RouteInput(ConsoleKeyInfo key)
    {
        if (_focusStack.Count == 0)
        {
            TerminaTrace.Input.Trace(this, "RouteInput: no focus, key={0} not handled", key.Key);
            return false;
        }

        // Route to the topmost focused component
        var current = _focusStack.Peek();

        if (!current.CanFocus)
        {
            TerminaTrace.Input.Debug(this, "RouteInput: {0} CanFocus=false, key={1} not handled", current.GetType().Name, key.Key);
            return false;
        }

        var handled = current.HandleInput(key);
        TerminaTrace.Input.Trace(this, "RouteInput: {0} key={1} handled={2}", current.GetType().Name, key.Key, handled);
        return handled;
    }

    /// <summary>
    /// Disposes the focus manager and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        ClearFocus();
        _focusChanged.OnCompleted();
        _focusChanged.Dispose();
        _disposed = true;
    }
}
