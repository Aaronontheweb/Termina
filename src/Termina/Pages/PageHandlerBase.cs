using Termina.Events;
using Termina.Input;

namespace Termina.Pages;

/// <summary>
/// Base class for page handlers in the Termina two-tier event architecture.
/// Handlers process UI events from pages and can inject any services they need.
/// </summary>
/// <typeparam name="TPage">The page type this handler controls.</typeparam>
/// <remarks>
/// <para>
/// Handlers receive UI events from the page and react by:
/// </para>
/// <list type="bullet">
///   <item>Sending commands to update the page UI</item>
///   <item>Calling injected services (actors, APIs, etc.) directly</item>
///   <item>Navigating to other pages</item>
///   <item>Requesting application shutdown</item>
/// </list>
/// <para>
/// Handlers are resolved from DI and can inject any services they need.
/// The framework only requires handlers to implement <see cref="IPageHandler"/>
/// and respond to UI events via HandleUIEvent.
/// </para>
/// </remarks>
public abstract class PageHandler<TPage>
    where TPage : IPage
{
    /// <summary>
    /// The page this handler controls.
    /// Set by the framework when the handler is attached to a page.
    /// </summary>
    internal TPage Page { get; set; } = default!;

    /// <summary>
    /// Action to navigate to another page.
    /// Set by the framework.
    /// </summary>
    internal Action<string> NavigateAction { get; set; } = default!;

    /// <summary>
    /// Action to request application shutdown.
    /// Set by the framework.
    /// </summary>
    internal Action ShutdownAction { get; set; } = default!;

    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// Override to perform initialization logic.
    /// </summary>
    protected virtual void OnNavigatedTo() { }

    /// <summary>
    /// Called when the page is about to become inactive (navigating away).
    /// Override to perform cleanup logic.
    /// </summary>
    protected virtual void OnNavigatingFrom() { }

    /// <summary>
    /// Navigate to another page by key.
    /// </summary>
    /// <param name="pageKey">The key of the page to navigate to.</param>
    protected void Navigate(string pageKey) => NavigateAction(pageKey);

    /// <summary>
    /// Request graceful application shutdown.
    /// </summary>
    protected void Shutdown() => ShutdownAction();

    /// <summary>
    /// Internal method called by the framework when page becomes active.
    /// </summary>
    internal void InvokeOnNavigatedTo() => OnNavigatedTo();

    /// <summary>
    /// Internal method called by the framework when page becomes inactive.
    /// </summary>
    internal void InvokeOnNavigatingFrom() => OnNavigatingFrom();
}

/// <summary>
/// Base class for page handlers with typed UI events and commands.
/// </summary>
/// <typeparam name="TPage">The page type this handler controls.</typeparam>
/// <typeparam name="TUIEvent">The typed UI event type (user interactions).</typeparam>
/// <typeparam name="TCommand">The command type sent to the page.</typeparam>
/// <remarks>
/// <para>
/// Handlers are resolved from DI and can inject any services they need.
/// Use constructor injection to get actors, services, or other dependencies.
/// </para>
/// <example>
/// <code>
/// public class SettingsHandler : PageHandler&lt;SettingsPage, SettingsUIEvent, SettingsCommand&gt;
/// {
///     private readonly ISettingsService _settings;
///
///     public SettingsHandler(ISettingsService settings)
///     {
///         _settings = settings;
///     }
///
///     protected override void HandleUIEvent(SettingsUIEvent evt) { ... }
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class PageHandler<TPage, TUIEvent, TCommand>
    : PageHandler<TPage>, IPageHandler
    where TPage : PageBase<TUIEvent, TCommand>, new()
    where TUIEvent : IPageUIEvent
    where TCommand : IUICommand
{
    /// <inheritdoc />
    public static PageRegistration CreateRegistration(string pageKey, NavigationBehavior behavior)
    {
        return new PageRegistration
        {
            PageFactory = () => new TPage(),
            HandlerFactory = null!, // Filled in by TerminaApplication.RegisterPage
            Behavior = behavior,
            WireUpHandler = (handler, page, navigate, shutdown) =>
            {
                var typedHandler = (PageHandler<TPage, TUIEvent, TCommand>)handler;
                typedHandler.Page = (TPage)page;
                typedHandler.NavigateAction = navigate;
                typedHandler.ShutdownAction = shutdown;
            },
            InvokeHandleUIEvent = (handler, evt) =>
            {
                var typedHandler = (PageHandler<TPage, TUIEvent, TCommand>)handler;
                typedHandler.InvokeHandleUIEvent((TUIEvent)evt);
            },
            InvokeOnNavigatedTo = handler =>
            {
                var typedHandler = (PageHandler<TPage, TUIEvent, TCommand>)handler;
                typedHandler.InvokeOnNavigatedTo();
            },
            InvokeOnNavigatingFrom = handler =>
            {
                var typedHandler = (PageHandler<TPage, TUIEvent, TCommand>)handler;
                typedHandler.InvokeOnNavigatingFrom();
            },
            TransformInput = (page, raw) =>
            {
                var typedPage = (TPage)page;
                return typedPage.TransformInput(raw);
            }
        };
    }

    /// <summary>
    /// Handle a UI event from the page (user interaction).
    /// </summary>
    /// <param name="evt">The typed UI event.</param>
    protected abstract void HandleUIEvent(TUIEvent evt);

    /// <summary>
    /// Send a command to the page to update UI state.
    /// </summary>
    /// <param name="command">The command to send.</param>
    protected void Send(TCommand command) => Page.ReceiveCommand(command);

    /// <summary>
    /// Internal method called by the framework to handle frontend events.
    /// </summary>
    internal void InvokeHandleUIEvent(TUIEvent evt) => HandleUIEvent(evt);
}
