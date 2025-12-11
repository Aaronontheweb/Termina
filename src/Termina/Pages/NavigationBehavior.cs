namespace Termina.Pages;

/// <summary>
/// Defines how a page behaves when navigating away and back.
/// </summary>
public enum NavigationBehavior
{
    /// <summary>
    /// Page is re-created each time it is navigated to.
    /// State is not preserved between visits.
    /// </summary>
    ResetOnNavigation,

    /// <summary>
    /// Page instance is cached and reused.
    /// State is preserved between visits (e.g., form data).
    /// </summary>
    PreserveState
}
