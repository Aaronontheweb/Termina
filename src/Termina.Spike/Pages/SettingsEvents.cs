using Termina.Events;

namespace Termina.Spike.Pages;

/// <summary>
/// UI events for the settings page.
/// </summary>
public abstract record SettingsUIEvent : IPageUIEvent
{
    /// <summary>
    /// User wants to go back to the previous page.
    /// </summary>
    public sealed record BackRequested() : SettingsUIEvent;

    /// <summary>
    /// User submitted their username (pressed Enter in username field).
    /// </summary>
    public sealed record UsernameSubmitted(string Username) : SettingsUIEvent;

    /// <summary>
    /// User selected a theme (pressed Enter on theme selector).
    /// </summary>
    public sealed record ThemeSelected(string Theme) : SettingsUIEvent;
}

/// <summary>
/// Commands sent from handler to settings page.
/// </summary>
public abstract record SettingsCommand : IUICommand
{
    /// <summary>
    /// Initialize the theme selector with available themes.
    /// </summary>
    public sealed record InitializeThemes(IReadOnlyList<string> Themes) : SettingsCommand;

    /// <summary>
    /// Set the username field value (e.g., when loaded from storage).
    /// </summary>
    public sealed record SetUsername(string Username) : SettingsCommand;

    /// <summary>
    /// Select a theme in the theme selector (e.g., when loaded from storage).
    /// </summary>
    public sealed record SelectTheme(string Theme) : SettingsCommand;

    /// <summary>
    /// Show a status message to the user.
    /// </summary>
    public sealed record ShowStatus(string Message, bool IsError = false) : SettingsCommand;

    /// <summary>
    /// Show a loading indicator.
    /// </summary>
    public sealed record ShowLoading(string Message) : SettingsCommand;

    /// <summary>
    /// Hide the loading indicator.
    /// </summary>
    public sealed record HideLoading() : SettingsCommand;
}
