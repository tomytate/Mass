namespace Mass.Spec.Contracts.State;

/// <summary>
/// Interface for centralized application state management.
/// </summary>
public interface IApplicationState
{
    /// <summary>
    /// Observable stream of plugin states.
    /// </summary>
    IObservable<PluginState[]> Plugins { get; }
    
    /// <summary>
    /// Observable stream of agent states.
    /// </summary>
    IObservable<AgentState[]> Agents { get; }
    
    /// <summary>
    /// Updates the plugins collection.
    /// </summary>
    void UpdatePlugins(Func<PluginState[], PluginState[]> updater);
    
    /// <summary>
    /// Updates the agents collection.
    /// </summary>
    void UpdateAgents(Func<AgentState[], AgentState[]> updater);
}

/// <summary>
/// Represents the state of a loaded plugin.
/// </summary>
public record PluginState
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsLoaded { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Represents the state of a connected agent.
/// </summary>
public record AgentState
{
    public required string AgentId { get; init; }
    public required string Hostname { get; init; }
    public string? IpAddress { get; init; }
    public string Status { get; init; } = "Unknown";
    public double CpuUsage { get; init; }
    public double MemoryUsage { get; init; }
    public DateTimeOffset LastSeen { get; init; }
}
