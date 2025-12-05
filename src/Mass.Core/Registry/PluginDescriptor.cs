using Mass.Spec.Contracts.Plugins;

namespace Mass.Core.Registry;

/// <summary>
/// Describes a loaded plugin with runtime state.
/// </summary>
public class LoadedPluginDescriptor
{
    /// <summary>
    /// Unique identifier for the plugin.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Plugin manifest from Mass.Spec.
    /// </summary>
    public PluginManifest Manifest { get; set; } = null!;

    /// <summary>
    /// Current state of the plugin.
    /// </summary>
    public PluginState State { get; set; } = PluginState.Registered;
}

/// <summary>
/// Plugin lifecycle states.
/// </summary>
public enum PluginState
{
    /// <summary>
    /// Plugin has been registered but not loaded.
    /// </summary>
    Registered,

    /// <summary>
    /// Plugin is currently being loaded.
    /// </summary>
    Loading,

    /// <summary>
    /// Plugin has been loaded into memory.
    /// </summary>
    Loaded,

    /// <summary>
    /// Plugin is active and running.
    /// </summary>
    Active,

    /// <summary>
    /// Plugin has been stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Plugin failed to load or encountered an error.
    /// </summary>
    Failed
}
