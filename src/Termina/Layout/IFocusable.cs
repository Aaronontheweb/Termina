// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Interface for layout nodes that can receive keyboard focus.
/// </summary>
/// <remarks>
/// Components that implement this interface can participate in the focus management system,
/// which routes keyboard input to the appropriate component based on focus state.
/// </remarks>
public interface IFocusable : ILayoutNode
{
    /// <summary>
    /// Gets whether this component can currently receive focus.
    /// </summary>
    /// <remarks>
    /// Return false to temporarily disable focus (e.g., during loading states).
    /// </remarks>
    bool CanFocus { get; }

    /// <summary>
    /// Gets whether this component currently has focus.
    /// </summary>
    bool HasFocus { get; }

    /// <summary>
    /// Gets the focus priority. Higher values capture input first.
    /// </summary>
    /// <remarks>
    /// Use priority to establish a hierarchy of input handlers:
    /// - 100+ for modals and overlays (capture all input)
    /// - 10-99 for interactive controls
    /// - 1-9 for background handlers
    /// </remarks>
    int FocusPriority { get; }

    /// <summary>
    /// Called when this component receives focus.
    /// </summary>
    void OnFocused();

    /// <summary>
    /// Called when this component loses focus.
    /// </summary>
    void OnBlurred();

    /// <summary>
    /// Handle keyboard input.
    /// </summary>
    /// <param name="key">The key info to handle.</param>
    /// <returns>True if the input was consumed, false to pass to the next handler.</returns>
    bool HandleInput(ConsoleKeyInfo key);
}
