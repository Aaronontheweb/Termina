// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Input;
using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// Interface for layout nodes that can handle mouse events.
/// </summary>
public interface IMouseHandler
{
    /// <summary>
    /// Handles a mouse event.
    /// </summary>
    /// <param name="mouseEvent">The mouse event to handle.</param>
    /// <param name="bounds">The bounds of this node in the terminal.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    bool HandleMouseEvent(MouseEvent mouseEvent, Rect bounds);
}
