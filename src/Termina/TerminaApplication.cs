using System.Threading.Channels;
using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Events;
using Termina.Input;
using Termina.Navigation;
using Termina.Pages;

namespace Termina;

/// <summary>
/// The main application orchestrator for Termina TUI applications.
/// Implements the two-tier duplex event loop architecture.
/// </summary>
/// <remarks>
/// <para>
/// TerminaApplication is the "app host" that:
/// </para>
/// <list type="bullet">
///   <item>Owns the event bus / channel infrastructure</item>
///   <item>Routes model events to appropriate handler(s)</item>
///   <item>Routes UI events to the current page's handler</item>
///   <item>Manages page registration and navigation</item>
///   <item>Dead letter logging for unhandled events</item>
///   <item>Controls which page is "active" and can render</item>
/// </list>
/// </remarks>
public sealed class TerminaApplication
{
    private readonly IAnsiConsole _console;
    private readonly Channel<object> _eventChannel;
    private readonly ApplicationBus _bus;
    private readonly Dictionary<string, PageRegistration> _pages = new();
    private readonly Dictionary<string, (IPage Page, object Handler)> _cachedPages = new();
    private readonly Stack<string> _history = new();
    private readonly List<IInputSource> _inputSources = new();

    private string? _currentPageKey;
    private IPage? _currentPage;
    private object? _currentHandler;
    private PageRegistration? _currentRegistration;
    private CancellationTokenSource? _shutdownCts;

    /// <summary>
    /// Creates a new Termina application.
    /// </summary>
    /// <param name="console">The Spectre.Console instance for rendering.</param>
    public TerminaApplication(IAnsiConsole console)
    {
        _console = console;
        _eventChannel = Channel.CreateUnbounded<object>();
        _bus = new ApplicationBus(_eventChannel);
    }

    /// <summary>
    /// Gets the application bus for Model layer integration.
    /// Actors and services can publish events and subscribe to events through this bus.
    /// </summary>
    public IApplicationBus Bus => _bus;

    /// <summary>
    /// Gets the current page key, if any.
    /// </summary>
    public string? CurrentPageKey => _currentPageKey;

    /// <summary>
    /// Whether navigation history allows going back.
    /// </summary>
    public bool CanGoBack => _history.Count > 0;

    /// <summary>
    /// Add an input source to the application.
    /// </summary>
    /// <param name="inputSource">The input source to add.</param>
    /// <returns>This application for fluent chaining.</returns>
    public TerminaApplication AddInputSource(IInputSource inputSource)
    {
        _inputSources.Add(inputSource);
        return this;
    }

