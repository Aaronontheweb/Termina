using System.Threading.Channels;
using Termina.Pages;

namespace Termina.Navigation;

/// <summary>
/// Registration info for a page.
/// </summary>
internal sealed record PageRegistration(
    Func<IPage> PageFactory,
    Func<IPageHandler> HandlerFactory,
    NavigationBehavior Behavior);

/// <summary>
/// Manages navigation between pages, including history and page caching.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Dictionary<string, PageRegistration> _pages = new();
    private readonly Stack<string> _history = new();
    private readonly Dictionary<string, (IPage Page, IPageHandler Handler)> _cachedPages = new();
    private ChannelWriter<object>? _eventWriter;

    private string? _currentPageKey;
    private IPage? _currentPage;
    private IPageHandler? _currentHandler;

    public NavigationService()
    {
    }

    /// <summary>
    /// Set the event writer. Called by EventMediator during initialization.
    /// </summary>
    internal void SetEventWriter(ChannelWriter<object> eventWriter)
    {
        _eventWriter = eventWriter;
    }

    /// <inheritdoc />
    public IPage? CurrentPage => _currentPage;

    /// <inheritdoc />
    public IPageHandler? CurrentHandler => _currentHandler;

    /// <inheritdoc />
    public bool CanGoBack => _history.Count > 0;

    /// <summary>
    /// Register a page with the navigation service.
    /// </summary>
    /// <typeparam name="TPage">The page type.</typeparam>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <param name="key">Unique key to identify this page.</param>
    /// <param name="behavior">How the page behaves on navigation.</param>
    public void RegisterPage<TPage, THandler>(
        string key,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
        where TPage : IPage, new()
        where THandler : IPageHandler, new()
    {
        _pages[key] = new PageRegistration(
            () => new TPage(),
            () => new THandler(),
            behavior);
    }

    /// <summary>
    /// Register a page with custom factories.
    /// </summary>
    public void RegisterPage(
        string key,
        Func<IPage> pageFactory,
        Func<IPageHandler> handlerFactory,
        NavigationBehavior behavior = NavigationBehavior.ResetOnNavigation)
    {
        _pages[key] = new PageRegistration(pageFactory, handlerFactory, behavior);
    }

    /// <inheritdoc />
    public void NavigateTo(string pageKey)
    {
        if (!_pages.TryGetValue(pageKey, out var registration))
            throw new InvalidOperationException($"Page '{pageKey}' is not registered.");

        // Notify current page it's leaving
        _currentPage?.OnNavigatingFrom();

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

            // Wire up event writer to handler
            if (_eventWriter != null)
            {
                _currentHandler.SetEventWriter(_eventWriter);

                // Wire up event writer to all components
                foreach (var component in _currentPage.Components)
                {
                    component.EventWriter = _eventWriter;
                }
            }

            // Cache if PreserveState
            if (registration.Behavior == NavigationBehavior.PreserveState)
            {
                _cachedPages[pageKey] = (_currentPage, _currentHandler);
            }
        }

        _currentPageKey = pageKey;

        // Notify new page it's active
        _currentPage.OnNavigatedTo();
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (_history.Count > 0)
        {
            var previousKey = _history.Pop();

            // Don't push current to history when going back
            var currentKey = _currentPageKey;
            _currentPageKey = null; // Prevent pushing to history

            NavigateTo(previousKey);

            // History already has correct state, don't need to adjust
        }
    }

    /// <summary>
    /// Get all registered page keys.
    /// </summary>
    public IEnumerable<string> RegisteredPages => _pages.Keys;
}
