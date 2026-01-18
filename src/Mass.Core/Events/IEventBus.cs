using Mass.Spec.Contracts.Events;

namespace Mass.Core.Events;

public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all subscribers synchronously.
    /// </summary>
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;

    /// <summary>
    /// Publishes an event to all subscribers asynchronously.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent;

    /// <summary>
    /// Subscribes to an event type. Returns a disposable subscription.
    /// </summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
}
