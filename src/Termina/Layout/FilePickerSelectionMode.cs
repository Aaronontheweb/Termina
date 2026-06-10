// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Controls single vs. multi-select behavior in a <see cref="FilePickerNode"/>.
/// </summary>
public enum FilePickerSelectionMode
{
    /// <summary>
    /// Exactly one item can be selected.
    /// </summary>
    Single,

    /// <summary>
    /// Multiple items can be toggled with Space and confirmed with Enter.
    /// </summary>
    Multi
}
