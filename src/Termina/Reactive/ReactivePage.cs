// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
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
public abstract class ReactivePage<TViewModel> : IBindablePage, IDisposable
    where TViewModel : ReactiveViewModel
{
    private readonly CompositeDisposable _subscriptions = new();
    private readonly PageKeyBindings _keyBindings = new();
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
    /// Key bindings for this page.
    /// Register bindings to intercept keys before they reach focused components.
    /// This implements a "capture phase" for input handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Key bindings registered here are checked before the focused component
    /// receives input. This solves the common problem where components consume
    /// keys (like Escape) that the page needs for navigation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override void OnNavigatedTo()
    /// {
    ///     base.OnNavigatedTo();
    ///     KeyBindings.Register(ConsoleKey.Escape, () => ViewModel.Navigate("/menu"));
    ///     KeyBindings.Register(ConsoleKey.Tab, () => CycleFocus());
    /// }
    /// </code>
    /// </example>
    protected PageKeyBindings KeyBindings => _keyBindings;

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
    /// Handles page-level keyboard input before it reaches focused components.
    /// Override this method for custom page-level input handling beyond key bindings.
    /// </summary>
    /// <param name="keyInfo">The key press information.</param>
    /// <returns>True if the page handled the input, false to let focused components handle it.</returns>
    public virtual bool HandlePageInput(ConsoleKeyInfo keyInfo)
    {
        // Check registered key bindings first
        return _keyBindings.TryHandle(keyInfo);
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
        // Build layout once on first navigation, reactivate on subsequent visits
        if (_layoutRoot == null)
        {
            _layoutRoot = BuildLayout();

            // Subscribe to layout invalidation events to trigger redraws
            // This is the bridge between reactive layout nodes and the render loop
            if (_layoutRoot is IInvalidatingNode invalidating)
            {
                invalidating.Invalidated
                    .Subscribe(_ => ViewModel.RequestRedraw())
                    .DisposeWith(_subscriptions);
            }
        }

        // Activate the layout tree (resume subscriptions, timers, etc.)
        if (_layoutRoot is LayoutNode node)
        {
            node.OnActivate();
        }
    }

    /// <summary>
    /// Called when the page is being deactivated (navigating away).
    /// Clears page subscriptions and deactivates the layout tree.
    /// </summary>
    public virtual void OnNavigatingFrom()
    {
        // Clear page-level subscriptions and key bindings
        _subscriptions.Clear();
        _keyBindings.Clear();

        // Deactivate layout (pause, don't dispose)
        if (_layoutRoot is LayoutNode node)
        {
            node.OnDeactivate();
        }

        // Note: _layoutRoot is NOT disposed or nulled - it's preserved for reactivation
    }

    /// <summary>
    /// Disposes the page and its layout tree.
    /// This is called when the page is truly destroyed, not just navigated away from.
    /// </summary>
    public virtual void Dispose()
    {
        // Final cleanup when page is truly destroyed (not just navigated away)
        _subscriptions.Dispose();
        _layoutRoot?.Dispose();
        _layoutRoot = null;
    }

    /// <summary>
    /// Gets the current layout root for rendering.
    /// </summary>
    ILayoutNode? IBindablePage.LayoutRoot => _layoutRoot;
}
