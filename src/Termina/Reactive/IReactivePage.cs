// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Rendering;

namespace Termina.Reactive;

/// <summary>
/// Represents a v2 reactive page that uses region-based rendering.
/// Pages define regions and wire ViewModel observables to those regions.
/// </summary>
public interface IReactivePage
{
    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// </summary>
    void OnNavigatedTo();

    /// <summary>
    /// Called when the page is about to become inactive (navigating away).
    /// </summary>
    void OnNavigatingFrom();

    /// <summary>
    /// Get the regions defined by this page.
    /// Called by the framework to register regions with the RenderCoordinator.
    /// </summary>
    IEnumerable<Region> GetRegions();

    /// <summary>
    /// Wire up observable subscriptions to regions.
    /// Called after the page is bound to a ViewModel and regions are registered.
    /// Use .RenderTo() to bind observables to regions.
    /// </summary>
    /// <param name="coordinator">The render coordinator to render to.</param>
    void WireRegions(RenderCoordinator coordinator);
}

/// <summary>
/// Internal interface for reactive pages that can be bound to ViewModels.
/// Allows AOT-compatible binding without reflection.
/// </summary>
internal interface IBindableReactivePage : IReactivePage
{
    /// <summary>
    /// Binds a ViewModel to this page.
    /// </summary>
    void BindViewModel(ReactiveViewModel viewModel);
}
