using Termina.Events;

namespace Termina.Pages;

/// <summary>
/// The application bus for publishing model events from the Model layer.
/// </summary>
/// <remarks>
/// <para>
/// The application bus is the central communication channel between the Model layer
/// (actors, services) and the UI layer (pages, handlers).
/// </para>
/// <para>
/// Model layer components publish events to this bus without any knowledge of the UI.
/// The TerminaApplication routes events to the appropriate handler based on the current page.
/// </para>
/// <para>
/// Events for inactive pages are routed to the dead letter handler.
/// </para>
/// <example>
/// <code>
/// // In an Akka.NET actor:
/// public class LlmActor : ReceiveActor
/// {
///     public LlmActor(IApplicationBus bus, ILlmClient llm)
///     {
///         ReceiveAsync&lt;SubmitPrompt&gt;(async msg =>
///         {
///             var response = await llm.CompleteAsync(msg.Text);
///             bus.Publish(new ChatModelEvent.LlmResponseReceived(response));
///         });
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IApplicationBus
{
    /// <summary>
    /// Publish a model event to be routed to the active page's handler.
    /// </summary>
    /// <typeparam name="T">The event type (must implement IModelEvent).</typeparam>
    /// <param name="evt">The event to publish.</param>
    /// <remarks>
    /// <para>
    /// If the active page's handler does not handle this event type,
    /// the event is sent to the dead letter handler.
    /// </para>
    /// </remarks>
    void Publish<T>(T evt) where T : IModelEvent;

    /// <summary>
    /// Subscribe to model events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when events of this type are published.</param>
    /// <returns>An IDisposable to unsubscribe.</returns>
    /// <remarks>
    /// <para>
    /// Use this to let Model layer components (actors, services) receive events
    /// from UI handlers. For example, a handler might publish a "SubmitPrompt" event
    /// that an LLM actor subscribes to.
    /// </para>
    /// </remarks>
    IDisposable Subscribe<T>(Action<T> handler) where T : IModelEvent;

    /// <summary>
    /// Event raised when an event cannot be routed to any handler.
    /// Use for logging and debugging unhandled events.
    /// </summary>
    /// <remarks>
    /// Parameters: (event object, reason string)
    /// </remarks>
    event Action<object, string>? DeadLetter;
}
