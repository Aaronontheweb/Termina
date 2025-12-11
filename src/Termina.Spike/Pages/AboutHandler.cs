using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Handler for the about page.
/// Stateless event transformer - just handles back navigation.
/// </summary>
public sealed class AboutHandler
    : PageHandler<AboutHandler, AboutPage, AboutUIEvent, AboutCommand>
{
    protected override void HandleUIEvent(AboutUIEvent evt)
    {
        switch (evt)
        {
            case AboutUIEvent.BackRequested:
                // Use navigation service to go back
                Navigate("main-menu");
                break;
        }
    }
}
