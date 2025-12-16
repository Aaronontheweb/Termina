// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Rendering;

namespace Termina.Layout;

/// <summary>
/// A layout node that delegates to a lazily-obtained node without owning or disposing it.
/// </summary>
/// <remarks>
/// <para>
/// Use this wrapper when you need to render a node that is owned elsewhere (like a ViewModel)
/// and should not be disposed when the layout changes. This is particularly useful for modals
/// and other overlay components that need to be shown/hidden without being recreated.
/// </para>
/// <para>
/// Example usage with reactive layouts:
/// <code>
/// ViewModel.ShowModalChanged
///     .Select(show => show
///         ? new DeferredNode(() => ViewModel.MyModal)
///         : Layouts.Empty())
///     .AsLayout()
/// </code>
/// </para>
/// </remarks>
public sealed class DeferredNode : ILayoutNode
{
    private readonly Func<ILayoutNode?> _getNode;

    /// <summary>
    /// Creates a new DeferredNode that delegates to the node returned by the factory.
    /// </summary>
    /// <param name="getNode">A function that returns the node to render, or null for no content.</param>
    public DeferredNode(Func<ILayoutNode?> getNode)
    {
        _getNode = getNode ?? throw new ArgumentNullException(nameof(getNode));
    }

    /// <inheritdoc />
    public SizeConstraint WidthConstraint => _getNode()?.WidthConstraint ?? SizeConstraint.AutoSize();

    /// <inheritdoc />
    public SizeConstraint HeightConstraint => _getNode()?.HeightConstraint ?? SizeConstraint.AutoSize();

    /// <inheritdoc />
    public Size Measure(Size available)
    {
        return _getNode()?.Measure(available) ?? Size.Zero;
    }

    /// <inheritdoc />
    public void Render(IRenderContext context, Rect bounds)
    {
        _getNode()?.Render(context, bounds);
    }

    /// <summary>
    /// Does not dispose the underlying node - it is owned by the caller.
    /// </summary>
    public void Dispose()
    {
        // Intentionally empty - we don't own the node
    }
}
