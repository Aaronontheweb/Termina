using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Handler for the main menu page.
/// Stateless event transformer - handles menu selections and triggers navigation.
/// </summary>
public sealed class MainMenuHandler
    : PageHandler<MainMenuPage, MainMenuUIEvent, MainMenuCommand>
{
    protected override void OnNavigatedTo()
    {
        // Initialize the menu with options
        Send(new MainMenuCommand.InitializeMenu(["Settings", "About", "Exit"]));
    }

    protected override void HandleUIEvent(MainMenuUIEvent evt)
    {
        switch (evt)
        {
            case MainMenuUIEvent.MenuItemSelected(var value):
                switch (value)
                {
                    case "Settings":
                        Navigate("settings");
                        break;
                    case "About":
                        Navigate("about");
                        break;
                    case "Exit":
                        Shutdown();
                        break;
                }
                break;
        }
    }
}
