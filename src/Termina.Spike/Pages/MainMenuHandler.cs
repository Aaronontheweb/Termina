using System.Threading.Channels;
using Termina.Components;
using Termina.Navigation;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Handler for the main menu page.
/// Handles menu selections and triggers navigation.
/// </summary>
public sealed class MainMenuHandler : IPageHandler
{
    private ChannelWriter<object>? _eventWriter;

    public void SetEventWriter(ChannelWriter<object> writer)
    {
        _eventWriter = writer;
    }

    public void HandleEvent(object evt)
    {
        if (evt is OptionSelected selected)
        {
            switch (selected.Value)
            {
                case "Settings":
                    _eventWriter?.TryWrite(new NavigationRequested("settings"));
                    break;
                case "About":
                    _eventWriter?.TryWrite(new NavigationRequested("about"));
                    break;
                case "Exit":
                    // Signal graceful shutdown
                    _eventWriter?.TryWrite(new ShutdownRequested());
                    break;
            }
        }
    }

    public void Tick()
    {
        // No state updates needed
    }
}
