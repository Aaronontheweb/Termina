// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
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
            return;
        }

        // Blur the current focus if any
        if (_focusStack.Count > 0)
        {
            var current = _focusStack.Peek();
            current.OnBlurred();
        }

        // Push and focus the new component
        _focusStack.Push(focusable);
        focusable.OnFocused();
        _focusChanged.OnNext(focusable);
    }

    /// <inheritdoc />
    public void PopFocus()
    {
        if (_focusStack.Count == 0)
            return;

        // Blur and remove the current focus
        var current = _focusStack.Pop();
        current.OnBlurred();

        // Focus the previous component if any
        if (_focusStack.Count > 0)
        {
            var previous = _focusStack.Peek();
            previous.OnFocused();
            _focusChanged.OnNext(previous);
        }
        else
        {
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
            return false;
        }

        // Route to the topmost focused component
        var current = _focusStack.Peek();

        if (!current.CanFocus)
        {
            return false;
        }

        return current.HandleInput(key);
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
