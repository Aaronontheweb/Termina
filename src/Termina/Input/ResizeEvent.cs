// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Terminal resize event.
/// Fired when the terminal window dimensions change.
/// </summary>
/// <param name="Width">New terminal width in columns.</param>
/// <param name="Height">New terminal height in rows.</param>
public sealed record ResizeEvent(int Width, int Height) : IInputEvent;
