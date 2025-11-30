using System.Text.Json;

namespace Mass.Core.Plugins;

public class PluginDiscoveryService
{
    private readonly List<string> _pluginPaths;

    public PluginDiscoveryService(params string[] pluginPaths)
    {
        _pluginPaths = pluginPaths.ToList();
    }

    public async Task<List<DiscoveredPlugin>> DiscoverPluginsAsync()
    {
        var discoveredPlugins = new List<DiscoveredPlugin>();

        foreach (var basePath in _pluginPaths)
        {
            if (!Directory.Exists(basePath))
            {
                continue;
            }

            var pluginDirectories = Directory.GetDirectories(basePath);

            foreach (var pluginDir in pluginDirectories)
            {
                var manifestPath = Path.Combine(pluginDir, "plugin.json");
                
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                try
                {
                    var json = await File.ReadAllTextAsync(manifestPath);
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(json);

                    if (manifest != null && ValidateManifest(manifest))
                    {
                        discoveredPlugins.Add(new DiscoveredPlugin
                        {
                            Manifest = manifest,
                            PluginPath = pluginDir,
                            ManifestPath = manifestPath
                        });
                    }
                }
                catch
                {
                }
            }
        }

        return discoveredPlugins;
    }

    private bool ValidateManifest(PluginManifest manifest)
    {
        return !string.IsNullOrWhiteSpace(manifest.Id) &&
               !string.IsNullOrWhiteSpace(manifest.Name) &&
               !string.IsNullOrWhiteSpace(manifest.Version) &&
               !string.IsNullOrWhiteSpace(manifest.EntryAssembly) &&
               !string.IsNullOrWhiteSpace(manifest.EntryType);
    }
}

public class DiscoveredPlugin
{
    public PluginManifest Manifest { get; set; } = null!;
    public string PluginPath { get; set; } = string.Empty;
    public string ManifestPath { get; set; } = string.Empty;
}
