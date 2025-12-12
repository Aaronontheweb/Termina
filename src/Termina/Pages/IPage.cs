using Spectre.Console.Rendering;

namespace Termina.Pages;

/// <summary>
/// Represents a page (screen) in the TUI application.
/// Pages render UI and handle lifecycle events.
/// </summary>
public interface IPage
{
    /// <summary>
    /// Called when the page becomes active (navigated to).
    /// Use this to initialize or refresh component state.
    /// </summary>
    void OnNavigatedTo();

    /// <summary>
    /// Called when the page is about to become inactive (navigating away).
    /// Use this to save state or clean up.
    /// </summary>
    void OnNavigatingFrom();

    /// <summary>
    /// Render the page as a Spectre.Console renderable.
    /// </summary>
    IRenderable Render();
}
