using System.Collections.Concurrent;
using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Events;

namespace Mass.Core.Events;

public class EventBus : IEventBus
{
    private readonly ILogService _logger;
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = [];

    public EventBus(ILogService logger)
    {
        _logger = logger;
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            // Snapshot for thread safety during iteration
            var handlersList = handlers.ToArray(); 
            
            foreach (var handler in handlersList)
            {
                try
                {
                    if (handler is Action<TEvent> typedHandler)
                    {
                        typedHandler(@event);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error handling event {typeof(TEvent).Name}", ex, "EventBus");
                }
            }
        }
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent
    {
        // For now, run sync handlers on thread pool. 
        // In future, could support true async handlers.
        return Task.Run(() => Publish(@event), ct);
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        _handlers.AddOrUpdate(eventType, 
            _ => [handler], 
            (_, list) => 
            {
                lock (list) 
                { 
                    list.Add(handler); 
                }
                return list;
            });

        return new Subscription<TEvent>(this, handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            lock (list)
            {
                list.Remove(handler);
            }
        }
    }

    private class Subscription<T> : IDisposable where T : IEvent
    {
        private readonly EventBus _bus;
        private readonly Action<T> _handler;

        public Subscription(EventBus bus, Action<T> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            _bus.Unsubscribe(_handler);
        }
    }
}
