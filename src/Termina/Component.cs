using System.Threading.Channels;
using Spectre.Console.Rendering;
using Termina.Input;

namespace Termina;

/// <summary>
/// Base class for all UI components in Termina.
/// Components subscribe to events via fluent API and render Spectre.Console widgets.
/// </summary>
public abstract class Component
{
    private readonly Dictionary<Type, List<Action<object>>> _subscriptions = new();

    /// <summary>
    /// Channel writer for emitting business events back to the event loop.
    /// Set by the EventMediator when the component becomes active.
    /// </summary>
    internal ChannelWriter<object>? EventWriter { get; set; }

    /// <summary>
    /// Emit a business event to be processed by the page handler.
    /// </summary>
    /// <param name="evt">The business event to emit.</param>
    protected void Emit(object evt)
    {
        EventWriter?.TryWrite(evt);
    }

    /// <summary>
    /// Handle low-level input events. Override to implement custom keyboard behavior.
    /// Default implementation does nothing.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    public virtual void HandleInput(KeyPressed key)
    {
        // Default: no-op. Components can override to handle keyboard input.
    }

    /// <summary>
    /// Subscribe to an event type with a typed handler.
    /// Returns this component for fluent chaining.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="handler">Handler that receives the event and updates component state.</param>
    public Component Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : class
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.ContainsKey(eventType))
            _subscriptions[eventType] = new List<Action<object>>();

        _subscriptions[eventType].Add(evt => handler((TEvent)evt));
        return this;
    }

    /// <summary>
    /// Send an event to this component.
    /// Routes the event to subscribed handlers.
    /// Used by pages to send business commands to components.
    /// </summary>
    public void ReceiveEvent(object evt)
    {
        var eventType = evt.GetType();
        if (_subscriptions.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
                handler(evt);
        }
    }

    /// <summary>
    /// Render this component as a Spectre.Console renderable.
    /// Called after events are processed to update the display.
    /// </summary>
    public abstract IRenderable Render();
}
