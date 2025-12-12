using System.Reactive.Disposables;
using Termina.Input;

namespace Termina.Reactive;

/// <summary>
/// Base class for reactive view models in Termina.
/// ViewModels contain application state and logic, exposing observable properties
/// that pages subscribe to for automatic UI updates.
/// </summary>
/// <remarks>
/// <para>
/// ReactiveViewModel is the "ViewModel" in MVVM pattern. It:
/// </para>
/// <list type="bullet">
///   <item>Owns application state as observable properties (use [Reactive] attribute)</item>
///   <item>Subscribes to input events and backend services</item>
///   <item>Provides navigation and shutdown actions</item>
///   <item>Manages subscription lifecycle via CompositeDisposable</item>
/// </list>
/// <para>
/// Properties marked with [Reactive] get source-generated BehaviorSubject backing.
/// Pages subscribe to the generated *Changed observables to update UI automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public partial class CounterViewModel : ReactiveViewModel
/// {
///     [Reactive] private int _count;
///
///     public CounterViewModel(IObservable&lt;IInputEvent&gt; input)
///     {
///         input.OfType&lt;KeyPressed&gt;()
///             .Subscribe(HandleKey)
///             .DisposeWith(Subscriptions);
///     }
///
///     private void HandleKey(KeyPressed key)
///     {
///         if (key.KeyInfo.Key == ConsoleKey.UpArrow)
///             Count++;
///     }
/// }
/// </code>
/// </example>
public abstract class ReactiveViewModel : IDisposable
{
    private readonly CompositeDisposable _subscriptions = new();

    /// <summary>
    /// Composite disposable for managing subscriptions.
    /// Use <see cref="RxExtensions.DisposeWith{T}"/> to add subscriptions.
    /// All subscriptions are disposed when the ViewModel is disposed.
    /// </summary>
    protected CompositeDisposable Subscriptions => _subscriptions;

    /// <summary>
    /// Navigate to another page by path.
    /// Set by the framework when the ViewModel is bound to a page.
    /// </summary>
    /// <example>
    /// <code>
    /// Navigate("/todos/42");
    /// Navigate("/");
    /// </code>
    /// </example>
    protected Action<string> Navigate { get; private set; } = _ => { };

    /// <summary>
    /// Navigate to another page using a route template and values.
    /// Set by the framework when the ViewModel is bound to a page.
    /// </summary>
    /// <example>
    /// <code>
    /// NavigateWithParams("/todos/{id}", new { id = 42 });
    /// </code>
    /// </example>
    protected Action<string, object?> NavigateWithParams { get; private set; } = (_, _) => { };

    /// <summary>
    /// Request graceful application shutdown.
    /// Set by the framework when the ViewModel is bound to a page.
    /// </summary>
    protected Action Shutdown { get; private set; } = () => { };

    /// <summary>
    /// Request a UI redraw. Use this when content changes asynchronously
    /// (e.g., from streaming data) and the display needs to be refreshed.
    /// </summary>
    protected Action RequestRedraw { get; private set; } = () => { };

    /// <summary>
    /// Observable stream of input events from the application.
    /// Subscribe to this to handle keyboard input in the ViewModel.
    /// </summary>
    protected IObservable<IInputEvent> Input { get; private set; } = null!;

    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// Override to perform initialization that should happen each time the page is shown.
    /// </summary>
    public virtual void OnActivated()
    {
    }

    /// <summary>
    /// Called when the page is being deactivated (navigating away).
    /// Override to perform cleanup or state saving.
    /// </summary>
    public virtual void OnDeactivating()
    {
    }

    /// <summary>
    /// Disposes all subscriptions.
    /// Called by the framework when the ViewModel is no longer needed.
    /// </summary>
    public virtual void Dispose()
    {
        _subscriptions.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Wires up the navigation, shutdown actions, and input observable.
    /// Called by the framework when binding to a page.
    /// </summary>
    internal void WireUp(
        Action<string> navigate,
        Action<string, object?> navigateWithParams,
        Action shutdown,
        Action requestRedraw,
        IObservable<IInputEvent> input)
    {
        Navigate = navigate;
        NavigateWithParams = navigateWithParams;
        Shutdown = shutdown;
        RequestRedraw = requestRedraw;
        Input = input;
    }
}