    /// <summary>
    /// Register a page with the application using the simplified API.
    /// Types are inferred from the handler's base class via static abstract members.
    /// </summary>
    /// <typeparam name="THandler">The handler type (must implement IPageHandler).</typeparam>
    /// <param name="pageKey">Unique key to identify this page.</param>
    /// <param name="behavior">How the page behaves on navigation.</param>
    /// <example>
    /// <code>
    /// app.RegisterPage&lt;MainMenuHandler&gt;("main-menu");
    /// app.RegisterPage&lt;SettingsHandler&gt;("settings", NavigationBehavior.PreserveState);
    /// </code>
    /// </example>
    public void RegisterPage<THandler>(
        string pageKey,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where THandler : IPageHandler, new()
    {
        var registration = THandler.CreateRegistration(pageKey, behavior);
        _pages[pageKey] = registration with { HandlerFactory = () => new THandler() };
    }

    /// <summary>
    /// Register a page with a custom handler factory for dependency injection scenarios.
    /// </summary>
    /// <typeparam name="THandler">The handler type (must implement IPageHandler).</typeparam>
    /// <param name="pageKey">Unique key to identify this page.</param>
    /// <param name="handlerFactory">Factory function that creates the handler instance.</param>
    /// <param name="behavior">How the page behaves on navigation.</param>
    /// <example>
    /// <code>
    /// // With dependency injection
    /// app.RegisterPage&lt;TaskListHandler&gt;("tasks", () =>
    /// {
    ///     var handler = new TaskListHandler();
    ///     handler.SetTaskManager(taskManagerActor);
    ///     return handler;
    /// });
    /// </code>
    /// </example>
    public void RegisterPage<THandler>(
        string pageKey,
        Func<THandler> handlerFactory,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where THandler : IPageHandler
    {
        var registration = THandler.CreateRegistration(pageKey, behavior);
        _pages[pageKey] = registration with { HandlerFactory = () => handlerFactory() };
    }

    /// <summary>
    /// Navigate to a page by key.
    /// </summary>
    /// <param name="pageKey">The key of the page to navigate to.</param>
    public void NavigateTo(string pageKey)
    {
        if (!_pages.TryGetValue(pageKey, out var registration))
            throw new InvalidOperationException($"Page '{pageKey}' is not registered.");

        // Notify current page/handler they're leaving
        if (_currentPage != null && _currentHandler != null && _currentRegistration != null)
        {
            _currentPage.OnNavigatingFrom();
            _currentRegistration.InvokeOnNavigatingFrom(_currentHandler);
        }

        // Push current page to history (if not going to same page)
        if (_currentPageKey != null && _currentPageKey != pageKey)
        {
            _history.Push(_currentPageKey);
        }

        // Get or create page instance
        if (registration.Behavior == NavigationBehavior.PreserveState &&
            _cachedPages.TryGetValue(pageKey, out var cached))
        {
            _currentPage = cached.Page;
            _currentHandler = cached.Handler;
        }
        else
        {
            _currentPage = registration.PageFactory();
            _currentHandler = registration.HandlerFactory();

            // Wire up handler
            registration.WireUpHandler(
                _currentHandler,
                _currentPage,
                _bus,
                NavigateTo,
                Shutdown);

            // Cache if PreserveState
            if (registration.Behavior == NavigationBehavior.PreserveState)
            {
                _cachedPages[pageKey] = (_currentPage, _currentHandler);
            }
        }

        _currentPageKey = pageKey;
        _currentRegistration = registration;

        // Notify new page/handler they're active
        _currentPage.OnNavigatedTo();
        registration.InvokeOnNavigatedTo(_currentHandler);
    }

    /// <summary>
    /// Go back to the previous page in history.
    /// </summary>
    public void GoBack()
    {
        if (_history.Count > 0)
        {
            var previousKey = _history.Pop();
            var currentKey = _currentPageKey;
            _currentPageKey = null; // Prevent pushing to history
            NavigateTo(previousKey);
        }
    }

    /// <summary>
    /// Request graceful shutdown of the application.
    /// </summary>
    public void Shutdown()
    {
        _shutdownCts?.Cancel();
    }

    /// <summary>
    /// Run the application until cancellation or shutdown is requested.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_inputSources.Count == 0)
        {
            // Add default console input source if none configured
            _inputSources.Add(new ConsoleInputSource());
        }

        // Create linked token - cancelled by either external token OR Shutdown()
        _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedToken = _shutdownCts.Token;

        // Start each input source - they push to the shared channel
        var inputTasks = _inputSources
            .Select(source => source.RunAsync(_eventChannel.Writer, linkedToken))
            .ToList();

        try
        {
            // Initial render
            var rootRenderable = RenderCurrentPage();

            await _console.Live(rootRenderable)
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    // Initial render - don't wait for first event
                    ctx.Refresh();

                    // Single-threaded event loop - all input sources merge here
                    await foreach (var evt in _eventChannel.Reader.ReadAllAsync(linkedToken))
                    {
                        ProcessEvent(evt);

                        // Re-render after event processing
                        ctx.UpdateTarget(RenderCurrentPage());
                        ctx.Refresh();
                    }
                });
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        // Wait for all input sources to complete
        try
        {
            await Task.WhenAll(inputTasks);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            _shutdownCts.Dispose();
            _shutdownCts = null;
        }
    }

    /// <summary>
    /// Process an event by routing it to the appropriate handlers.
    /// </summary>
    private void ProcessEvent(object evt)
    {
        // Handle system events
        switch (evt)
        {
            case ShutdownRequested:
                Shutdown();
                return;

            case NavigationRequested navReq:
                NavigateTo(navReq.PageKey);
                return;

            case NavigationBackRequested:
                GoBack();
                return;
        }

        // Check if we have an active page
        if (_currentPage == null || _currentHandler == null || _currentRegistration == null)
        {
            _bus.RaiseDeadLetter(evt, "No active page");
            return;
        }

        // Check if this is a model event
        if (evt is IModelEvent modelEvent)
        {
            // Route to handler - it will pattern match on the event type
            _currentRegistration.InvokeHandleModelEvent(_currentHandler, modelEvent);
            return;
        }

        // Check if this is a raw input event
        if (evt is IInputEvent inputEvent)
        {
            // Transform to typed UI event
            var uiEvent = _currentRegistration.TransformInput(_currentPage, inputEvent);

            if (uiEvent != null)
            {
                // Route to handler
                _currentRegistration.InvokeHandleUIEvent(_currentHandler, uiEvent);
            }
            // else: page swallowed the event (returned null from MapToUIEvent)
            return;
        }

        // Unknown event type
        _bus.RaiseDeadLetter(evt, $"Unknown event type: {evt.GetType().Name}");
    }

    /// <summary>
    /// Renders the current page.
    /// </summary>
    private IRenderable RenderCurrentPage()
    {
        return _currentPage?.Render()
            ?? new Text("No page active");
    }
}
