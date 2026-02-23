// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Interface for components that can accept pasted content from the terminal.
/// </summary>
/// <remarks>
/// Implement this interface on focusable layout nodes (such as <c>TextInputNode</c>)
/// to receive bracketed paste events. When a focused component implements this interface,
/// <c>TerminaApplication</c> routes <see cref="PasteEvent"/>s directly to it rather
/// than broadcasting them through the input observable.
/// </remarks>
public interface IPasteReceiver
{
    /// <summary>
    /// Handle a paste event from the terminal.
    /// </summary>
    /// <param name="paste">The paste event containing the pasted text.</param>
    /// <returns>
    /// <see langword="true"/> if the paste was handled; <see langword="false"/> otherwise.
    /// </returns>
    bool HandlePaste(PasteEvent paste);
}
