// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Type of mouse button.
/// </summary>
public enum MouseButton
{
    /// <summary>
    /// No button.
    /// </summary>
    None,

    /// <summary>
    /// Left mouse button.
    /// </summary>
    Left,

    /// <summary>
    /// Right mouse button.
    /// </summary>
    Right,

    /// <summary>
    /// Middle mouse button (scroll wheel click).
    /// </summary>
    Middle,

    /// <summary>
    /// Scroll wheel up.
    /// </summary>
    WheelUp,

    /// <summary>
    /// Scroll wheel down.
    /// </summary>
    WheelDown
}

/// <summary>
/// Type of mouse event.
/// </summary>
public enum MouseEventType
{
    /// <summary>
    /// Mouse button pressed down.
    /// </summary>
    Press,

    /// <summary>
    /// Mouse button released.
    /// </summary>
    Release,

    /// <summary>
    /// Mouse moved while button held.
    /// </summary>
    Drag,

    /// <summary>
    /// Mouse moved without button held.
    /// </summary>
    Move,

    /// <summary>
    /// Mouse wheel scrolled.
    /// </summary>
    Scroll
}

/// <summary>
/// Low-level mouse input event.
/// </summary>
/// <param name="X">X position (column) of the mouse cursor.</param>
/// <param name="Y">Y position (row) of the mouse cursor.</param>
/// <param name="Button">The mouse button involved.</param>
/// <param name="EventType">The type of mouse event.</param>
/// <param name="Modifiers">Any keyboard modifiers held during the event.</param>
public sealed record MouseEvent(
    int X,
    int Y,
    MouseButton Button,
    MouseEventType EventType,
    ConsoleModifiers Modifiers = 0) : IInputEvent;
