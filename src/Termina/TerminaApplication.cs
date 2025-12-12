using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Hosting;
using Termina.Input;
using Termina.Navigation;
using Termina.Pages;
using Termina.Reactive;

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
    private readonly IAnsiConsole _console;
    private readonly IServiceProvider? _serviceProvider;
    private readonly Channel<object> _eventChannel;
    private readonly Subject<IInputEvent> _inputSubject = new();
    private readonly Dictionary<string, ReactivePageRegistration> _pages = new();
    private readonly Dictionary<string, (IPage Page, ReactiveViewModel ViewModel)> _cachedPages = new();
    private readonly Stack<string> _history = new();
    private readonly List<IInputSource> _inputSources = new();

    private string? _currentPageKey;
    private IPage? _currentPage;
    private ReactiveViewModel? _currentViewModel;
    private ReactivePageRegistration? _currentRegistration;
    private CancellationTokenSource? _shutdownCts;

    /// <summary>
    /// Creates a new Termina application.
    /// </summary>
    /// <param name="console">The Spectre.Console instance for rendering.</param>
    /// <param name="serviceProvider">Optional service provider for resolving ViewModels and input sources.</param>
    public TerminaApplication(IAnsiConsole console, IServiceProvider? serviceProvider = null)
    {
        _console = console;
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
    /// Register a reactive page with its ViewModel.
    /// </summary>
    /// <typeparam name="TPage">The page type (must implement ReactivePage&lt;TViewModel&gt;).</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="pageKey">Unique key to identify this page.</param>
    /// <param name="behavior">How the page behaves on navigation.</param>
    public void RegisterPage<TPage, TViewModel>(
        string pageKey,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : ReactivePage<TViewModel>, new()
        where TViewModel : ReactiveViewModel, new()
    {
        _pages[pageKey] = new ReactivePageRegistration(
            pageKey,
            behavior,
            () => new TPage(),
            () => new TViewModel());
    }

    /// <summary>
    /// Register a page from a descriptor (used by TerminaBuilder).
    /// </summary>
    internal void RegisterPageFromDescriptor(ReactivePageRegistrationDescriptor descriptor)
    {
        _pages[descriptor.PageKey] = new ReactivePageRegistration(
            descriptor.PageKey,
            descriptor.Behavior,
            () => (IPage)descriptor.PageFactory(_serviceProvider!),
            () => descriptor.ViewModelFactory(_serviceProvider!));
    }

    /// <summary>
    /// Navigate to a page by key.
    /// </summary>
    /// <param name="pageKey">The key of the page to navigate to.</param>
    public void NavigateTo(string pageKey)
    {
        if (!_pages.TryGetValue(pageKey, out var registration))
            throw new InvalidOperationException($"Page '{pageKey}' is not registered.");

        // Notify current page/ViewModel they're leaving
        if (_currentPage != null && _currentViewModel != null)
        {
            _currentPage.OnNavigatingFrom();
            _currentViewModel.OnDeactivating();
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
            _currentViewModel = cached.ViewModel;
        }
        else
        {
            _currentPage = registration.PageFactory();
            _currentViewModel = registration.ViewModelFactory();

            // Wire up ViewModel with navigation and shutdown actions
            _currentViewModel.WireUp(NavigateTo, Shutdown, Input);

            // Bind page to ViewModel
            BindPageToViewModel(_currentPage, _currentViewModel);

            // Cache if PreserveState
            if (registration.Behavior == NavigationBehavior.PreserveState)
            {
                _cachedPages[pageKey] = (_currentPage, _currentViewModel);
            }
        }

        _currentPageKey = pageKey;
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
            var previousKey = _history.Pop();
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
        finally
        {
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
        }

        // Route input events to the observable - ViewModels subscribe to this
        if (evt is IInputEvent inputEvent)
        {
            _inputSubject.OnNext(inputEvent);
        }
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

/// <summary>
/// Registration information for a reactive page.
/// </summary>
internal sealed class ReactivePageRegistration
{
    public string PageKey { get; }
    public NavigationBehavior Behavior { get; }
    public Func<IPage> PageFactory { get; }
    public Func<ReactiveViewModel> ViewModelFactory { get; }

    public ReactivePageRegistration(
        string pageKey,
        NavigationBehavior behavior,
        Func<IPage> pageFactory,
        Func<ReactiveViewModel> viewModelFactory)
    {
        PageKey = pageKey;
        Behavior = behavior;
        PageFactory = pageFactory;
        ViewModelFactory = viewModelFactory;
    }
}
