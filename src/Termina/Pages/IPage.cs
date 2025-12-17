// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Layout;
using Termina.Reactive;

namespace Termina.Pages;

/// <summary>
/// Represents a page (screen) in the TUI application.
/// Pages render UI and handle lifecycle events.
/// </summary>
public interface IPage
{
    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// Use this to initialize or refresh component state.
    /// </summary>
    void OnNavigatedTo();

    /// <summary>
    /// Called when the page is about to become inactive (navigating away).
    /// Use this to save state or clean up.
    /// </summary>
    void OnNavigatingFrom();

    /// <summary>
    /// Build the layout tree for this page.
    /// </summary>
    ILayoutNode BuildLayout();
}

/// <summary>
/// Internal interface for reactive pages that can be bound to ViewModels.
/// This allows AOT-compatible binding without reflection.
/// </summary>
internal interface IBindablePage : IPage
{
    /// <summary>
    /// Binds a ViewModel to this page.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind.</param>
    void BindViewModel(ReactiveViewModel viewModel);

    /// <summary>
    /// Wires up focus management to this page.
    /// </summary>
    /// <param name="focusManager">The focus manager.</param>
    void WireUpFocus(Input.IFocusManager focusManager);

    /// <summary>
    /// Gets the current cached layout root, if any.
    /// </summary>
    ILayoutNode? LayoutRoot { get; }
}
