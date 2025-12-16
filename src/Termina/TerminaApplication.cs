// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Termina.Hosting;
using Termina.Input;
using Termina.Layout;
using Termina.Navigation;
using Termina.Pages;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Routing;
using Termina.Terminal;

namespace Termina;

/// <summary>
/// The main application orchestrator for Termina TUI applications.
/// </summary>
/// <remarks>
/// <para>
/// TerminaApplication is the "app host" that:
/// </para>
/// <list type="bullet">
///   <item>Owns the event channel infrastructure for input routing</item>
///   <item>Exposes input as IObservable&lt;IInputEvent&gt; for reactive ViewModels</item>
///   <item>Manages page registration and navigation</item>
///   <item>Controls which page is "active" and can render</item>
/// </list>
/// <para>
/// ViewModels are resolved from DI and can inject any services they need.
/// ViewModels subscribe to Input observable to handle keyboard events.
/// </para>
/// </remarks>
public sealed class TerminaApplication
{
    private readonly IAnsiTerminal _terminal;
    private readonly DiffingTerminal? _diffingTerminal;
    private readonly IServiceProvider? _serviceProvider;
    private readonly Channel<object> _eventChannel;
    private readonly Subject<IInputEvent> _inputSubject = new();
    private readonly RouteMatcher _routeMatcher = new();
    private readonly Dictionary<string, ReactivePageRegistration> _pages = new();
    private readonly Dictionary<string, (IPage Page, ReactiveViewModel ViewModel)> _cachedPages = new();
    private readonly Stack<(string Path, IReadOnlyDictionary<string, object>? Parameters)> _history = new();
    private readonly List<IInputSource> _inputSources = new();
    private readonly FocusManager _focusManager = new();

    private string? _currentPath;
    private IReadOnlyDictionary<string, object>? _currentParameters;
    private IPage? _currentPage;
    private ReactiveViewModel? _currentViewModel;
    private ReactivePageRegistration? _currentRegistration;
    private CancellationTokenSource? _shutdownCts;

    /// <summary>
    /// Creates a new Termina application.
    /// </summary>
    /// <param name="terminal">The ANSI terminal for rendering.</param>
    /// <param name="serviceProvider">Optional service provider for resolving ViewModels and input sources.</param>
    public TerminaApplication(IAnsiTerminal terminal, IServiceProvider? serviceProvider = null)
    {
        // Wrap terminal with DiffingTerminal for flicker-free rendering
        // unless it's already a DiffingTerminal or VirtualTerminal (for tests)
        if (terminal is DiffingTerminal diffing)
        {
            _terminal = terminal;
            _diffingTerminal = diffing;
        }
        else if (terminal is VirtualTerminal)
        {
            // Don't wrap VirtualTerminal - it's used for testing
            _terminal = terminal;
            _diffingTerminal = null;
        }
        else
        {
            _diffingTerminal = new DiffingTerminal(terminal);
            _terminal = _diffingTerminal;
        }

        _serviceProvider = serviceProvider;
        _eventChannel = Channel.CreateUnbounded<object>();

        // If using DI, check for registered input sources
        if (serviceProvider != null)
        {
            var inputSources = serviceProvider.GetServices<IInputSource>();
            foreach (var source in inputSources)
            {
                _inputSources.Add(source);
            }
        }
    }

    /// <summary>
    /// Observable stream of input events. ViewModels subscribe to this.
    /// </summary>
    public IObservable<IInputEvent> Input => _inputSubject.AsObservable();

    /// <summary>
    /// Gets the focus manager for routing input to focused components.
    /// </summary>
    public IFocusManager Focus => _focusManager;

