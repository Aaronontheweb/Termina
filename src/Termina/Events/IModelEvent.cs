namespace Termina.Events;

/// <summary>
/// Marker interface for events published by the Model layer (actors, services).
/// Model events notify the UI about backend state changes.
/// </summary>
/// <remarks>
/// Model events flow: Model Layer → Application Bus → Active Page Handler
///
/// The Model layer publishes these events without any knowledge of the UI.
/// Only the currently active page's handler receives model events.
/// Events for inactive pages go to the dead letter log.
///
/// Each page handler declares what model events it handles via the TModelEvent type parameter.
///
/// <code>
/// public abstract record ChatModelEvent : IModelEvent
/// {
///     public sealed record LlmResponseReceived(string Text) : ChatModelEvent;
///     public sealed record LlmError(string Message) : ChatModelEvent;
/// }
/// </code>
/// </remarks>
public interface IModelEvent;
