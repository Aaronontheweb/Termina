using System.Threading.Channels;

namespace Termina.Pages;

/// <summary>
/// Handles business events for a page.
/// The handler contains the logic (code-behind) while the page contains the UI.
/// </summary>
public interface IPageHandler
{
    /// <summary>
    /// Set the event writer for emitting events back to the event loop.
    /// Called by the navigation service when the page becomes active.
    /// </summary>
    void SetEventWriter(ChannelWriter<object> writer);

    /// <summary>
    /// Handle a business event from a component or the system.
    /// </summary>
    /// <param name="evt">The event to handle.</param>
    void HandleEvent(object evt);

    /// <summary>
    /// Called after each event is processed.
    /// Use this to push state updates or commands to components if needed.
    /// </summary>
    void Tick();
}
