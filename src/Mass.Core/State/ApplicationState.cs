using Mass.Spec.Contracts.State;

namespace Mass.Core.State;

public class ApplicationState : IApplicationState
{
    private readonly StateSubject<PluginState[]> _plugins = new(Array.Empty<PluginState>());
    private readonly StateSubject<AgentState[]> _agents = new(Array.Empty<AgentState>());

    public IObservable<PluginState[]> Plugins => _plugins;
    public IObservable<AgentState[]> Agents => _agents;

    public void UpdatePlugins(Func<PluginState[], PluginState[]> updater)
    {
        _plugins.Value = updater(_plugins.Value);
    }

    public void UpdateAgents(Func<AgentState[], AgentState[]> updater)
    {
        _agents.Value = updater(_agents.Value);
    }
}

// Simple implementation of IObservable/BehaviorSubject to avoid Rx dependency for now
public class StateSubject<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = new();
    private T _value;
    private readonly object _lock = new();

    public StateSubject(T initialValue)
    {
        _value = initialValue;
    }

    public T Value
    {
        get => _value;
        set
        {
            lock (_lock)
            {
                _value = value;
                foreach (var observer in _observers.ToArray())
                {
                    observer.OnNext(value);
                }
            }
        }
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_lock)
        {
            _observers.Add(observer);
            observer.OnNext(_value); // Emit current value on subscribe
        }
        return new Unsubscriber(this, observer);
    }

    private class Unsubscriber : IDisposable
    {
        private readonly StateSubject<T> _subject;
        private readonly IObserver<T> _observer;

        public Unsubscriber(StateSubject<T> subject, IObserver<T> observer)
        {
            _subject = subject;
            _observer = observer;
        }

        public void Dispose()
        {
            lock (_subject._lock)
            {
                _subject._observers.Remove(_observer);
            }
        }
    }
}
