using Termina.Events;

namespace Termina.Spike.Pages;

/// <summary>
/// UI events for the main menu page.
/// Transformed from raw input by the page.
/// </summary>
public abstract record MainMenuUIEvent : IPageUIEvent
{
    /// <summary>
    /// User selected a menu option.
    /// </summary>
    public sealed record MenuItemSelected(string Value) : MainMenuUIEvent;
}

/// <summary>
/// Commands sent from handler to main menu page.
/// </summary>
public abstract record MainMenuCommand : IUICommand
{
    /// <summary>
    /// Initialize the menu with options.
    /// </summary>
    public sealed record InitializeMenu(IReadOnlyList<string> Options) : MainMenuCommand;
}
