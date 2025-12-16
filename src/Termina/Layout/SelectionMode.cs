// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Defines the selection behavior for SelectionListNode.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Only one item can be selected at a time.
    /// </summary>
    Single,

    /// <summary>
    /// Multiple items can be selected simultaneously.
    /// </summary>
    Multi
}
