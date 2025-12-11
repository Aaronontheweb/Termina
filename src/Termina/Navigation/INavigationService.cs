using Termina.Pages;

namespace Termina.Navigation;

/// <summary>
/// Service for navigating between pages in the TUI application.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Get the currently active page, or null if no page is active.
    /// </summary>
    IPage? CurrentPage { get; }

    /// <summary>
    /// Get the currently active page handler, or null if no page is active.
    /// </summary>
    IPageHandler? CurrentHandler { get; }

    /// <summary>
    /// Navigate to a page by its registered key.
    /// </summary>
    /// <param name="pageKey">The key the page was registered with.</param>
    void NavigateTo(string pageKey);

    /// <summary>
    /// Go back to the previous page in the navigation history.
    /// Does nothing if there is no previous page.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Check if there is a previous page to go back to.
    /// </summary>
    bool CanGoBack { get; }
}
