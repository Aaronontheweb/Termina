// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
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
    /// Determines how focus is automatically assigned when this page is navigated to.
    /// Set in the page constructor or <see cref="OnBound"/> method.
    /// </summary>
    protected FocusPolicy FocusPolicy { get; set; } = FocusPolicy.Manual;

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

    private Action<string> _navigate = _ => { };
    private Action<string, object?> _navigateWithParams = (_, _) => { };
    private Action _shutdown = () => { };

    /// <summary>
    /// Navigates to the specified route.
    /// Use this for input-driven navigation (e.g., Escape to go back).
    /// </summary>
    /// <param name="route">The route to navigate to.</param>
    /// <example>
    /// <code>
    /// KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));
    /// </code>
    /// </example>
    protected void Navigate(string route) => _navigate(route);

    /// <summary>
    /// Navigates to the specified route with parameters.
    /// </summary>
    /// <param name="routeTemplate">The route template (e.g., "/items/{id}").</param>
    /// <param name="parameters">Anonymous object with parameter values.</param>
    protected void NavigateWithParams(string routeTemplate, object? parameters) =>
        _navigateWithParams(routeTemplate, parameters);

    /// <summary>
    /// Requests application shutdown.
    /// </summary>
    protected void Shutdown() => _shutdown();

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
    ///     KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));
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
    /// Wires up navigation capabilities to this page.
    /// </summary>
    void IBindablePage.WireUpNavigation(Action<string> navigate, Action<string, object?> navigateWithParams, Action shutdown)
    {
        _navigate = navigate;
        _navigateWithParams = navigateWithParams;
        _shutdown = shutdown;
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
    /// Handles mouse events at the page level.
    /// Override this method for custom mouse event handling.
    /// </summary>
    /// <param name="mouseEvent">The mouse event to handle.</param>
    /// <returns>True if the page handled the event, false otherwise.</returns>
    public virtual bool HandleMouseEvent(Termina.Input.MouseEvent mouseEvent)
    {
        // Default implementation: try to route to layout root if it supports mouse events
        if (_layoutRoot is IMouseHandler handler)
        {
            // Pass the bounds as the full terminal size for top-level nodes
            var bounds = new Termina.Layout.Rect(0, 0, Console.WindowWidth, Console.WindowHeight);
            return handler.HandleMouseEvent(mouseEvent, bounds);
        }

        return false;
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
        }

        // Subscribe to layout invalidation events to trigger redraws.
        // This must be outside the null check because _subscriptions is cleared
        // in OnNavigatingFrom() — the subscription must be re-created on each visit.
        if (_layoutRoot is IInvalidatingNode invalidating)
        {
            invalidating.Invalidated
                .Subscribe(_ => ViewModel.RequestRedraw())
                .DisposeWith(_subscriptions);
        }

        // Activate the layout tree (resume subscriptions, timers, etc.)
        if (_layoutRoot is LayoutNode node)
        {
            node.OnActivate();
        }

        // Auto-focus based on policy
        if (FocusPolicy != FocusPolicy.Manual && _layoutRoot != null)
        {
            ApplyFocusPolicy(_layoutRoot);
        }
    }

    /// <summary>
    /// Cycle focus forward through focusable nodes in the layout tree.
    /// Register as a key binding: <c>KeyBindings.Register(ConsoleKey.Tab, CycleFocusForward);</c>
    /// </summary>
    protected void CycleFocusForward()
    {
        if (_layoutRoot == null) return;
        var focusables = Focus.CollectFocusables(_layoutRoot);
        Focus.CycleFocus(focusables);
    }

    /// <summary>
    /// Cycle focus backward through focusable nodes in the layout tree.
    /// Register as a key binding: <c>KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, CycleFocusBackward);</c>
    /// </summary>
    protected void CycleFocusBackward()
    {
        if (_layoutRoot == null) return;
        var focusables = Focus.CollectFocusables(_layoutRoot);
        Focus.CycleFocus(focusables, reverse: true);
    }

    /// <summary>
    /// Apply the configured focus policy to the layout tree.
    /// </summary>
    private void ApplyFocusPolicy(ILayoutNode root)
    {
        var focusables = Focus.CollectFocusables(root);
        if (focusables.Count == 0)
            return;

        IFocusable? target = FocusPolicy switch
        {
            FocusPolicy.FirstFocusable => focusables[0],
            FocusPolicy.ByPriority => focusables.OrderByDescending(f => f.FocusPriority).First(),
            _ => null
        };

        if (target != null)
        {
            Focus.SetFocus(target);
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
