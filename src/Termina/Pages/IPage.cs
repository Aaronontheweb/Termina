using Spectre.Console.Rendering;
using Termina.Reactive;

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

/// <summary>
/// Internal interface for reactive pages that can be bound to ViewModels.
/// This allows AOT-compatible binding without reflection.
/// </summary>
internal interface IBindablePage : IPage
{
    /// <summary>
    /// Binds a ViewModel to this page.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind.</param>
    void BindViewModel(ReactiveViewModel viewModel);
}
