using System.Threading.Channels;
using Termina.Events;
using Termina.Pages;

namespace Termina;

/// <summary>
/// Implementation of the application bus that routes model events.
/// </summary>
/// <remarks>
/// <para>
/// The bus maintains two types of subscriptions:
/// </para>
/// <list type="bullet">
///   <item>Model layer subscriptions - actors/services subscribing to events from handlers</item>
///   <item>UI routing - events routed to the active page's handler</item>
/// </list>
/// </remarks>
internal sealed class ApplicationBus : IApplicationBus
{
    private readonly Channel<object> _eventChannel;
    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public event Action<object, string>? DeadLetter;

    public ApplicationBus(Channel<object> eventChannel)
    {
        _eventChannel = eventChannel;
    }

    /// <inheritdoc />
    public void Publish<T>(T evt) where T : IModelEvent
    {
        if (evt == null) return;

        // Write to the event channel for processing by the main loop
        _eventChannel.Writer.TryWrite(evt);

        // Also dispatch to model layer subscribers
        DispatchToSubscribers(evt);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Action<T> handler) where T : IModelEvent
    {
        var eventType = typeof(T);
        var subscription = new Subscription(evt => handler((T)evt));

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var list))
            {
                list = new List<Subscription>();
                _subscriptions[eventType] = list;
            }
            list.Add(subscription);
        }

        return new SubscriptionDisposable(() => Unsubscribe(eventType, subscription));
    }

    /// <summary>
    /// Raise the dead letter event.
    /// </summary>
    internal void RaiseDeadLetter(object evt, string reason)
    {
        DeadLetter?.Invoke(evt, reason);
    }

    private void DispatchToSubscribers<T>(T evt) where T : IModelEvent
    {
        var eventType = typeof(T);

        List<Subscription>? subscribers;
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out subscribers))
                return;
            // Copy to avoid holding lock during dispatch
            subscribers = subscribers.ToList();
        }

        foreach (var sub in subscribers)
        {
            try
            {
                sub.Handler(evt!);
            }
            catch
            {
                // Swallow exceptions from subscribers to avoid breaking the event loop
                // In a production system, you'd want to log these
            }
        }
    }

    private void Unsubscribe(Type eventType, Subscription subscription)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var list))
            {
                list.Remove(subscription);
            }
        }
    }

    private sealed record Subscription(Action<object> Handler);

    private sealed class SubscriptionDisposable : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public SubscriptionDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _onDispose();
            }
        }
    }
}
