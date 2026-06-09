// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Controls which types of filesystem entries can be selected in a <see cref="FilePickerNode"/>.
/// Folders can always be navigated into regardless of mode.
/// </summary>
public enum FilePickerMode
{
    /// <summary>
    /// Only files are selectable.
    /// </summary>
    Files,

    /// <summary>
    /// Only directories are selectable.
    /// </summary>
    Directories,

    /// <summary>
    /// Both files and directories are selectable.
    /// </summary>
    All
}
