using R3;
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
///   <item>Owns application state as <c>ReactiveProperty&lt;T&gt;</c> properties</item>
///   <item>Subscribes to input events and backend services</item>
///   <item>Provides navigation and shutdown actions</item>
///   <item>Manages subscription lifecycle via CompositeDisposable</item>
/// </list>
/// <para>
/// Use <c>ReactiveProperty&lt;T&gt;</c> for observable state. Pages subscribe directly
/// to the property (which is an <c>Observable&lt;T&gt;</c>) for automatic UI updates.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CounterViewModel : ReactiveViewModel
/// {
///     public ReactiveProperty&lt;int&gt; Count { get; } = new(0);
///
///     public override void OnActivated()
///     {
///         Input.OfType&lt;IInputEvent, KeyPressed&gt;()
///             .Subscribe(HandleKey)
///             .DisposeWith(Subscriptions);
///     }
///
///     private void HandleKey(KeyPressed key)
///     {
///         if (key.KeyInfo.Key == ConsoleKey.UpArrow)
///             Count.Value++;
///     }
///
///     public override void Dispose()
///     {
///         Count.Dispose();
///         base.Dispose();
///     }
/// }
/// </code>
/// </example>
public abstract class ReactiveViewModel : IDisposable
{
    private CompositeDisposable _subscriptions = new();

    /// <summary>
    /// Composite disposable for managing subscriptions.
    /// Use <see cref="RxExtensions.DisposeWith{T}"/> to add subscriptions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscriptions added in <see cref="OnActivated"/> are automatically disposed when
    /// <see cref="OnDeactivating"/> is called. This prevents duplicate subscriptions when using
    /// <see cref="Pages.NavigationBehavior.PreserveState"/>.
    /// </para>
    /// <para>
    /// For subscriptions that should persist across activations (e.g., subscriptions created in
    /// the constructor), store the disposable manually and dispose it in <see cref="Dispose"/>.
    /// </para>
    /// </remarks>
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
    /// Public to allow Pages to trigger redraws on layout invalidation events.
    /// </summary>
    public Action RequestRedraw { get; private set; } = () => { };

    /// <summary>
    /// Observable stream of input events from the application.
    /// Subscribe to this in the ViewModel to handle keyboard input,
    /// or access from the Page to route input to interactive layout nodes.
    /// </summary>
    public Observable<IInputEvent> Input { get; private set; } = null!;

    /// <summary>
    /// Request graceful application shutdown.
    /// Called by Pages in response to user input (e.g., Ctrl+Q, Escape).
    /// </summary>
    /// <remarks>
    /// Override this method to add custom shutdown behavior such as
    /// confirmation dialogs, saving state, or cleanup operations.
    /// The default implementation calls <see cref="Shutdown"/> directly.
    /// </remarks>
    public virtual void RequestShutdown() => Shutdown();

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
    /// <remarks>
    /// The base implementation disposes all <see cref="Subscriptions"/> to prevent
    /// duplicate subscriptions when using <see cref="Pages.NavigationBehavior.PreserveState"/>.
    /// If you override this method, always call the base implementation.
    /// </remarks>
    public virtual void OnDeactivating()
    {
        // Dispose subscriptions and create a new container for next activation
        // This prevents duplicate subscriptions with PreserveState navigation
        _subscriptions.Dispose();
        _subscriptions = new CompositeDisposable();
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
        Observable<IInputEvent> input)
    {
        Navigate = navigate;
        NavigateWithParams = navigateWithParams;
        Shutdown = shutdown;
        RequestRedraw = requestRedraw;
        Input = input;
    }
}
