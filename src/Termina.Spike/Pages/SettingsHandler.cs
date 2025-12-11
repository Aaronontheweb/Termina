using System.Threading.Channels;
using Termina.Components;
using Termina.Input;
using Termina.Navigation;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Handler for the settings page.
/// Handles form interactions and back navigation.
/// </summary>
public sealed class SettingsHandler : IPageHandler
{
    private ChannelWriter<object>? _eventWriter;

    public void SetEventWriter(ChannelWriter<object> writer)
    {
        _eventWriter = writer;
    }

    public void HandleEvent(object evt)
    {
        switch (evt)
        {
            // Handle Escape key to go back
            case KeyPressed key when key.KeyInfo.Key == ConsoleKey.Escape:
                _eventWriter?.TryWrite(new NavigationBackRequested());
                break;

            // Could handle other events like TextSubmitted, OptionSelected
            // For the POC, we just let the components manage their own state
            case TextSubmitted submitted:
                // Could save the username somewhere
                break;

            case OptionSelected selected:
                // Could apply the theme
                break;
        }
    }

    public void Tick()
    {
        // No state updates needed
    }
}
