using System.Threading.Channels;
using Termina.Input;
using Termina.Navigation;
using Termina.Pages;

namespace Termina.Spike.Pages;

/// <summary>
/// Handler for the about page.
/// Just handles back navigation.
/// </summary>
public sealed class AboutHandler : IPageHandler
{
    private ChannelWriter<object>? _eventWriter;

    public void SetEventWriter(ChannelWriter<object> writer)
    {
        _eventWriter = writer;
    }

    public void HandleEvent(object evt)
    {
        if (evt is KeyPressed key && key.KeyInfo.Key == ConsoleKey.Escape)
        {
            _eventWriter?.TryWrite(new NavigationBackRequested());
        }
    }

    public void Tick()
    {
        // No state updates needed
    }
}
