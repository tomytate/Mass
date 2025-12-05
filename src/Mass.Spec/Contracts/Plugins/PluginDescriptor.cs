namespace Mass.Spec.Contracts.Plugins;

/// <summary>
/// Describes a loaded plugin at runtime.
/// </summary>
public class PluginDescriptor
{
    /// <summary>
    /// The manifest of the plugin.
    /// </summary>
    public PluginManifest Manifest { get; set; } = new();

    /// <summary>
    /// Whether the plugin is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The absolute path to the plugin directory.
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;
}
