using Spectre.Console.Rendering;

namespace Termina;

/// <summary>
/// Base class for all UI components in Termina.
/// Components subscribe to events via fluent API and render Spectre.Console widgets.
/// </summary>
public abstract class Component
{
    private readonly Dictionary<Type, List<Action<IUIEvent>>> _subscriptions = new();

    /// <summary>
    /// Subscribe to an event type with a typed handler.
    /// Returns this component for fluent chaining.
    /// </summary>
    /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
    /// <param name="handler">Handler that receives the event and updates component state.</param>
    public Component Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IUIEvent
    {
        var eventType = typeof(TEvent);
        if (!_subscriptions.ContainsKey(eventType))
            _subscriptions[eventType] = new List<Action<IUIEvent>>();

        _subscriptions[eventType].Add(evt => handler((TEvent)evt));
        return this;
    }

    /// <summary>
    /// Called by EventMediator when an event occurs.
    /// Routes the event to subscribed handlers.
    /// </summary>
    internal void ReceiveEvent(IUIEvent evt)
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
