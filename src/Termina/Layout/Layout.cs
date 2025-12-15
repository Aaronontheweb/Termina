// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Factory for creating layout containers.
/// </summary>
public static class Layouts
{
    /// <summary>
    /// Create a vertical stack layout (children arranged top to bottom).
    /// </summary>
    public static VerticalLayout Vertical(params ILayoutNode[] children) => new(children);

    /// <summary>
    /// Create a horizontal layout (children arranged left to right).
    /// </summary>
    public static HorizontalLayout Horizontal(params ILayoutNode[] children) => new(children);

    /// <summary>
    /// Create a stack layout (children overlapping, last on top).
    /// </summary>
    public static StackLayout Stack(params ILayoutNode[] children) => new(children);

    /// <summary>
    /// Create an empty node (renders nothing, takes no space).
    /// </summary>
    public static EmptyNode Empty() => new();
}
