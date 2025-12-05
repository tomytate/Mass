namespace Mass.Spec.Contracts.Plugins;

/// <summary>
/// Represents the manifest file of a plugin (plugin.json).
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Unique identifier for the plugin.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the plugin.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version of the plugin.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Author of the plugin.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Description of the plugin.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The main assembly file name.
    /// </summary>
    public string EntryAssembly { get; set; } = string.Empty;

    /// <summary>
    /// The entry point class name.
    /// </summary>
    public string EntryType { get; set; } = string.Empty;

    /// <summary>
    /// List of dependencies required by this plugin.
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// List of capabilities provided by this plugin.
    /// </summary>
    public List<string> Capabilities { get; set; } = new();

    /// <summary>
    /// List of permissions required by this plugin.
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Icon resource or path.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
