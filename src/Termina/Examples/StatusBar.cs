using Spectre.Console;
using Spectre.Console.Rendering;

namespace Termina.Examples;

/// <summary>
/// Example component showing event subscription and rendering.
/// </summary>
public sealed class StatusBar : Component
{
    private string _status = "Ready";

    public StatusBar()
    {
        // Fluent subscription to events
        this.Subscribe<StatusUpdated>(OnStatusUpdated);
    }

    private void OnStatusUpdated(StatusUpdated evt)
    {
        _status = evt.Message;
    }

    public override IRenderable Render()
    {
        return new Panel(_status)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Aqua)
            .Header("[bold]Status[/]");
    }
}

/// <summary>
/// Example event for updating status.
/// </summary>
public sealed record StatusUpdated(string Message, DateTime Timestamp) : IUIEvent;
