// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Termina.Input;
using Termina.Layout;
using Termina.Pages;

namespace Termina.Reactive;

/// <summary>
/// Base class for pages that bind to reactive view models.
/// Pages subscribe to ViewModel observable properties and update components accordingly.
/// </summary>
/// <typeparam name="TViewModel">The ViewModel type this page binds to.</typeparam>
/// <remarks>
/// <para>
/// ReactivePage is the "View" in MVVM pattern. It:
/// </para>
/// <list type="bullet">
///   <item>Builds a declarative layout tree in <see cref="BuildLayout"/></item>
///   <item>Uses observable bindings to automatically update when ViewModel changes</item>
///   <item>Manages subscription lifecycle automatically</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class CounterPage : ReactivePage&lt;CounterViewModel&gt;
/// {
///     public override ILayoutNode BuildLayout()
///     {
///         return Layout.Vertical()
///             .WithChild(ViewModel.CountChanged.Select(c => $"Count: {c}").AsLayout())
///             .WithChild(new TextNode("[+] Increment  [-] Decrement  [Q] Quit"));
///     }
/// }
/// </code>
/// </example>
public abstract class ReactivePage<TViewModel> : IBindablePage
    where TViewModel : ReactiveViewModel
{
    private readonly CompositeDisposable _subscriptions = new();
    private ILayoutNode? _layoutRoot;

    /// <summary>
    /// The ViewModel this page is bound to.
    /// Available after <see cref="Bind"/> is called by the framework.
    /// </summary>
    protected TViewModel ViewModel { get; private set; } = default!;

    /// <summary>
    /// Composite disposable for page subscriptions.
    /// Subscriptions are automatically cleared when navigating away.
    /// </summary>
    protected CompositeDisposable Subscriptions => _subscriptions;

    /// <summary>
    /// Focus manager for this page.
    /// Use this to manage focus for modals and interactive controls.
    /// </summary>
    protected IFocusManager Focus { get; private set; } = null!;

    /// <summary>
    /// Build the layout tree for this page.
    /// Override this to compose your page's UI declaratively.
    /// </summary>
    public abstract ILayoutNode BuildLayout();

    /// <summary>
    /// Called when the ViewModel is bound to this page.
    /// Override to perform additional setup after binding.
    /// </summary>
    protected virtual void OnBound()
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Binds the ViewModel to this page (interface implementation for AOT compatibility).
    /// </summary>
    void IBindablePage.BindViewModel(ReactiveViewModel viewModel)
    {
        Bind((TViewModel)viewModel);
    }

    /// <summary>
    /// Wires up focus management to this page.
    /// </summary>
    void IBindablePage.WireUpFocus(IFocusManager focusManager)
    {
        Focus = focusManager;
    }

    /// <summary>
    /// Binds the ViewModel to this page.
    /// Called by the framework during page initialization.
    /// </summary>
    internal void Bind(TViewModel viewModel)
    {
        ViewModel = viewModel;
        OnBound();
    }

    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// Override to perform page-specific initialization.
    /// </summary>
    public virtual void OnNavigatedTo()
    {
        // Build the layout tree when the page becomes active
        _layoutRoot?.Dispose();
        _layoutRoot = BuildLayout();
    }

    /// <summary>
    /// Called when the page is being deactivated (navigating away).
    /// Clears page subscriptions automatically.
    /// </summary>
    public virtual void OnNavigatingFrom()
    {
        _subscriptions.Clear();
        _layoutRoot?.Dispose();
        _layoutRoot = null;
    }

    /// <summary>
    /// Gets the current layout root for rendering.
    /// </summary>
    ILayoutNode? IBindablePage.LayoutRoot => _layoutRoot;
}
