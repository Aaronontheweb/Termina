// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Termina.Layout;
using Termina.Rendering;

namespace Termina.Reactive;

/// <summary>
/// Base class for v2 reactive pages that use region-based rendering.
/// Pages subscribe to ViewModel observable properties and wire them to regions.
/// </summary>
/// <typeparam name="TViewModel">The ViewModel type this page binds to.</typeparam>
/// <remarks>
/// <para>
/// ReactivePageBase is the "View" in MVVM pattern for v2 Termina. It:
/// </para>
/// <list type="bullet">
///   <item>Defines regions via <see cref="GetRegions"/></item>
///   <item>Wires ViewModel observables to regions in <see cref="OnBound"/></item>
///   <item>Uses <see cref="Extensions.ObservableExtensions.RenderTo{T}(IObservable{T}, RenderCoordinator, string)"/> for binding</item>
///   <item>Manages subscription lifecycle automatically</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class CounterPage : ReactivePageBase&lt;CounterViewModel&gt;
/// {
///     public override IEnumerable&lt;Region&gt; GetRegions()
///     {
///         yield return Region.TopRow("header", 3);
///         yield return Region.FullScreen("counter") with { Y = new LayoutConstraint.Fixed(3) };
///         yield return Region.BottomRow("status", 1);
///     }
///
///     protected override void OnBound(RenderCoordinator coordinator)
///     {
///         ViewModel.CountChanged
///             .Select(c => new Text($"Count: {c}"))
///             .RenderTo(coordinator, "counter")
///             .DisposeWith(Subscriptions);
///     }
/// }
/// </code>
/// </example>
public abstract class ReactivePageBase<TViewModel> : IBindableReactivePage
    where TViewModel : ReactiveViewModel
{
    private readonly CompositeDisposable _subscriptions = new();
    private RenderCoordinator? _coordinator;

    /// <summary>
    /// The ViewModel this page is bound to.
    /// Available after binding is complete.
    /// </summary>
    protected TViewModel ViewModel { get; private set; } = default!;

    /// <summary>
    /// Composite disposable for page subscriptions.
    /// Subscriptions are automatically cleared when navigating away.
    /// </summary>
    protected CompositeDisposable Subscriptions => _subscriptions;

    /// <summary>
    /// The render coordinator for this page.
    /// Available after WireRegions is called.
    /// </summary>
    protected RenderCoordinator Coordinator => _coordinator
        ?? throw new InvalidOperationException("Coordinator not available until WireRegions is called");

    /// <summary>
    /// Define the regions for this page.
    /// </summary>
    public abstract IEnumerable<Region> GetRegions();

    /// <summary>
    /// Called when the ViewModel is bound and regions are ready.
    /// Set up subscriptions to ViewModel properties here using .RenderTo().
    /// </summary>
    /// <param name="coordinator">The render coordinator to render to.</param>
    protected abstract void OnBound(RenderCoordinator coordinator);

    /// <summary>
    /// Binds the ViewModel to this page (interface implementation).
    /// </summary>
    void IBindableReactivePage.BindViewModel(ReactiveViewModel viewModel)
    {
        ViewModel = (TViewModel)viewModel;
    }

    /// <summary>
    /// Wire up observable subscriptions to regions.
    /// </summary>
    public void WireRegions(RenderCoordinator coordinator)
    {
        _coordinator = coordinator;
        OnBound(coordinator);
    }

    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// Override to perform page-specific initialization.
    /// </summary>
    public virtual void OnNavigatedTo()
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Called when the page is being deactivated (navigating away).
    /// Clears page subscriptions automatically.
    /// </summary>
    public virtual void OnNavigatingFrom()
    {
        _subscriptions.Clear();
        _coordinator = null;
    }
}
