// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Rendering;

/// <summary>
/// Interface for components that can be rendered to a terminal.
/// Components should render relative to (0,0) - the framework will translate
/// coordinates to the actual screen position.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// Render this component to the given context.
    /// The context provides the available width and height, and
    /// all coordinates should be relative to (0,0).
    /// </summary>
    /// <param name="context">The render context to draw to.</param>
    void Render(IRenderContext context);

    /// <summary>
    /// Calculate the desired size for this component.
    /// </summary>
    /// <param name="availableWidth">The maximum available width.</param>
    /// <param name="availableHeight">The maximum available height.</param>
    /// <returns>The desired size (width, height) for this component.</returns>
    (int Width, int Height) Measure(int availableWidth, int availableHeight);
}
