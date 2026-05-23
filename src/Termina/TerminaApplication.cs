// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using R3;
using Termina.Diagnostics;
using Termina.Hosting;
using Termina.Input;
using Termina.Layout;
using Termina.Navigation;
using Termina.Notifications;
using Termina.Pages;
using Termina.Platform;
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
    private readonly TerminaRuntimeOptions _runtimeOptions;
    private readonly IServiceProvider? _serviceProvider;
    private readonly Channel<object> _eventChannel;
    private readonly Subject<IInputEvent> _inputSubject = new();
    private readonly RouteMatcher _routeMatcher = new();
    private readonly Dictionary<string, ReactivePageRegistration> _pages = new();
    private readonly Dictionary<string, (IPage Page, ReactiveViewModel ViewModel)> _cachedPages = new();
    private readonly Stack<(string Path, IReadOnlyDictionary<string, object>? Parameters)> _history = new();
    private readonly List<IInputSource> _inputSources = new();
    private readonly FocusManager _focusManager = new();
    private readonly IToastService? _toastService;
    private readonly ToastOverlayNode? _toastOverlay;
    private readonly IDisposable? _toastInvalidationSubscription;

    private string? _currentPath;
    private IReadOnlyDictionary<string, object>? _currentParameters;
    private IPage? _currentPage;
    private ReactiveViewModel? _currentViewModel;
    private ReactivePageRegistration? _currentRegistration;
    private CancellationTokenSource? _shutdownCts;
    private DateTime? _firstCtrlCAt;
    private static readonly TimeSpan CtrlCDoublePressWindow = TimeSpan.FromSeconds(2);
    private bool _rawInputActive;
    private bool _kittyKeyboardPushed;
    private bool _wheelScrollEnabledByApp;

    /// <summary>
    /// Creates a new Termina application.
    /// </summary>
    /// <param name="terminal">The ANSI terminal for rendering.</param>
    public TerminaApplication(IAnsiTerminal terminal)
        : this(terminal, runtimeOptions: null, serviceProvider: null)
    {
    }

    /// <summary>
    /// Creates a new Termina application.
    /// </summary>
    /// <param name="terminal">The ANSI terminal for rendering.</param>
    /// <param name="serviceProvider">Optional service provider for resolving ViewModels and input sources.</param>
    public TerminaApplication(IAnsiTerminal terminal, IServiceProvider? serviceProvider)
        : this(terminal, runtimeOptions: null, serviceProvider: serviceProvider)
    {
    }

    /// <summary>
    /// Creates a new Termina application.
    /// </summary>
    /// <param name="terminal">The ANSI terminal for rendering.</param>
    /// <param name="runtimeOptions">Runtime terminal/input options.</param>
    /// <param name="serviceProvider">Optional service provider for resolving ViewModels and input sources.</param>
    public TerminaApplication(
        IAnsiTerminal terminal,
        TerminaRuntimeOptions? runtimeOptions = null,
        IServiceProvider? serviceProvider = null)
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(ex =>
        {
            TerminaTrace.Reactive.Error("ObservableSystem", "Unhandled observable error: {0}", ex);
        });

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

        _runtimeOptions = runtimeOptions ?? new TerminaRuntimeOptions();
        _serviceProvider = serviceProvider;
        _eventChannel = Channel.CreateUnbounded<object>();
        _toastService = serviceProvider?.GetService<IToastService>();
        _toastOverlay = _toastService != null ? new ToastOverlayNode(_toastService) : null;
        _toastInvalidationSubscription = _toastOverlay?.Invalidated.Subscribe(_ => RequestRedraw());

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
    public Observable<IInputEvent> Input => _inputSubject.AsObservable();

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
        TerminaTrace.Page.Info(this, "Navigating to: {0}", path);

        // Notify current page/ViewModel they're leaving
        if (_currentPage != null && _currentViewModel != null)
        {
            TerminaTrace.Page.Debug(this, "Deactivating current page: {0}", _currentPage.GetType().Name);
            _currentPage.OnNavigatingFrom();
            _currentViewModel.OnDeactivating();
        }

        // Push current page to history (if not going to same page)
        if (_currentPath != null && _currentPath != path)
        {
            _history.Push((_currentPath, _currentParameters));
            TerminaTrace.Page.Debug(this, "Pushed to history: {0}, depth={1}", _currentPath, _history.Count);
        }

        // Generate a cache key that includes parameters for PreserveState pages
        var cacheKey = registration.RouteTemplate.Template;

        // Get or create page instance
        if (registration.Behavior == NavigationBehavior.PreserveState &&
            _cachedPages.TryGetValue(cacheKey, out var cached))
        {
            TerminaTrace.Page.Debug(this, "Using cached page: {0}", cacheKey);
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
            TerminaTrace.Page.Debug(this, "Creating new page, behavior={0}", registration.Behavior);
            _currentPage = registration.PageFactory();
            _currentViewModel = registration.ViewModelFactory();

            // Inject route parameters before wiring up
            if (parameters != null && _currentViewModel is IRouteParameterReceiver receiver)
            {
                receiver.SetRouteParameters(parameters);
            }

            // Wire up ViewModel with navigation, shutdown, redraw, and input.
            // Navigation is routed through the event channel (RequestNavigation)
            // so it is processed on the render loop thread, not the caller's.
            _currentViewModel.WireUp(RequestNavigation, RequestNavigation, Shutdown, RequestRedraw, Input);

            // Bind page to ViewModel and wire up focus and navigation
            BindPageToViewModel(_currentPage, _currentViewModel);
            WireUpPageFocus(_currentPage);
            WireUpPageNavigation(_currentPage);

            // Cache if PreserveState
            if (registration.Behavior == NavigationBehavior.PreserveState)
            {
                _cachedPages[cacheKey] = (_currentPage, _currentViewModel);
                TerminaTrace.Page.Debug(this, "Cached page: {0}", cacheKey);
            }
        }

        _currentPath = path;
        _currentParameters = parameters;
        _currentRegistration = registration;

        // Notify new page/ViewModel they're active
        _currentPage.OnNavigatedTo();
        _currentViewModel.OnActivated();
        TerminaTrace.Page.Info(this, "Navigation complete: {0}, page={1}", path, _currentPage.GetType().Name);
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
    /// Wires up focus management to the page.
    /// </summary>
    private void WireUpPageFocus(IPage page)
    {
        if (page is IBindablePage bindablePage)
        {
            bindablePage.WireUpFocus(_focusManager);
        }
    }

    /// <summary>
    /// Wires up navigation capabilities to the page.
    /// </summary>
    private void WireUpPageNavigation(IPage page)
    {
        if (page is IBindablePage bindablePage)
        {
            // Route through the event channel so navigation is processed on the
            // render loop thread (see RequestNavigation).
            bindablePage.WireUpNavigation(RequestNavigation, RequestNavigation, Shutdown);
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

    // Navigation requested by a ViewModel or page runs on whatever thread the
    // caller is on (e.g. an async continuation on the thread pool). Posting a
    // NavigationRequested event to the channel defers NavigateToInternal to the
    // render loop thread, so page swaps are serialized against rendering. A
    // direct cross-thread NavigateTo would publish a not-yet-bound _currentPage
    // to the render thread — a data race that crashes under ARM64's weak memory
    // model (the render loop calls BuildLayout() before OnBound() has run).
    private void RequestNavigation(string path)
    {
        _eventChannel.Writer.TryWrite(new NavigationRequested(path));
    }

    private void RequestNavigation(string routeTemplate, object? routeValues)
    {
        _eventChannel.Writer.TryWrite(
            new NavigationRequested(RouteMatcher.BuildPath(routeTemplate, routeValues)));
    }

    /// <summary>
    /// Run the application until cancellation or shutdown is requested.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        TerminaTrace.Page.Info(this, "RunAsync starting");

        // Create platform console for native input handling
        IPlatformConsole? platformConsole = null;

        if (_inputSources.Count == 0)
        {
            // Use platform-specific console for event-driven input (no polling on Windows)
            platformConsole = PlatformConsoleFactory.Create(_runtimeOptions);
            platformConsole.Initialize();
            _rawInputActive = platformConsole.Capabilities.RawInputActive;

            var kittyFlags = GetKittyKeyboardFlags();
            var kittyReportAllKeysVisible = _rawInputActive && (kittyFlags & 8) != 0;

            _inputSources.Add(new PlatformInputSource(
                platformConsole,
                new PlatformInputConfiguration(_rawInputActive, kittyReportAllKeysVisible)));
            TerminaTrace.Input.Debug(this, "Added PlatformInputSource");
        }

        // Create linked token - cancelled by either external token OR Shutdown()
        TerminaTrace.Input.Debug(this, "Creating linked cancellation token");
        _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedToken = _shutdownCts.Token;
        TerminaTrace.Input.Debug(this, "Linked token created");

        // Start each input source - they push to the shared channel
        TerminaTrace.Input.Debug(this, "Starting {0} input source(s)...", _inputSources.Count);
        var inputTasks = _inputSources
            .Select(source => source.RunAsync(_eventChannel.Writer, linkedToken))
            .ToList();

        TerminaTrace.Input.Debug(this, "Input tasks started, count={0}", inputTasks.Count);

        try
        {
            // Enter alternate screen and hide cursor
            TerminaTrace.Render.Debug(this, "About to enter alternate screen");
            _terminal.EnterAlternateScreen();
            _terminal.SetCursorVisible(false);
            _terminal.Flush();
            TerminaTrace.Render.Debug(this, "Entered alternate screen, cursor hidden, flushed");

            var inTmux = Environment.GetEnvironmentVariable("TMUX") is not null;
            Console.Write(AnsiCodes.EnableBracketedPaste);

            // When running inside tmux, the inner-pane ESC[?2004h above is intercepted by tmux
            // and never reaches the outer terminal. The outer terminal therefore does not know
            // to wrap Ctrl+Shift+V pastes with ESC[200~...ESC[201~. Use a DCS passthrough to
            // also enable bracketed paste in the outer terminal.
            // Requires: set -g allow-passthrough on  in ~/.tmux.conf (tmux 3.3+).
            if (inTmux)
            {
                Console.Write(AnsiCodes.TmuxPassthrough(AnsiCodes.EnableBracketedPaste));
            }

            var kittyFlags = GetKittyKeyboardFlags();
            _kittyKeyboardPushed = KittyKeyboardEnhancement.TryEnter(this, kittyFlags, inTmux);

            if (_runtimeOptions.ScrollInputMode == ScrollInputMode.AlternateScroll && _rawInputActive)
            {
                // Alternate-scroll preserves native selection/clipboard behavior but only works
                // correctly when the input parser sees raw byte sequences.
                _terminal.EnableWheelScroll();
                _wheelScrollEnabledByApp = true;
            }
            else
            {
                _terminal.EnableMouse();
                _wheelScrollEnabledByApp = false;
            }

            _terminal.Flush();

            // Initial render
            TerminaTrace.Render.Debug(this, "Starting initial render");
            RenderCurrentPage();
            TerminaTrace.Render.Debug(this, "Initial render complete");

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
            var inTmux = Environment.GetEnvironmentVariable("TMUX") is not null;

            // Disable bracketed paste and kitty keyboard protocol before restoring terminal
            if (_kittyKeyboardPushed)
            {
                KittyKeyboardEnhancement.TryLeave(inTmux);
                _kittyKeyboardPushed = false;
            }

            Console.Write(AnsiCodes.DisableBracketedPaste);
            if (inTmux)
            {
                Console.Write(AnsiCodes.TmuxPassthrough(AnsiCodes.DisableBracketedPaste));
            }

            if (_wheelScrollEnabledByApp)
                _terminal.DisableWheelScroll();
            else
                _terminal.DisableMouse();

            _terminal.SetCursorVisible(true);
            _terminal.ResetColors();
            _terminal.ExitAlternateScreen();
            _terminal.Flush();

            // Clear any partial line artifacts
            Console.WriteLine();

            // Complete the input subject
            _inputSubject.OnCompleted();

            // Restore and dispose platform console
            platformConsole?.Dispose();
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
        TerminaTrace.Input.Trace(this, "ProcessEvent: {0}", evt.GetType().Name);

        // Framework-level Ctrl+C handling: first press shows a hint, second press
        // within CtrlCDoublePressWindow shuts the app down. We scope this to raw-input
        // mode by default so regular Console.ReadKey apps keep their existing behavior.
        if (ShouldInterceptCtrlC()
            && evt is KeyPressed ctrlC
            && ctrlC.KeyInfo.Key == ConsoleKey.C
            && ctrlC.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            var now = DateTime.UtcNow;
            if (_firstCtrlCAt.HasValue && (now - _firstCtrlCAt.Value) <= CtrlCDoublePressWindow)
            {
                TerminaTrace.Page.Info(this, "Ctrl+C pressed twice — shutting down");
                _firstCtrlCAt = null;
                Shutdown();
                return;
            }

            _firstCtrlCAt = now;
            _toastService?.Show(
                "Press Ctrl+C again to quit",
                new ToastOptions(Duration: CtrlCDoublePressWindow, Position: ToastPosition.BottomCenter));
            RequestRedraw();
            return;
        }

        if (evt is KeyPressed keyPressedEvent
            && !(keyPressedEvent.KeyInfo.Key == ConsoleKey.C
                 && keyPressedEvent.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)))
        {
            _firstCtrlCAt = null;
        }

        // Handle system events
        switch (evt)
        {
            case ShutdownRequested:
                TerminaTrace.Page.Info(this, "ShutdownRequested received");
                Shutdown();
                return;

            case NavigationRequested navReq:
                TerminaTrace.Page.Debug(this, "NavigationRequested: {0}", navReq.PageKey);
                NavigateTo(navReq.PageKey);
                return;

            case NavigationBackRequested:
                TerminaTrace.Page.Debug(this, "NavigationBackRequested, canGoBack={0}", CanGoBack);
                GoBack();
                return;

            case ResizeEvent resize:
                TerminaTrace.Render.Debug(this, "ResizeEvent: {0}x{1}", resize.Width, resize.Height);
                // Force full refresh on resize since terminal dimensions changed
                _diffingTerminal?.ForceFullRefresh();
                return;

            case PasteEvent pasteEvent:
                if (_focusManager.CurrentFocus is IPasteReceiver focused)
                {
                    if (focused.HandlePaste(pasteEvent))
                    {
                        ShowPasteToast(pasteEvent.Content);
                    }
                }
                else if (FindPasteReceiver(GetCurrentLayoutRoot()) is { } fallback)
                {
                    if (fallback.HandlePaste(pasteEvent))
                    {
                        ShowPasteToast(pasteEvent.Content);
                    }
                }
                else
                {
                    _inputSubject.OnNext(pasteEvent);
                }
                return;

            case MouseScrollEvent mouseScroll:
                const int linesPerTick = 3;
                if (_focusManager.CurrentFocus is IScrollable scrollable)
                {
                    if (mouseScroll.Delta > 0)
                        scrollable.ScrollUp(linesPerTick);
                    else
                        scrollable.ScrollDown(linesPerTick);
                    return; // Handled by focused scrollable
                }
                break; // No focused scrollable — fall through to ViewModel input observable
        }

        // Route input events: Page (capture) -> Focus Manager (bubble) -> ViewModel
        if (evt is IInputEvent inputEvent)
        {
            // For key presses, use capture-then-bubble pattern
            if (inputEvent is KeyPressed keyPressed)
            {
                TerminaTrace.Input.Trace(this, "KeyPressed: key={0}, mods={1}", keyPressed.KeyInfo.Key, keyPressed.KeyInfo.Modifiers);

                // CAPTURE PHASE: Page-level key bindings get first chance
                // This allows pages to intercept keys (like Escape) before focused components consume them
                if (_currentPage is IBindablePage bindablePage)
                {
                    if (bindablePage.HandlePageInput(keyPressed.KeyInfo))
                    {
                        TerminaTrace.Input.Trace(this, "Key consumed by page-level handler");
                        return; // Input was consumed by page
                    }
                }

                // BUBBLE PHASE: Focused components handle remaining keys
                if (_focusManager.RouteInput(keyPressed.KeyInfo))
                {
                    TerminaTrace.Input.Trace(this, "Key consumed by focused component");
                    return; // Input was consumed by focused component
                }
            }

            // If not consumed, route to ViewModel via observable
            TerminaTrace.Input.Trace(this, "Routing input to ViewModel");
            _inputSubject.OnNext(inputEvent);
        }
    }

    /// <summary>
    /// Recursively walks the layout tree to find the first <see cref="IPasteReceiver"/>.
    /// </summary>
    private static IPasteReceiver? FindPasteReceiver(ILayoutNode? node)
    {
        if (node is null) return null;
        if (node is IPasteReceiver receiver) return receiver;

        var children = node switch
        {
            LayoutNode layoutNode => layoutNode.GetChildNodes(),
            IContainerNode container => container.Children,
            _ => Enumerable.Empty<ILayoutNode>()
        };

        foreach (var child in children)
        {
            var found = FindPasteReceiver(child);
            if (found != null) return found;
        }
        return null;
    }

    private int GetKittyKeyboardFlags() => (int)_runtimeOptions.KittyKeyboardMode;

    private bool ShouldInterceptCtrlC() => _runtimeOptions.CtrlCHandlingMode switch
    {
        CtrlCHandlingMode.Disabled => false,
        CtrlCHandlingMode.DoublePressWhenRawInput => _rawInputActive,
        CtrlCHandlingMode.DoublePressAlways => true,
        _ => false,
    };

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
        if (_toastOverlay != null)
        {
            layoutRoot = new StackLayout([layoutRoot, new DeferredNode(() => _toastOverlay)]);
        }

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

    private void ShowPasteToast(string content)
    {
        if (_toastService is null)
            return;

        var lineCount = content.Count(c => c == '\n') + 1;
        var message = lineCount > 1
            ? $"Pasted {lineCount} lines"
            : $"Pasted {content.Length} characters";

        _toastService.Show(message);
    }

    /// <summary>
    /// Clears the page cache and disposes all cached pages.
    /// </summary>
    private void ClearPageCache()
    {
        foreach (var (page, viewModel) in _cachedPages.Values)
        {
            // Properly dispose cached pages
            if (page is IDisposable disposablePage)
                disposablePage.Dispose();
            viewModel.Dispose();
        }
        _cachedPages.Clear();
    }

    /// <summary>
    /// Disposes the application and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        // Dispose current page if it's disposable
        if (_currentPage is IDisposable currentDisposable)
            currentDisposable.Dispose();

        // Clear and dispose all cached pages
        ClearPageCache();

        // Complete and dispose the input subject
        _inputSubject.OnCompleted();
        _inputSubject.Dispose();

        // Dispose all input sources
        foreach (var source in _inputSources)
        {
            if (source is IDisposable disposable)
                disposable.Dispose();
        }

        _toastInvalidationSubscription?.Dispose();
        _toastOverlay?.Dispose();
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
