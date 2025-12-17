// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Platform;

/// <summary>
/// Marker interface for low-level console input events from the platform layer.
/// These events represent raw input before being transformed into <see cref="Input.IInputEvent"/>.
/// </summary>
/// <remarks>
/// Implementations should be readonly record structs to minimize GC overhead,
/// as input events are high-frequency and short-lived.
/// </remarks>
public interface IConsoleInputEvent
{
}

/// <summary>
/// Keyboard input event from the platform console.
/// </summary>
/// <param name="KeyInfo">The raw key information from the console.</param>
public readonly record struct ConsoleKeyEvent(ConsoleKeyInfo KeyInfo) : IConsoleInputEvent;

/// <summary>
/// Terminal resize event from the platform console.
/// </summary>
/// <param name="Width">New terminal width in columns.</param>
/// <param name="Height">New terminal height in rows.</param>
public readonly record struct ConsoleResizeEvent(int Width, int Height) : IConsoleInputEvent;

/// <summary>
/// Mouse input event from the platform console.
/// </summary>
/// <param name="X">Mouse X position (column).</param>
/// <param name="Y">Mouse Y position (row).</param>
/// <param name="Button">The mouse button involved.</param>
/// <param name="EventType">The type of mouse event.</param>
public readonly record struct ConsoleMouseEvent(
    int X,
    int Y,
    MouseButton Button,
    MouseEventType EventType) : IConsoleInputEvent;

/// <summary>
/// Mouse button identifiers.
/// </summary>
public enum MouseButton
{
    None = 0,
    Left = 1,
    Middle = 2,
    Right = 3,
    ScrollUp = 4,
    ScrollDown = 5
}

/// <summary>
/// Type of mouse event.
/// </summary>
public enum MouseEventType
{
    Press,
    Release,
    Move,
    Scroll
}
