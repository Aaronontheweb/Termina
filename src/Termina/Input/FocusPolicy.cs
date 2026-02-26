// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Determines how focus is automatically assigned when a page is navigated to.
/// </summary>
public enum FocusPolicy
{
    /// <summary>
    /// No automatic focus assignment. The page must manually call
    /// <see cref="IFocusManager.PushFocus"/> or <see cref="IFocusManager.SetFocus"/>.
    /// </summary>
    Manual,

    /// <summary>
    /// Automatically focus the first focusable node found via depth-first tree walk.
    /// </summary>
    FirstFocusable,

    /// <summary>
    /// Automatically focus the focusable node with the highest <see cref="Layout.IFocusable.FocusPriority"/>.
    /// </summary>
    ByPriority
}
