using Termina.Events;
using Termina.Input;

namespace Termina.Pages;

/// <summary>
/// Base class for page handlers in the Termina two-tier event architecture.
/// Handlers are stateless event transformers that process both frontend and backend events.
/// </summary>
/// <typeparam name="TPage">The page type this handler controls.</typeparam>
/// <remarks>
/// <para>
/// <b>IMPORTANT: Handlers must be STATELESS.</b> Do not add fields to store state.
/// All state lives in the Model layer (actors, services) or in page components.
/// </para>
/// <para>
/// Handlers are pure event transformers:
/// </para>
/// <list type="bullet">
///   <item>Receive UI events from the page → Send commands and/or publish model events</item>
///   <item>Receive model events from backend → Send commands to update UI</item>
/// </list>
/// <para>
/// Available methods in handlers:
/// </para>
/// <list type="bullet">
///   <item><see cref="Send"/> - Push a command to the page (triggers re-render)</item>
///   <item><see cref="Publish{T}"/> - Push an event to the application bus (for model layer)</item>
///   <item><see cref="Navigate"/> - Request navigation to another page</item>
///   <item><see cref="Shutdown"/> - Request application shutdown</item>
/// </list>
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
    /// The application bus for publishing model events.
    /// Set by the framework.
    /// </summary>
    internal IApplicationBus Bus { get; set; } = default!;

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
    /// Override to perform cleanup or publish cancellation events.
    /// </summary>
    protected virtual void OnNavigatingFrom() { }

    /// <summary>
    /// Publish an event to the application bus.
    /// Model layer actors/services can subscribe to these events.
    /// </summary>
    /// <typeparam name="T">The event type (must implement IModelEvent).</typeparam>
    /// <param name="evt">The event to publish.</param>
    protected void Publish<T>(T evt) where T : IModelEvent => Bus.Publish(evt);

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
/// Implements <see cref="IPageHandler{THandler}"/> for AOT-compatible registration.
/// </summary>
/// <typeparam name="THandler">The concrete handler type (CRTP pattern).</typeparam>
/// <typeparam name="TPage">The page type this handler controls.</typeparam>
/// <typeparam name="TUIEvent">The typed UI event type (user interactions).</typeparam>
/// <typeparam name="TCommand">The command type sent to the page.</typeparam>
/// <remarks>
/// <para>
/// This base class is used when the handler needs to receive UI events and send commands.
/// </para>
/// <example>
/// <code>
/// public class SettingsHandler : PageHandler&lt;SettingsHandler, SettingsPage, SettingsUIEvent, SettingsCommand&gt;
/// {
///     protected override void HandleUIEvent(SettingsUIEvent evt)
///     {
///         switch (evt)
///         {
///             case SettingsUIEvent.SaveRequested:
///                 Publish(new SaveSettings());
///                 break;
///         }
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class PageHandler<THandler, TPage, TUIEvent, TCommand>
    : PageHandler<TPage>, IPageHandler<THandler>
    where THandler : new()
    where TPage : PageBase<TUIEvent, TCommand>, new()
    where TUIEvent : IPageUIEvent
    where TCommand : IUICommand
{
    /// <inheritdoc />
    public static Type PageType => typeof(TPage);

    /// <inheritdoc />
    public static Type UIEventType => typeof(TUIEvent);

    /// <inheritdoc />
    public static Type CommandType => typeof(TCommand);

    /// <inheritdoc />
    public static PageRegistration CreateRegistration(string pageKey, NavigationBehavior behavior)
    {
        return new PageRegistration
        {
            PageFactory = () => new TPage(),
            HandlerFactory = () => new THandler(),
            Behavior = behavior,
            PageType = typeof(TPage),
            HandlerType = typeof(THandler),
            UIEventType = typeof(TUIEvent),
            CommandType = typeof(TCommand),
            WireUpHandler = (handler, page, bus, navigate, shutdown) =>
            {
                var typedHandler = (PageHandler<THandler, TPage, TUIEvent, TCommand>)handler;
                typedHandler.Page = (TPage)page;
                typedHandler.Bus = bus;
                typedHandler.NavigateAction = navigate;
                typedHandler.ShutdownAction = shutdown;
            },
            InvokeHandleUIEvent = (handler, evt) =>
            {
                var typedHandler = (PageHandler<THandler, TPage, TUIEvent, TCommand>)handler;
                typedHandler.InvokeHandleUIEvent((TUIEvent)evt);
            },
            InvokeHandleModelEvent = (handler, evt) =>
            {
                var typedHandler = (PageHandler<THandler, TPage, TUIEvent, TCommand>)handler;
                typedHandler.InvokeHandleModelEvent(evt);
            },
            InvokeOnNavigatedTo = handler =>
            {
                var typedHandler = (PageHandler<THandler, TPage, TUIEvent, TCommand>)handler;
                typedHandler.InvokeOnNavigatedTo();
            },
            InvokeOnNavigatingFrom = handler =>
            {
                var typedHandler = (PageHandler<THandler, TPage, TUIEvent, TCommand>)handler;
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
    /// Handle a model event from the backend (state change notification).
    /// </summary>
    /// <param name="evt">The model event from the backend.</param>
    protected virtual void HandleModelEvent(IModelEvent evt)
    {
        // Default: ignore model events. Override to handle specific events.
    }

    /// <summary>
    /// Send a command to the page to update UI state.
    /// </summary>
    /// <param name="command">The command to send.</param>
    protected void Send(TCommand command) => Page.ReceiveCommand(command);

    /// <summary>
    /// Internal method called by the framework to handle frontend events.
    /// </summary>
    internal void InvokeHandleUIEvent(TUIEvent evt) => HandleUIEvent(evt);

    /// <summary>
    /// Internal method called by the framework to handle backend events.
    /// </summary>
    internal void InvokeHandleModelEvent(IModelEvent evt) => HandleModelEvent(evt);
}
