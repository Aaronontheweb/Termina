// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Defines how the modal backdrop is rendered.
/// </summary>
public enum BackdropStyle
{
    /// <summary>
    /// No backdrop - content behind is fully visible.
    /// </summary>
    Transparent,

    /// <summary>
    /// Semi-transparent dimmed backdrop using a pattern character.
    /// </summary>
    Dim,

    /// <summary>
    /// Solid colored backdrop that completely obscures content behind.
    /// </summary>
    Solid
}
