using System.Reactive.Disposables;
using Spectre.Console.Rendering;
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
///   <item>Subscribes to ViewModel property changes in <see cref="OnBound"/></item>
///   <item>Updates component state when properties change</item>
///   <item>Renders components via Spectre.Console</item>
///   <item>Manages subscription lifecycle automatically</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class CounterPage : ReactivePage&lt;CounterViewModel&gt;
/// {
///     protected override void OnBound()
///     {
///         ViewModel.CountChanged
///             .Subscribe(count => _display.Text = $"Count: {count}")
///             .DisposeWith(Subscriptions);
///     }
///
///     public override IRenderable Render() => _display.Render();
/// }
/// </code>
/// </example>
public abstract class ReactivePage<TViewModel> : IBindablePage
    where TViewModel : ReactiveViewModel
{
    private readonly CompositeDisposable _subscriptions = new();

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
    /// Called when the ViewModel is bound to this page.
    /// Set up subscriptions to ViewModel properties here.
    /// </summary>
    protected abstract void OnBound();

    /// <summary>
    /// Render the page as a Spectre.Console renderable.
    /// </summary>
    public abstract IRenderable Render();

    /// <summary>
    /// Binds the ViewModel to this page (interface implementation for AOT compatibility).
    /// </summary>
    void IBindablePage.BindViewModel(ReactiveViewModel viewModel)
    {
        Bind((TViewModel)viewModel);
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
        // Override in derived classes if needed
    }

    /// <summary>
    /// Called when the page is being deactivated (navigating away).
    /// Clears page subscriptions automatically.
    /// </summary>
    public virtual void OnNavigatingFrom()
    {
        _subscriptions.Clear();
    }
}
