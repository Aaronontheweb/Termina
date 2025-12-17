// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Layout;

/// <summary>
/// Defines lifecycle methods for layout nodes that can be activated/deactivated.
/// Used with NavigationBehavior.PreserveState to pause nodes instead of disposing them.
/// </summary>
/// <remarks>
/// <para>
/// When a page with <see cref="Pages.NavigationBehavior.PreserveState"/> is navigated away from,
/// its layout tree should be deactivated (paused) rather than disposed. This prevents
/// ObjectDisposedException when Rx events are in-flight and allows state preservation.
/// </para>
/// <para>
/// Implementing nodes should:
/// </para>
/// <list type="bullet">
///   <item>In <see cref="OnActivate"/>: Resume subscriptions, start timers, restore active state</item>
///   <item>In <see cref="OnDeactivate"/>: Pause subscriptions, stop timers, but preserve state</item>
///   <item>In <see cref="IDisposable.Dispose"/>: Permanently release all resources</item>
/// </list>
/// </remarks>
public interface IActivatableNode : ILayoutNode
{
    /// <summary>
    /// Called when the node becomes active (page navigated to).
    /// Resume subscriptions, start timers, and restore active state.
    /// </summary>
    /// <remarks>
    /// This method is called when:
    /// <list type="bullet">
    ///   <item>The page is first navigated to (initial activation)</item>
    ///   <item>The page is navigated back to after being deactivated</item>
    /// </list>
    /// Implementations should be idempotent - calling OnActivate multiple times
    /// without intervening OnDeactivate should be safe.
    /// </remarks>
    void OnActivate();

    /// <summary>
    /// Called when the node becomes inactive (navigating away from page).
    /// Pause subscriptions, stop timers, but preserve state.
    /// </summary>
    /// <remarks>
    /// This method is called when:
    /// <list type="bullet">
    ///   <item>The user navigates away from the page</item>
    ///   <item>The page is being temporarily hidden (e.g., modal overlay)</item>
    /// </list>
    /// Implementations should:
    /// <list type="bullet">
    ///   <item>Stop resource-consuming operations (timers, animations)</item>
    ///   <item>Pause subscriptions to prevent unnecessary work</item>
    ///   <item>Preserve all state for reactivation</item>
    ///   <item>NOT dispose Subjects or complete observables</item>
    /// </list>
    /// </remarks>
    void OnDeactivate();
}
