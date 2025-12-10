namespace Termina;

/// <summary>
/// Base interface for all UI events that flow through the event channel.
/// Events are strongly-typed and immutable records.
/// </summary>
public interface IUIEvent
{
    /// <summary>
    /// Timestamp when this event was created.
    /// </summary>
    DateTime Timestamp { get; }
}