    /// <summary>
    /// Gets the current navigation path, if any.
    /// </summary>
    public string? CurrentPath => _currentPath;

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
    /// Register a reactive page with a route template.
    /// </summary>
    /// <typeparam name="TPage">The page type (must implement ReactivePage&lt;TViewModel&gt;).</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="routeTemplate">Route template (e.g., "/tasks/{id:int}").</param>
    /// <param name="behavior">How the page behaves on navigation.</param>
    public void RegisterRoute<TPage, TViewModel>(
        string routeTemplate,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : ReactivePage<TViewModel>, new()
        where TViewModel : ReactiveViewModel, new()
    {
        var template = RouteParser.Parse(routeTemplate);
        var registration = new ReactivePageRegistration(
            template,
            behavior,
            () => new TPage(),
            () => new TViewModel());

        _pages[template.Template] = registration;
        _routeMatcher.AddRoute(template, template.Template);
    }

    /// <summary>
    /// Register a page from a descriptor (used by TerminaBuilder).
    /// </summary>
    internal void RegisterPageFromDescriptor(ReactivePageRegistrationDescriptor descriptor)
    {
        var registration = new ReactivePageRegistration(
            descriptor.RouteTemplate,
            descriptor.Behavior,
            () => (IPage)descriptor.PageFactory(_serviceProvider!),
            () => descriptor.ViewModelFactory(_serviceProvider!));

        _pages[descriptor.PageKey] = registration;
        _routeMatcher.AddRoute(descriptor.RouteTemplate, descriptor.PageKey);
    }

    /// <summary>
    /// Navigate to a path (e.g., "/tasks/42").
    /// </summary>
    /// <param name="path">The path to navigate to.</param>
    public void NavigateTo(string path)
    {
        // Try to match the path against registered routes
        if (!_routeMatcher.TryMatch(path, out var pageKey, out var parameters))
            throw new InvalidOperationException($"No route matches path '{path}'.");

        if (!_pages.TryGetValue(pageKey!, out var registration))
            throw new InvalidOperationException($"Page '{pageKey}' is not registered.");

        NavigateToInternal(path, registration, parameters);
    }

    /// <summary>
    /// Navigate to a path with route values.
    /// </summary>
    /// <param name="routeTemplate">The route template (e.g., "/tasks/{id}").</param>
    /// <param name="routeValues">The route values to substitute.</param>
    public void NavigateTo(string routeTemplate, object? routeValues)
    {
        var path = RouteMatcher.BuildPath(routeTemplate, routeValues);
        NavigateTo(path);
    }

    private void NavigateToInternal(
        string path,
        ReactivePageRegistration registration,
        IReadOnlyDictionary<string, object>? parameters)
    {
        // Notify current page/ViewModel they're leaving
        if (_currentPage != null && _currentViewModel != null)
        {
            _currentPage.OnNavigatingFrom();
            _currentViewModel.OnDeactivating();
        }

        // Push current page to history (if not going to same page)
        if (_currentPath != null && _currentPath != path)
        {
            _history.Push((_currentPath, _currentParameters));
        }

        // Generate a cache key that includes parameters for PreserveState pages
        var cacheKey = registration.RouteTemplate.Template;

        // Get or create page instance
        if (registration.Behavior == NavigationBehavior.PreserveState &&
            _cachedPages.TryGetValue(cacheKey, out var cached))
        {
            _currentPage = cached.Page;
            _currentViewModel = cached.ViewModel;

            // Still need to inject new parameters if the route has them
            if (parameters != null && _currentViewModel is IRouteParameterReceiver receiver)
            {
                receiver.SetRouteParameters(parameters);
            }
        }
        else
        {
            _currentPage = registration.PageFactory();
            _currentViewModel = registration.ViewModelFactory();

            // Inject route parameters before wiring up
            if (parameters != null && _currentViewModel is IRouteParameterReceiver receiver)
            {
                receiver.SetRouteParameters(parameters);
            }

            // Wire up ViewModel with navigation, shutdown, redraw, and focus manager
            _currentViewModel.WireUp(NavigateTo, (t, v) => NavigateTo(t, v), Shutdown, RequestRedraw, Input, _focusManager);

            // Bind page to ViewModel
            BindPageToViewModel(_currentPage, _currentViewModel);

            // Cache if PreserveState
            if (registration.Behavior == NavigationBehavior.PreserveState)
            {
                _cachedPages[cacheKey] = (_currentPage, _currentViewModel);
            }
        }

        _currentPath = path;
        _currentParameters = parameters;
        _currentRegistration = registration;

        // Notify new page/ViewModel they're active
        _currentPage.OnNavigatedTo();
        _currentViewModel.OnActivated();
    }

    /// <summary>
    /// Binds a page to its ViewModel using the IBindablePage interface (AOT-compatible).
    /// </summary>
    private static void BindPageToViewModel(IPage page, ReactiveViewModel viewModel)
    {
        if (page is IBindablePage bindablePage)
        {
            bindablePage.BindViewModel(viewModel);
        }
    }

    /// <summary>
    /// Go back to the previous page in history.
    /// </summary>
    public void GoBack()
    {
        if (_history.Count > 0)
        {
            var (previousPath, _) = _history.Pop();
            _currentPath = null; // Prevent pushing to history
            NavigateTo(previousPath);
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
    /// Request a UI redraw. Used by ViewModels when async content changes.
    /// </summary>
    public void RequestRedraw()
    {
        // Push a redraw event to the event channel - this will trigger re-render
        _eventChannel.Writer.TryWrite(RedrawRequested.Instance);
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
            // Enter alternate screen and hide cursor
            _terminal.EnterAlternateScreen();
            _terminal.SetCursorVisible(false);

            // Initial render
            RenderCurrentPage();

            // Single-threaded event loop - all input sources merge here
            await foreach (var evt in _eventChannel.Reader.ReadAllAsync(linkedToken))
            {
                ProcessEvent(evt);

                // Re-render after event processing
                RenderCurrentPage();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            // Restore terminal
            _terminal.SetCursorVisible(true);
            _terminal.ExitAlternateScreen();

            // Complete the input subject
            _inputSubject.OnCompleted();
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

            case ResizeEvent:
                // Force full refresh on resize since terminal dimensions changed
                _diffingTerminal?.ForceFullRefresh();
                return;
        }

        // Route input events through the focus manager first, then to ViewModel
        if (evt is IInputEvent inputEvent)
        {
            // For key presses, give focused components first chance to handle
            if (inputEvent is KeyPressed keyPressed)
            {
                if (_focusManager.RouteInput(keyPressed.KeyInfo))
                    return; // Input was consumed by focused component
            }

            // If not consumed, route to ViewModel via observable
            _inputSubject.OnNext(inputEvent);
        }
    }

    /// <summary>
    /// Renders the current page directly to the terminal.
    /// </summary>
    /// <remarks>
    /// When using DiffingTerminal, ClearScreen() only clears the pending buffer,
    /// not the actual screen. On Flush(), only changed cells are output.
    /// </remarks>
    private void RenderCurrentPage()
    {
        var layoutRoot = GetCurrentLayoutRoot() ?? new TextNode("No page active");

        // Clear the pending buffer (DiffingTerminal) or screen (other terminals)
        _terminal.ClearScreen();

        // Measure and render the layout
        var available = new Size(_terminal.Width, _terminal.Height);
        var measured = layoutRoot.Measure(available);

        // Create a full-screen render context
        var context = new RegionRenderContext(_terminal, 0, 0, _terminal.Width, _terminal.Height);
        var bounds = new Rect(0, 0, _terminal.Width, _terminal.Height);

        layoutRoot.Render(context, bounds);

        // Flush output
        _terminal.Flush();
    }

    /// <summary>
    /// Gets the layout root from the current page.
    /// </summary>
    private ILayoutNode? GetCurrentLayoutRoot()
    {
        if (_currentPage == null)
            return null;

        // If it's a ReactivePage, get the cached layout root
        if (_currentPage is IBindablePage bindable)
        {
            var layoutRoot = bindable.LayoutRoot;
            if (layoutRoot != null)
                return layoutRoot;
        }

        // Fall back to building the layout fresh
        return _currentPage.BuildLayout();
    }
}

/// <summary>
/// Registration information for a reactive page.
/// </summary>
internal sealed class ReactivePageRegistration
{
    public RouteTemplate RouteTemplate { get; }
    public NavigationBehavior Behavior { get; }
    public Func<IPage> PageFactory { get; }
    public Func<ReactiveViewModel> ViewModelFactory { get; }

    public ReactivePageRegistration(
        RouteTemplate routeTemplate,
        NavigationBehavior behavior,
        Func<IPage> pageFactory,
        Func<ReactiveViewModel> viewModelFactory)
    {
        RouteTemplate = routeTemplate;
        Behavior = behavior;
        PageFactory = pageFactory;
        ViewModelFactory = viewModelFactory;
    }
}
