// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Input event fired when the user pastes text via the terminal's bracketed paste mode.
/// </summary>
/// <remarks>
/// This event is only emitted when bracketed paste mode is active
/// (<c>ESC[?2004h</c> has been sent to the terminal). In that mode the terminal
/// wraps pasted content between <c>ESC[200~</c> and <c>ESC[201~</c>, allowing the
/// application to receive the entire paste as a single event rather than individual
/// key presses.
/// </remarks>
/// <param name="Content">The full pasted text, including any newlines present in the paste.</param>
public sealed record PasteEvent(string Content) : IInputEvent;
