using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Handler for the settings page.
/// Stateless event transformer - handles form interactions and back navigation.
/// </summary>
public sealed class SettingsHandler
    : PageHandler<SettingsHandler, SettingsPage, SettingsUIEvent, SettingsCommand>
{
    protected override void OnNavigatedTo()
    {
        // Initialize theme options (only needed first time, but idempotent)
        Send(new SettingsCommand.InitializeThemes(["Light", "Dark", "System Default"]));
    }

    protected override void HandleUIEvent(SettingsUIEvent evt)
    {
        switch (evt)
        {
            case SettingsUIEvent.BackRequested:
                Navigate("main-menu");
                break;

            case SettingsUIEvent.UsernameSubmitted(var username):
                // In a real app, we'd publish an event to save this
                // Publish(new SaveUsernameSetting(username));
                break;

            case SettingsUIEvent.ThemeSelected(var theme):
                // In a real app, we'd publish an event to apply the theme
                // Publish(new ApplyThemeSetting(theme));
                break;
        }
    }
}
