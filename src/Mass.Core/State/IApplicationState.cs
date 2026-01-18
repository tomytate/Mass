using Mass.Spec.Contracts.State;

namespace Mass.Core.State;

public interface IApplicationState
{
    IObservable<PluginState[]> Plugins { get; }
    IObservable<AgentState[]> Agents { get; }

    void UpdatePlugins(Func<PluginState[], PluginState[]> updater);
    void UpdateAgents(Func<AgentState[], AgentState[]> updater);
}
